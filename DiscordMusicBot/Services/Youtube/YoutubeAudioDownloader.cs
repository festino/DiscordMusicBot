using AsyncEvent;
using DiscordMusicBot.AudioRequesting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Sockets;
using static DiscordMusicBot.AudioRequesting.IAudioDownloader;

namespace DiscordMusicBot.Services.Youtube
{
    public class YoutubeAudioDownloader : IAudioDownloader
    {
        private readonly ILogger _logger;

        private readonly HttpClient _httpClient;

        private readonly List<string> _downloadingIds = new();
        private readonly List<string> _notifyIds = new();

        public event AsyncEventHandler<LoadCompletedArgs>? LoadCompleted;
        public event AsyncEventHandler<LoadFailedArgs>? LoadFailed;

        public YoutubeAudioDownloader(ILogger<YoutubeAudioDownloader> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            _httpClient.DefaultRequestHeaders.Add("Keep-Alive", "3600");
        }

        public void RequestDownload(string youtubeId, bool notify)
        {
            lock (_downloadingIds)
            {
                if (notify)
                    _notifyIds.Add(youtubeId);

                if (_downloadingIds.Contains(youtubeId))
                    return;

                _downloadingIds.Add(youtubeId);
            }

            Task.Run(() => DownloadAsync(youtubeId));
        }

        public void StopDownloading(string youtubeId)
        {
            lock (_downloadingIds)
            {
                _notifyIds.Remove(youtubeId);
            }
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
                FileName = "yt-dlp",
                Arguments = $"--extract-audio --audio-format mp3 -q -v -o \"{path}\" https://www.youtube.com/watch?v={youtubeId}",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            if (process is null)
                return null;

            await process.WaitForExitAsync();
            _logger.LogDebug("Downloaded {YoutubeId}", youtubeId);
            return !File.Exists(path) ? null : path;
        }

        private async Task<string?> GetWebaLink(string youtubeId)
        {
            if (!YoutubeUtils.IsValidYoutubeId(youtubeId))
                return null;

            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "yt-dlp",
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
                    _logger.LogDebug("Channel probably was closed: video was downloaded or skipped");
                }
                catch (IOException e) when (e.InnerException is SocketException)
                {
                    _logger.LogDebug("Could not continue downloading, retrying since {Position}", source.Position);
                    return source.Position;
                }
                catch (Exception e)
                {
                    _logger.LogError("Could not continue downloading\n{Exception}", e);
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
                FileName = "ffmpeg",
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
            bool notify = false;
            lock (_downloadingIds)
            {
                _downloadingIds.Remove(youtubeId);
                notify = _notifyIds.Remove(youtubeId);
            }

            if (notify)
                await OnLoadedAsync(youtubeId, stream);
        }

        private async Task OnLoadedAsync(string youtubeId, Stream? stream)
        {
            Task? task;
            if (stream is null)
            {
                task = LoadFailed?.InvokeAsync(this, new LoadFailedArgs(youtubeId));
            }
            else
            {
                task = LoadCompleted?.InvokeAsync(this, new LoadCompletedArgs(youtubeId, stream));
            }

            if (task is not null)
                await task;
        }
    }
}