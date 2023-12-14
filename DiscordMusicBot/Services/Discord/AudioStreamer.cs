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
        private Task? _playTask = null;
        private Video? _currentVideo = null;
        private DateTime _startTime = DateTime.Now;

        public event AsyncEventHandler<Video>? Finished;

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

        public async Task JoinAndPlayAsync(Video video, string path, Func<ulong[]> getRequesterIds)
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            await JoinAsync(getRequesterIds, cancellationToken);
            _playTask = StartNewAsync(video, path, cancellationToken);
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
            if (_playTask is null)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new();
            try { await _playTask; } catch { }
            Console.WriteLine("Stopped audio streamer");
        }

        private async Task StartNewAsync(Video video, string path, CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting");

            _currentVideo = video;
            await PlayAudio(path, cancellationToken);

            _currentVideo = null;
            _state = PlaybackState.NO_STREAM;
            Console.WriteLine("Finished");
            if (cancellationToken.IsCancellationRequested)
                return;

            Task? task = Finished?.InvokeAsync(this, video);
            if (task is not null)
                await task;
        }

        private async Task PlayAudio(string path, CancellationToken cancellationToken)
        {
            if (_audioClient is null)
            {
                Console.WriteLine($"Audio client is null!");
                return;
            }

            // TODO start task, that will check if in voice channel etc?, try join
            using (var ffmpeg = CreateStream(path))
            {
                if (ffmpeg is null)
                    return;

                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = _audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    _state = PlaybackState.PLAYING;
                    _startTime = DateTime.Now;
                    try
                    {
                        await output.CopyToAsync(discord, cancellationToken);
                    }
                    finally
                    {
                        await discord.FlushAsync(cancellationToken);
                        await ffmpeg.WaitForExitAsync(cancellationToken);
                    }
                }
            }
            _playTask = null;
        }

        private Process? CreateStream(string path)
        {
            // Probably mp3 cannot be piped (stream mus be seekable)
            // Therefore use "-i <path>" instead of "-i pipe:0"
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -f mp3 -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
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
            if (_audioClient is not null)
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
