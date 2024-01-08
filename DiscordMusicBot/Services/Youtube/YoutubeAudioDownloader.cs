using DiscordMusicBot.Abstractions;
using Serilog;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Sockets;
using static DiscordMusicBot.Abstractions.IAudioDownloader;
using static DiscordMusicBot.Extensions.LoggerExtensions;

namespace DiscordMusicBot.Services.Youtube
{
    public class YoutubeAudioDownloader : IAudioDownloader
    {
        private record CallbackInfo(
            string YoutubeId,
            Func<LoadCompletedArgs, Task> OnCompleted,
            Func<LoadFailedArgs, Task> OnFailed);

        private readonly string YtDlFilepath = "yt-dlp";
        private readonly string FfmpegFilepath = "ffmpeg";

        private readonly ILogger _logger;

        private readonly HttpClient _httpClient;

        private readonly List<string> _downloadingIds = new();
        private readonly List<CallbackInfo> _callbacks = new();

        public YoutubeAudioDownloader(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            _httpClient.DefaultRequestHeaders.Add("Keep-Alive", "3600");

            List<string> missingFiles = new();
            if (!File.Exists(YtDlFilepath + ".exe"))
            {
                missingFiles.Add(YtDlFilepath + ".exe");
            }
            if (!File.Exists(FfmpegFilepath + ".exe"))
            {
                missingFiles.Add(FfmpegFilepath + ".exe");
            }
            if (missingFiles.Count > 0)
            {
                throw new FileNotFoundException($"Could not find files: {string.Join(", ", missingFiles)}");
            }
        }

        public void RequestDownload(string youtubeId,
                                    Func<LoadCompletedArgs, Task> OnCompleted,
                                    Func<LoadFailedArgs, Task> OnFailed)
        {
            lock (_downloadingIds)
            {
                _callbacks.Add(new CallbackInfo(youtubeId, OnCompleted, OnFailed));

                if (_downloadingIds.Contains(youtubeId))
                    return;

                _downloadingIds.Add(youtubeId);
            }

            Task.Run(() => DownloadAsync(youtubeId));
        }

        private async Task<string?> DownloadMp3Async(string youtubeId)
        {
            if (!YoutubeUtils.IsValidYoutubeId(youtubeId))
                return null;

            string path = Path.GetFullPath($"./downloads/{youtubeId}.mp3");
            if (File.Exists(path))
                return path;

            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = YtDlFilepath,
                Arguments = $"--extract-audio --audio-format mp3 -q -v -o \"{path}\" https://www.youtube.com/watch?v={youtubeId}",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            if (process is null)
                return null;

            await process.WaitForExitAsync();
            _logger.Here().Debug("Downloaded {YoutubeId}", youtubeId);
            return !File.Exists(path) ? null : path;
        }

        private async Task<string?> GetWebaLink(string youtubeId)
        {
            if (!YoutubeUtils.IsValidYoutubeId(youtubeId))
                return null;

            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = YtDlFilepath,
                Arguments = $"--extract-audio --quiet --simulate --print url https://www.youtube.com/watch?v={youtubeId}",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            if (process is null)
                return null;

            string link = process.StandardOutput.ReadToEnd().Trim();
            await process.WaitForExitAsync();
            // invalid video may return "NA" string
            return link.Contains("https") ? link : null;
        }

        private async Task CopyFromUrlAsync(Stream destination, string url)
        {
            // "it's a rare problem"
            // https://github.com/dotnet/runtime/issues/60644
            using (destination)
            {
                long? lastPosition = null;
                do
                {
                    long position = lastPosition is null ? 0L : (long)lastPosition;
                    lastPosition = await TryCopyFromUrlAsync(destination, url, position);
                }
                while (lastPosition is not null);

            }
        }

        private async Task<long?> TryCopyFromUrlAsync(Stream destination, string url, long position)
        {
            _httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(position, null);
            using (Stream webStream = await _httpClient.GetStreamAsync(url))
            using (CountingReadonlyStream source = new(webStream, position))
            {
                try
                {
                    await source.CopyToAsync(destination);
                }
                catch (IOException e) when (e.InnerException is null)
                {
                    _logger.Here().Debug("Channel probably was closed: video was downloaded or skipped");
                }
                catch (IOException e) when (e.InnerException is SocketException)
                {
                    _logger.Here().Debug("Could not continue downloading, retrying since {Position}", source.Position);
                    return source.Position;
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Could not continue downloading\n{Exception}", e);
                }
            }

            return null;
        }

        private Stream? GetPCMStream(string url)
        {
            if (url.Contains('"'))
                return null;

            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = FfmpegFilepath,
                Arguments = $"-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });

            if (process is null)
                return null;

            Task.Run(() => CopyFromUrlAsync(process.StandardInput.BaseStream, url));

            return process.StandardOutput.BaseStream;
        }

        private async Task DownloadAsync(string youtubeId)
        {
            string? link = await GetWebaLink(youtubeId);
            Stream? stream = link is null ? null : GetPCMStream(link);
            List<CallbackInfo> callbacks;
            lock (_downloadingIds)
            {
                _downloadingIds.Remove(youtubeId);

                callbacks = _callbacks
                    .Where(c => c.YoutubeId == youtubeId)
                    .ToList();

                _callbacks.RemoveAll(c => callbacks.Contains(c));
            }

            await OnLoadedAsync(youtubeId, stream, callbacks);
        }

        private async Task OnLoadedAsync(string youtubeId, Stream? stream, List<CallbackInfo> callbacks)
        {
            Task? task;
            if (stream is null)
            {
                foreach (var callback in callbacks)
                    await callback.OnFailed(new LoadFailedArgs(youtubeId));
            }
            else
            {
                foreach (var callback in callbacks)
                    await callback.OnCompleted(new LoadCompletedArgs(youtubeId, stream));
            }
        }
    }
}