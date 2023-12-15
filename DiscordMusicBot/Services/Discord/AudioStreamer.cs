using AsyncEvent;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Services.Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DiscordMusicBot.AudioRequesting.IAudioStreamer;

namespace DiscordMusicBot.AudioRequesting
{
    public class AudioStreamer : IAudioStreamer
    {
        private const int RETRY_DELAY_MS = 500;

        private readonly DiscordSocketClient _client;
        private readonly ulong _guildId;
        private IAudioClient? _audioClient = null;

        private CancellationTokenSource _cancellationTokenSource = new();
        private PlaybackState _state = PlaybackState.NO_STREAM;
        private Task _playTask = Task.CompletedTask;
        private Video? _currentVideo = null;
        private DateTime _startTime = DateTime.Now;

        public event AsyncEventHandler<PlaybackEndedArgs>? Finished;

        public AudioStreamer(DiscordBot bot, ulong guildId)
        {
            _client = bot.Client;
            _guildId = guildId;
        }

        public AudioInfo? GetCurrentTime()
        {
            if (_state == PlaybackState.NO_STREAM)
                return null;

            if (_currentVideo is null)
                return null;

            return new AudioInfo(_currentVideo, DateTime.Now - _startTime);
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
            Console.WriteLine("Stopped audio streamer");
        }

        private async Task StartNewAsync(Video video, Stream pcmStream, CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting");
            _currentVideo = video;
            _playTask = PlayAudio(pcmStream, cancellationToken);
            await _playTask;
            Console.WriteLine("Finished");

            PlaybackEndedStatus status = PlaybackEndedStatus.OK;
            if (cancellationToken.IsCancellationRequested)
                status = PlaybackEndedStatus.STOPPED;
            if (_audioClient is null)
                status = PlaybackEndedStatus.DISCONNECTED;
            Task? task = Finished?.InvokeAsync(this, new PlaybackEndedArgs(status, video));
            if (task is not null)
                await task;
        }

        private async Task PlayAudio(Stream pcmStream, CancellationToken cancellationToken)
        {
            if (_audioClient is null)
            {
                Console.WriteLine($"Audio client is null!");
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
                    Console.WriteLine("Audio client was disconnected!");
                    Console.WriteLine(e);
                    _audioClient = null;
                }
            }
            _currentVideo = null;
            _state = PlaybackState.NO_STREAM;
        }

        private async Task PlayAsync(IAudioClient audioClient, Stream pcmStream, CancellationToken cancellationToken)
        {
            using (pcmStream)
            {
                using (var output = new VolumeStream())
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    _state = PlaybackState.PLAYING;
                    _startTime = DateTime.Now;
                    try
                    {
                        Task[] tasks = new Task[] {
                            pcmStream.CopyToAsync(output, cancellationToken),
                            output.CopyToAsync(discord, cancellationToken),
                        };
                        await Task.WhenAll(tasks);
                    }
                    finally
                    {
                        await discord.FlushAsync(cancellationToken);
                    }
                }
            }
        }

        private Tuple<IVoiceChannel, IGuildUser>? FindChannel(ulong[] requesterIds)
        {
            var guild = _client.GetGuild(_guildId);
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

            Console.WriteLine($"Joined [{voiceChannel.Name}] for [{voiceUser}]");
            return _audioClient;
        }
    }
}
