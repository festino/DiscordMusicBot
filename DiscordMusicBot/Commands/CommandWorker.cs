using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot
{
    public class CommandWorker : ICommandWorker
    {
        private readonly bool REPLY_UNKNOWN_COMMAND = false;
        private readonly Dictionary<string, Func<RequestQueue, IAudioStreamer, ICommandExecutor>> executorConstructors = new();
        private readonly Dictionary<ulong, Dictionary<string, ICommandExecutor>> executors = new();

        private readonly IAudioDownloader _downloader;
        private readonly Func<ulong, IAudioStreamer> _streamerConstructor;

        public CommandWorker(
            Dictionary<string, Func<RequestQueue, IAudioStreamer, ICommandExecutor>> executorConstructors,
            IAudioDownloader downloader,
            Func<ulong, IAudioStreamer> streamerConstructor)
        {
            _downloader = downloader;
            _streamerConstructor = streamerConstructor;
            foreach (var executor in executorConstructors)
                this.executorConstructors.Add(executor.Key.ToLower(), executor.Value);
        }

        public async Task<CommandResponse> OnCommand(string command, string args, DiscordMessageInfo discordMessageInfo)
        {
            Dictionary<string, ICommandExecutor> guildExecutors;
            if (executors.ContainsKey(discordMessageInfo.GuildId))
            {
                guildExecutors = executors[discordMessageInfo.GuildId];
            }
            else
            {
                guildExecutors = new();
                IAudioStreamer streamer = _streamerConstructor(discordMessageInfo.GuildId);
                RequestQueue guildQueue = new(_downloader, streamer);
                foreach (var executorConstructor in executorConstructors)
                    guildExecutors.Add(executorConstructor.Key, executorConstructor.Value(guildQueue, streamer));

                executors.Add(discordMessageInfo.GuildId, guildExecutors);
            }

            command = command.ToLower();
            if (guildExecutors.ContainsKey(command))
                return await guildExecutors[command].Execute(args, discordMessageInfo);

            if (REPLY_UNKNOWN_COMMAND)
                return new CommandResponse(CommandResponseStatus.ERROR, "unknown command");

            return new CommandResponse(CommandResponseStatus.OK, "");
        }
    }
}
