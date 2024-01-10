using AsyncEvent;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Utils;
using Serilog;
using static DiscordMusicBot.Abstractions.IAudioPlayer;

namespace DiscordMusicBot.AudioRequesting
{
    public class AudioPlayer : IAudioPlayer
    {
        private readonly static int VoiceChannelTimeoutMs = 5 * 60 * 1000;

        private readonly ILogger _logger;

        private readonly IFloatingMessage _floatingMessage;

        private readonly IAudioStreamer _audioStreamer;

        private CancellationTokenSource _playCancellationSource = new();
        private Task _playTask = Task.CompletedTask;
        private Video? _currentVideo = null;

        private CancellationTokenSource _leaveCancellationSource = new();
        private Task _leaveTask = Task.CompletedTask;

        public event AsyncEventHandler<PlaybackEndedArgs>? Finished;

        public AudioPlayer(ILogger logger, IAudioStreamer audioStreamer, IFloatingMessage floatingMessage)
        {
            _logger = logger;
            _audioStreamer = audioStreamer;
            _floatingMessage = floatingMessage;
        }

        public AudioInfo? GetPlaybackInfo()
        {
            TimeSpan? currentTime = _audioStreamer.GetCurrentTime();
            if (_currentVideo is null || currentTime is null)
                return null;

            return new AudioInfo(_currentVideo, (TimeSpan)currentTime);
        }

        public async Task JoinAndPlayAsync(Video video, Stream pcmStream, Func<ulong[]> getRequesterIds)
        {
            if (!_leaveTask.IsCompleted)
            {
                _leaveCancellationSource.Cancel();
                _leaveCancellationSource = new CancellationTokenSource();
                _leaveTask = Task.CompletedTask;
            }

            CancellationToken cancellationToken = _playCancellationSource.Token;
            _floatingMessage.Update(() => MessageFormatUtils.FormatJoiningMessage());
            await _audioStreamer.JoinAsync(getRequesterIds, cancellationToken);
            _floatingMessage.Update(() => MessageFormatUtils.FormatPlayingMessage(GetPlaybackInfo()));
            await StartNewAsync(video, pcmStream, getRequesterIds, cancellationToken);
        }

        public void RequestLeave()
        {
            _leaveTask = Task.Run(() => _audioStreamer.LeaveAsync(VoiceChannelTimeoutMs, _leaveCancellationSource.Token));
        }

        public async Task PauseAsync()
        {
            throw new NotImplementedException();
        }

        public async Task ResumeAsync()
        {
            throw new NotImplementedException();
        }

        public async Task StopAsync()
        {
            if (_playTask.IsCompleted)
                return;

            _playCancellationSource.Cancel();
            _playCancellationSource = new();
            try { await _playTask; } catch { }
            _logger.Here().Debug("Stopped playing audio");
        }

        private async Task StartNewAsync(Video video, Stream pcmStream, Func<ulong[]> getRequesterIds, CancellationToken cancellationToken)
        {
            _logger.Here().Debug("Starting {YoutubeId}", video.YoutubeId);
            _currentVideo = video;
            _playTask = _audioStreamer.PlayAudioAsync(pcmStream, getRequesterIds, cancellationToken);
            await _playTask;
            _logger.Here().Debug("Finished {YoutubeId}", video.YoutubeId);
            _currentVideo = null;

            PlaybackEndedStatus status = PlaybackEndedStatus.Ok;
            if (cancellationToken.IsCancellationRequested)
                status = PlaybackEndedStatus.Stopped;
            if (!_audioStreamer.IsConnected)
                status = PlaybackEndedStatus.Disconnected;

            Task? task = Finished?.InvokeAsync(this, new PlaybackEndedArgs(status, video));
            if (task is not null)
                await task;
        }
    }
}
