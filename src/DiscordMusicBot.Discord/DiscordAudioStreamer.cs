using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Services.Discord.Volume;
using Serilog;

namespace DiscordMusicBot.AudioRequesting
{
    public class DiscordAudioStreamer : IAudioStreamer
    {
        private readonly static string OpusFilepath = "opus.dll";
        private readonly static string SodiumFilepath = "libsodium.dll";

        private readonly static int RejoinIntervalMs = 500;

        // AuditLog merges kicks for ~5 minutes
        private readonly static int KickTimeWindowMs = 5 * 1000 * 60;

        private readonly ILogger _logger;

        private readonly DiscordSocketClient _client;
        private ulong? _guildId = null;
        private ulong? _channelId = null;

        private IAudioClient? _audioClient = null;
        private PlaybackState _state = PlaybackState.NoStream;

        private VolumeStream? _volumeStream = null;

        // dependency injection skill issue
        public ulong? GuildId
        {
            get => _guildId;
            set => _guildId = value;
        }

        public bool IsConnected { get => _audioClient != null; }

        public DiscordAudioStreamer(ILogger logger, DiscordSocketClient client)
        {
            _logger = logger;
            _client = client;

            List<string> missingFiles = new();
            if (!File.Exists(OpusFilepath))
            {
                missingFiles.Add(OpusFilepath);
            }
            if (!File.Exists(SodiumFilepath))
            {
                missingFiles.Add(SodiumFilepath);
            }
            if (missingFiles.Count > 0)
            {
                throw new FileNotFoundException($"Could not find files: {string.Join(", ", missingFiles)}");
            }
        }

        public TimeSpan? GetCurrentTime()
        {
            if (_state != PlaybackState.Playing && _state != PlaybackState.Paused)
                return null;

            if (_volumeStream is null)
                return null;

            return _volumeStream.TimeRead;
        }

        public async Task LeaveAsync(int delayMs, CancellationToken cancellationToken)
        {
            if (delayMs > 0)
            {
                _state = PlaybackState.ReadyToLeave;
                await Task.Delay(delayMs, cancellationToken);
            }
            _logger.Here().Information("Leaving voice channel");
            _audioClient?.StopAsync();
            _audioClient = null;
            _channelId = null;
            _state = PlaybackState.NoStream;
        }

        public async Task PlayAudioAsync(Stream pcmStream, Func<ulong[]> getRequesterIds, CancellationToken cancellationToken)
        {
            if (_audioClient is null)
            {
                _logger.Here().Error("Audio client is null!");
                return;
            }

            try
            {
                await PlayAsync(pcmStream, getRequesterIds, cancellationToken);
            }
            catch (Exception e)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.Here().Error("Audio client was disconnected!\n{Exception}", e);
                    if (e is OperationCanceledException)
                        await LeaveAsync(0, CancellationToken.None);
                }
            }
            _state = PlaybackState.NoStream;
        }

        public async Task JoinAsync(Func<ulong[]> getRequesterIds, CancellationToken cancellationToken)
        {
            if (_state != PlaybackState.ReadyToLeave
                && _state != PlaybackState.NoStream
                && _state != PlaybackState.Reconnecting)
            {
                _logger.Here().Error("Could not join in state {State}", _state);
                return;
            }

            ulong channelId = _channelId is null ? 0 : (ulong)_channelId;
            if (GetChannels(getRequesterIds()).Select(t => t.Item1.Id).Contains(channelId))
                return;

            _logger.Here().Debug("Joining voice channel");
            _state = PlaybackState.TryingToJoin;
            while (!cancellationToken.IsCancellationRequested)
            {
                _audioClient = await TryJoinAsync(getRequesterIds);
                if (_audioClient is not null)
                    return;

                await Task.Delay(RejoinIntervalMs, CancellationToken.None);
            }
        }

        private async Task PlayAsync(Stream pcmStream, Func<ulong[]> getRequesterIds, CancellationToken cancellationToken)
        {
            // TODO try load average volume 
            using (pcmStream)
            using (_volumeStream = new VolumeStream(new AverageVolumeBalancer(), null, pcmStream))
            {
                while (await TryPlayAsync(_volumeStream, cancellationToken))
                {
                    _state = PlaybackState.Reconnecting;
                    await JoinAsync(getRequesterIds, cancellationToken);
                }

                if (_volumeStream.AverageVolume is not null)
                {
                    // TODO save average volume 
                }
            }
        }

        private async Task<bool> TryPlayAsync(Stream stream, CancellationToken cancellationToken)
        {
            IAudioClient? audioClient = _audioClient;
            if (audioClient is null)
            {
                _logger.Here().Error("Audio client is null!");
                return true;
            }

            using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                _state = PlaybackState.Playing;
                bool wasDisconnected = false;
                try
                {
                    await stream.CopyToAsync(discord, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return false;

                    // if bot was disconnected cancellationToken will not be set
                    // causing discord.FlushAsync to hang forever
                    wasDisconnected = true;
                    _channelId = null;
                    _audioClient = null;

                    if (await WasKickedAsync())
                        throw;

                    _logger.Here().Warning("Inner task was cancelled! Reconnecting voice channel...\n{Exception}", e);
                    return true;
                }
                finally
                {
                    if (!wasDisconnected && !cancellationToken.IsCancellationRequested)
                        await discord.FlushAsync(cancellationToken);
                }
            }
            return false;
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

            _channelId = voiceChannel.Id;
            _logger.Here().Information("Joined [{VoiceChannel}] for [{UserName}]", voiceChannel.Name, voiceUser.Username);
            return _audioClient;
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

        private async Task<bool> WasKickedAsync()
        {
            if (_guildId is null)
                throw new InvalidOperationException("Guild id is not initialized!");

            IGuild? guild = _client.GetGuild((ulong)_guildId);
            if (guild is null)
            {
                _logger.Here().Error("Guild {GuildId} is null", (ulong)_guildId);
                return true;
            }

            var user = await guild.GetCurrentUserAsync();
            if (!user.GuildPermissions.Has(GuildPermission.ViewAuditLog))
            {
                _logger.Here().Warning("Missing permission {Permission}, {BotName} has only {BotPermissions}",
                                       GuildPermission.ViewAuditLog, user.DisplayName,
                                       string.Join(", ", user.GuildPermissions.ToList()));
                return true;
            }

            var auditEntries = await guild.GetAuditLogsAsync(limit: 10, actionType: ActionType.MemberDisconnected);
            foreach (var audit in auditEntries)
            {
                if ((DateTime.UtcNow - audit.CreatedAt.DateTime).TotalMilliseconds > KickTimeWindowMs)
                    continue;

                _logger.Here().Information("Possibly kicked by {UserName}", audit.User.GlobalName);
                return true;
            }
            return false;
        }
    }
}
