using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Services.Discord;
using Serilog;
using static DiscordMusicBot.Abstractions.IAudioDownloader;
using static DiscordMusicBot.Abstractions.IAudioStreamer;

namespace DiscordMusicBot.AudioRequesting
{
    public class RequestQueue
    {
        private readonly ILogger _logger;

        private readonly INotificationService _notificationService;
        private readonly IAudioDownloader _audioDownloader;
        private readonly IAudioStreamer _audioStreamer;

        private readonly int MaxHistoryCount = 21;
        private readonly List<Video> _history = new();
        private List<Video> _videos = new();

        public RequestQueue(
            ILogger logger,
            INotificationService notificationService,
            IAudioDownloader audioDownloader,
            IAudioStreamer audioStreamer
        )
        {
            _logger = logger;
            _notificationService = notificationService;
            _audioDownloader = audioDownloader;
            _audioStreamer = audioStreamer;
            _audioStreamer.Finished += OnAudioFinishedAsync;
            _audioDownloader.LoadCompleted += OnLoadCompletedAsync;
            _audioDownloader.LoadFailed += OnLoadFailedAsync;
        }

        public Video[] GetHistory()
        {
            if (_videos.Count == 0)
                return _history.ToArray();

            return _history
                .Where((video) => video != _videos[0])
                .ToArray();
        }

        public List<Video> GetVideos()
        {
            return _videos;
        }

        public void Add(Video video)
        {
            _videos.Add(video);
            TryRequestDownload(_videos.Count - 1);
        }

        public async Task<Video?> RemoveCurrentAsync()
        {
            if (_videos.Count < 1)
                return null;

            return await RemoveAtAsync(0);
        }

        public async Task<Video[]?> RemoveLastAsync(DiscordMessageInfo discordMessageInfo)
        {
            var requester = discordMessageInfo.RequesterId;
            int index = _videos.FindLastIndex(v => v.MessageInfo.RequesterId.Equals(requester));
            if (index < 0)
                return null;

            var message = _videos[index].MessageInfo.MessageId;
            List<Video> videos = new();
            while ((index = _videos.FindLastIndex(index, v => v.MessageInfo.MessageId.Equals(message))) >= 0)
            {
                videos.Add(await RemoveAtAsync(index--));
            }
            return videos.ToArray();
        }

        public async Task<List<Video>> ClearAsync()
        {
            if (_videos.Count >= 1)
                _audioDownloader.StopDownloading(_videos[0].YoutubeId);
            if (_videos.Count >= 2)
                _audioDownloader.StopDownloading(_videos[1].YoutubeId);
            await _audioStreamer.StopAsync();

            var videos = _videos;
            _videos = new List<Video>();

            await TryLeaveAsync();
            _logger.Here().Information("Queue was cleared");
            return videos;
        }

        private async Task OnLoadCompletedAsync(object sender, LoadCompletedArgs args)
        {
            if (_videos.Count == 0 || _videos[0].YoutubeId != args.YoutubeId)
                return;

            Video video = _videos[0];
            AddToHistory(video);
            await _audioStreamer.JoinAndPlayAsync(video, args.PcmStream, GetRequesterIds);
        }

        private async Task OnLoadFailedAsync(object sender, LoadFailedArgs args)
        {
            _logger.Here().Warning("Load failed {YoutubeId}", args.YoutubeId);
            await RemoveAtAsync(0);
        }

        private async Task OnAudioFinishedAsync(object sender, PlaybackEndedArgs args)
        {
            if (args.Status == PlaybackEndedStatus.Stopped) return;

            if (args.Status == PlaybackEndedStatus.Disconnected)
            {
                await ClearAsync();
                return;
            }

            if (_videos.Count == 0 || !_videos[0].Equals(args.Video))
                return;

            await RemoveAtAsync(0);
        }

        private async Task<Video> RemoveAtAsync(int index)
        {
            Video video = _videos[index];
            _videos.RemoveAt(index);
            _audioDownloader.StopDownloading(video.YoutubeId);
            if (index == 0)
                await _audioStreamer.StopAsync();

            await TryLeaveAsync();
            TryRequestDownload(index);
            return video;
        }

        private async Task TryLeaveAsync()
        {
            if (_videos.Count == 0)
            {
                // TODO write message AFTER skip message
                await _notificationService.SendAsync(CommandStatus.Info, "no videos left!!!");
                _audioStreamer.RequestLeave();
            }
        }

        private void TryRequestDownload(int index)
        {
            if (index >= _videos.Count)
                return;

            if (index == 0)
                _audioDownloader.RequestDownload(_videos[0].YoutubeId, true);
            if (index <= 1 && _videos.Count >= 2)
                _audioDownloader.RequestDownload(_videos[1].YoutubeId, false);
        }

        private ulong[] GetRequesterIds()
        {
            return _videos
                .Select((video) => video.MessageInfo.RequesterId)
                .Distinct()
                .ToArray();
        }

        private void AddToHistory(Video video)
        {
            if (_history.Count >= MaxHistoryCount)
                _history.RemoveAt(0);

            _history.Add(video);
        }
    }
}
