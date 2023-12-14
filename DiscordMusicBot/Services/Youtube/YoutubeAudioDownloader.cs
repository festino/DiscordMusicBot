﻿using AsyncEvent;
using DiscordMusicBot.AudioRequesting;
using System.Diagnostics;
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
        return (process is null || !File.Exists(path)) ? null : path;
    }

    private async Task DownloadAsync(string youtubeId)
    {
        string? path = await DownloadMp3Async(youtubeId);
        bool notify = false;
        lock (_downloadingIds)
        {
            _downloadingIds.Remove(youtubeId);
            notify = _notifyIds.Remove(youtubeId);
        }

        if (notify)
            await OnLoadedAsync(youtubeId, path);
    }

    private async Task OnLoadedAsync(string youtubeId, string? path)
    {
        Console.WriteLine("Downloaded");
        Task? task;
        if (path is null)
            task = LoadFailed?.InvokeAsync(this, new LoadFailedArgs(youtubeId));
        else
            task = LoadCompleted?.InvokeAsync(this, new LoadCompletedArgs(youtubeId, path));

        if (task is not null)
            await task;
    }

    private bool IsValidYoutubeId(string youtubeId)
    {
        return youtubeId.Length == 11 && youtubeId.All(c => IsValidYoutubeIdChar(c));
    }

    private bool IsValidYoutubeIdChar(char c)
    {
        return 'a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || '0' <= c && c <= '9' || c == '-' || c == '_';
    }
}