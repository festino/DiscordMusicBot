using AsyncEvent;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Discord.Volume;
using Serilog;
using static DiscordMusicBot.AudioRequesting.IAudioStreamer;

namespace DiscordMusicBot.AudioRequesting
{
    public class AudioStreamer : IAudioStreamer
    {
        private const int RETRY_DELAY_MS = 500;

        private readonly ILogger _logger;

        private readonly DiscordSocketClient _client;
        private ulong? _guildId;
        private IAudioClient? _audioClient = null;

        private CancellationTokenSource _cancellationTokenSource = new();
        private PlaybackState _state = PlaybackState.NO_STREAM;
        private Task _playTask = Task.CompletedTask;
        private Video? _currentVideo = null;
        private VolumeStream? _volumeStream = null;

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
            if (_state == PlaybackState.NO_STREAM)
                return null;

            if (_currentVideo is null || _volumeStream is null)
                return null;

            return new AudioInfo(_currentVideo, _volumeStream.TimeRead);
        }

        public async Task JoinAndPlayAsync(Video video, Stream pcmStream, Func<ulong[]> getRequesterIds)
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            await JoinAsync(getRequesterIds, cancellationToken);
            await StartNewAsync(video, pcmStream, cancellationToken);
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

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new();
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

            PlaybackEndedStatus status = PlaybackEndedStatus.OK;
            if (cancellationToken.IsCancellationRequested)
                status = PlaybackEndedStatus.STOPPED;
            if (_audioClient is null)
                status = PlaybackEndedStatus.DISCONNECTED;
            Task? task = Finished?.InvokeAsync(this, new PlaybackEndedArgs(status, video));
            if (task is not null)
                await task;
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
                    _audioClient = null;
                }
            }
            _currentVideo = null;
            _state = PlaybackState.NO_STREAM;
        }

        private async Task PlayAsync(IAudioClient audioClient, Stream pcmStream, CancellationToken cancellationToken)
        {
            // TODO try load average volume 
            using (pcmStream)
            using (_volumeStream = new VolumeStream(new AverageVolumeBalancer(), null, pcmStream))
            {
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    _state = PlaybackState.PLAYING;
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

        private Tuple<IVoiceChannel, IGuildUser>? FindChannel(ulong[] requesterIds)
        {
            if (_guildId is null)
                throw new InvalidOperationException("Guild id is not initialized!");

            var guild = _client.GetGuild((ulong)_guildId);
            foreach (ulong requesterId in requesterIds)
            {
                IGuildUser? user = guild.GetUser(requesterId);
                if (user is null)
                    continue;

                IVoiceChannel? channel = user.VoiceChannel;
                if (channel is not null && channel.GuildId == _guildId)
                {
                    return Tuple.Create(channel, user);
                }
            }

            return null;
        }

        private async Task JoinAsync(Func<ulong[]> getRequesterIds, CancellationToken cancellationToken)
        {
            if (_audioClient is not null && _audioClient.ConnectionState == ConnectionState.Connected)
                return;

            _state = PlaybackState.TRYING_TO_JOIN;
            while (!cancellationToken.IsCancellationRequested)
            {
                _audioClient = await TryJoinAsync(getRequesterIds);
                if (_audioClient is not null)
                    return;

                await Task.Delay(RETRY_DELAY_MS, CancellationToken.None);
            }
        }

        private async Task<IAudioClient?> TryJoinAsync(Func<ulong[]> getRequesterIds)
        {
            var channelInfo = FindChannel(getRequesterIds());
            if (channelInfo is null)
                return null;

            (IVoiceChannel voiceChannel, IGuildUser voiceUser) = channelInfo;
            _audioClient = await voiceChannel.ConnectAsync(true, false);
            if (_audioClient is null)
                return null;

            _logger.Here().Information("Joined [{VoiceChannel}] for [{UserName}]", voiceChannel.Name, voiceUser.Username);
            return _audioClient;
        }
    }
}
