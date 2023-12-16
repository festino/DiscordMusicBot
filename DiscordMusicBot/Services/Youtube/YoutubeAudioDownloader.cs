﻿using AsyncEvent;
using DiscordMusicBot.AudioRequesting;
using System.Diagnostics;
using System.Net;
using static DiscordMusicBot.AudioRequesting.IAudioDownloader;

public class YoutubeAudioDownloader : IAudioDownloader
{
    private readonly List<string> _downloadingIds = new();
    private readonly List<string> _notifyIds = new();

    public event AsyncEventHandler<LoadCompletedArgs>? LoadCompleted;
    public event AsyncEventHandler<LoadFailedArgs>? LoadFailed;

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
        if (!IsValidYoutubeId(youtubeId))
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
        Console.WriteLine($"Downloaded {youtubeId}");
        return !File.Exists(path) ? null : path;
    }

    private async Task<string?> GetWebaLink(string youtubeId)
    {
        if (!IsValidYoutubeId(youtubeId))
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
        using (destination)
        using (var client = new HttpClient())
        {
            try
            {
                Stream source = await client.GetStreamAsync(url);
                await source.CopyToAsync(destination);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not continue downloading {url}\n{e}");
            }
        }
    }

    private Process? GetPCMStreamProcess(string url)
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
        
        return process;
    }

    private async Task DownloadAsync(string youtubeId)
    {
        string? link = await GetWebaLink(youtubeId);
        Process? process = link is null? null : GetPCMStreamProcess(link);
        bool notify = false;
        lock (_downloadingIds)
        {
            _downloadingIds.Remove(youtubeId);
            notify = _notifyIds.Remove(youtubeId);
        }

        if (notify)
            await OnLoadedAsync(youtubeId, process);
    }

    private async Task OnLoadedAsync(string youtubeId, Process? process)
    {
        Task? task;
        if (process is null)
        {
            task = LoadFailed?.InvokeAsync(this, new LoadFailedArgs(youtubeId));
            if (task is not null)
                await task;
        }
        else
        {
            using (var stream = process.StandardOutput.BaseStream)
            {
                task = LoadCompleted?.InvokeAsync(this, new LoadCompletedArgs(youtubeId, process));
                if (task is not null)
                    await task;
            }

        }
    }

    private static bool IsValidYoutubeId(string youtubeId)
    {
        return youtubeId.Length == 11 && youtubeId.All(c => IsValidYoutubeIdChar(c));
    }

    private static bool IsValidYoutubeIdChar(char c)
    {
        return 'a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || '0' <= c && c <= '9' || c == '-' || c == '_';
    }
}