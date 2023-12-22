using AsyncEvent;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Discord.Volume;
using Serilog;
using static DiscordMusicBot.Abstractions.IAudioStreamer;

namespace DiscordMusicBot.AudioRequesting
{
    public class AudioStreamer : IAudioStreamer
    {
        private const int RetryDelayMs = 500;
        private const int VoiceChannelTimeoutMs = 5 * 60 * 1000;

        private readonly ILogger _logger;

        private readonly DiscordSocketClient _client;
        private ulong? _guildId = null;
        private ulong? _channelId = null;

        private IAudioClient? _audioClient = null;
        private PlaybackState _state = PlaybackState.NoStream;

        private CancellationTokenSource _playCancellationSource = new();
        private Task _playTask = Task.CompletedTask;
        private Video? _currentVideo = null;
        private VolumeStream? _volumeStream = null;

        private CancellationTokenSource _leaveCancellationSource = new();
        private Task _leaveTask = Task.CompletedTask;

        public event AsyncEventHandler<PlaybackEndedArgs>? Finished;

        // dependency injection skill issue
        public ulong? GuildId
        {
            get => _guildId;
            set => _guildId = value;
        }

        public AudioStreamer(ILogger logger, DiscordBot bot)
        {
            _logger = logger;
            _client = bot.Client;
        }

        public AudioInfo? GetCurrentTime()
        {
            if (_state != PlaybackState.Playing && _state != PlaybackState.Paused)
                return null;

            if (_currentVideo is null || _volumeStream is null)
                return null;

            return new AudioInfo(_currentVideo, _volumeStream.TimeRead);
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
            await JoinAsync(getRequesterIds, cancellationToken);
            await StartNewAsync(video, pcmStream, cancellationToken);
        }

        public void RequestLeave()
        {
            _state = PlaybackState.ReadyToLeave;
            _leaveTask = Task.Run(() => LeaveAsync(VoiceChannelTimeoutMs, _leaveCancellationSource.Token));
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
            _logger.Here().Debug("Stopped audio streamer");
        }

        private async Task StartNewAsync(Video video, Stream pcmStream, CancellationToken cancellationToken)
        {
            _logger.Here().Debug("Starting {YoutubeId}", video.YoutubeId);
            _currentVideo = video;
            _playTask = PlayAudioAsync(pcmStream, cancellationToken);
            await _playTask;
            _logger.Here().Debug("Finished {YoutubeId}", video.YoutubeId);

            PlaybackEndedStatus status = PlaybackEndedStatus.Ok;
            if (cancellationToken.IsCancellationRequested)
                status = PlaybackEndedStatus.Stopped;
            if (_audioClient is null)
                status = PlaybackEndedStatus.Disconnected;
            Task? task = Finished?.InvokeAsync(this, new PlaybackEndedArgs(status, video));
            if (task is not null)
                await task;
        }

        private async Task LeaveAsync(int delayMs, CancellationToken cancellationToken)
        {
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
            _audioClient?.StopAsync();
            _audioClient = null;
            _channelId = null;
            _state = PlaybackState.NoStream;
        }

        private async Task PlayAudioAsync(Stream pcmStream, CancellationToken cancellationToken)
        {
            if (_audioClient is null)
            {
                _logger.Here().Error("Audio client is null!");
                return;
            }

            try
            {
                await PlayAsync(_audioClient, pcmStream, cancellationToken);
            }
            catch (Exception e)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.Here().Error("Audio client was disconnected!\n{Exception}", e);
                    await LeaveAsync(0, CancellationToken.None);
                }
            }
            _currentVideo = null;
            _state = PlaybackState.NoStream;
        }

        private async Task PlayAsync(IAudioClient audioClient, Stream pcmStream, CancellationToken cancellationToken)
        {
            // TODO try load average volume 
            using (pcmStream)
            using (_volumeStream = new VolumeStream(new AverageVolumeBalancer(), null, pcmStream))
            {
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    _state = PlaybackState.Playing;
                    bool isCancelled = false;
                    try
                    {
                        await _volumeStream.CopyToAsync(discord, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // if bot was kicked cancellationToken will not be set
                        // causing discord.FlushAsync to hang forever
                        isCancelled = true;
                        throw;
                    }
                    finally
                    {
                        if (!isCancelled)
                            await discord.FlushAsync(cancellationToken);
                    }
                }

                if (_volumeStream.AverageVolume is not null)
                {
                    // TODO save average volume 
                }
            }
        }

        private Tuple<IVoiceChannel, IGuildUser>[] GetChannels(ulong[] requesterIds)
        {
            if (_guildId is null)
                throw new InvalidOperationException("Guild id is not initialized!");

            List<Tuple<IVoiceChannel, IGuildUser>> channelIds = new();
            var guild = _client.GetGuild((ulong)_guildId);
            foreach (ulong requesterId in requesterIds)
            {
                IGuildUser? user = guild.GetUser(requesterId);
                if (user is null)
                    continue;

                IVoiceChannel? channel = user.VoiceChannel;
                if (channel is null || channel.GuildId != _guildId)
                    continue;

                channelIds.Add(Tuple.Create(channel, user));
            }

            return channelIds.ToArray();
        }

        private async Task JoinAsync(Func<ulong[]> getRequesterIds, CancellationToken cancellationToken)
        {
            if (_state != PlaybackState.ReadyToLeave && _state != PlaybackState.NoStream)
                return;

            ulong channelId = _channelId is null ? 0 : (ulong)_channelId;
            if (GetChannels(getRequesterIds()).Select(t => t.Item1.Id).Contains(channelId))
                return;

            _state = PlaybackState.TryingToJoin;
            while (!cancellationToken.IsCancellationRequested)
            {
                _audioClient = await TryJoinAsync(getRequesterIds);
                if (_audioClient is not null)
                    return;

                await Task.Delay(RetryDelayMs, CancellationToken.None);
            }
        }

        private async Task<IAudioClient?> TryJoinAsync(Func<ulong[]> getRequesterIds)
        {
            var channelsInfo = GetChannels(getRequesterIds());
            if (channelsInfo.Length == 0)
                return null;

            (IVoiceChannel voiceChannel, IGuildUser voiceUser) = channelsInfo[0];
            _audioClient = await voiceChannel.ConnectAsync(true, false);
            if (_audioClient is null)
                return null;

            _logger.Here().Information("Joined [{VoiceChannel}] for [{UserName}]", voiceChannel.Name, voiceUser.Username);
            return _audioClient;
        }
    }
}
