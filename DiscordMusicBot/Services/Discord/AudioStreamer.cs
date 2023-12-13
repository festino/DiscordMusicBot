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
        private readonly DiscordSocketClient _client;
        private readonly ulong _guildId;
        private IAudioClient? _audioClient = null;

        private PlaybackState _state = PlaybackState.NO_STREAM;
        private Task? _playTask = null;
        private CancellationTokenSource _cancellationTokenSource = new();

        public event AsyncEventHandler<Video>? Finished;

        public AudioStreamer(DiscordBot bot, IAudioDownloader downloader)
        {
            _client = bot.Client;
            _guildId = ...;
        }

        public async Task<bool> JoinAsync(ulong[] requesterIds)
        {
            if (_audioClient is not null)
                return true;

            var channelInfo = FindChannel(requesterIds);
            if (channelInfo is null)
                return false;

            (IVoiceChannel voiceChannel, IGuildUser voiceUser) = channelInfo;
            _audioClient = await voiceChannel.ConnectAsync(true, false);
            if (_audioClient is null)
                return false;

            Console.WriteLine($"Joined [{voiceChannel.Name}] for [{voiceUser}]");
            return true;
        }

        public async Task StartAsync(Video video, string path)
        {
            _playTask = StartNewAsync(video, path, _cancellationTokenSource.Token);
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

            await PlayAudio(path, cancellationToken);

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
            //using (var ffmpeg = CreateStream())
            using (var ffmpeg = CreateStream(path))
            {
                if (ffmpeg is null)
                    return;

                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = _audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    _state = PlaybackState.PLAYING;
                    try
                    {
                        /*Task[] tasks = new Task[]
                        {
                            path.CopyToAsync(ffmpeg.StandardInput.BaseStream),
                            output.CopyToAsync(discord)
                        };
                        await Task.WhenAll(tasks);*/
                        await output.CopyToAsync(discord, cancellationToken);
                    }
                    finally
                    {
                        //await ffmpeg.WaitForExitAsync(cancellationToken);
                        await discord.FlushAsync(cancellationToken);
                    }
                }
                await _audioClient.StopAsync();
            }
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

        private Process? CreateStream()
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -f mp3 -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardInput = true,
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
    }
}
