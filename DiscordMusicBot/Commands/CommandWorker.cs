using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.Executors;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Services.Discord;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DiscordMusicBot
{
    public class CommandWorker : ICommandWorker
    {
        private readonly bool ReplyUnknownCommand = false;
        private readonly Dictionary<Type, string> ExecutorsCommands = new()
        {
            { typeof(PlayCommandExecutor), "play" },
            { typeof(ListCommandExecutor), "list" },
            { typeof(StopCommandExecutor), "stop" },
            { typeof(SkipCommandExecutor), "skip" },
            { typeof(UndoCommandExecutor), "undo" },
            { typeof(NowCommandExecutor), "now" },
            { typeof(HelpCommandExecutor), "help" },
        };

        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly Dictionary<ulong, Dictionary<string, ICommandExecutor>> _executors = new();

        public CommandWorker(ILogger logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            ValidateCommands();
        }

        public async Task<CommandResponse> OnCommandAsync(string command, string args, DiscordMessageInfo discordMessageInfo)
        {
            Dictionary<string, ICommandExecutor> guildExecutors = GetGuildExecutors(discordMessageInfo.GuildId);

            command = command.ToLower();
            _logger.Here().Information("{UserName} issued command \"{Command}\"", discordMessageInfo.RequesterName, command);
            if (guildExecutors.ContainsKey(command))
                return await guildExecutors[command].Execute(args, discordMessageInfo);

            if (ReplyUnknownCommand)
                return new CommandResponse(CommandResponseStatus.Error, "unknown command");

            return new CommandResponse(CommandResponseStatus.Ok, "");
        }

        private Dictionary<string, ICommandExecutor> GetGuildExecutors(ulong guildId)
        {
            if (_executors.ContainsKey(guildId))
                return _executors[guildId];

            Dictionary<string, ICommandExecutor> guildExecutors = new();
            IServiceProvider services = _scopeFactory.CreateScope().ServiceProvider;
            // cannot add guild id to the scope context
            services.GetRequiredService<IAudioStreamer>().GuildId = guildId;
            foreach (var executor in services.GetServices<ICommandExecutor>())
            {
                guildExecutors.Add(ExecutorsCommands[executor.GetType()], executor);
            }

            _executors.Add(guildId, guildExecutors);
            return guildExecutors;
        }

        private void ValidateCommands()
        {
            Dictionary<string, Type> commandsExecutors = new();
            foreach ((Type type, string command) in ExecutorsCommands.AsEnumerable())
            {
                if (command.Length == 0)
                    throw new Exception($"Command cannot be empty: {type}");

                if (command.Contains(' '))
                    throw new Exception($"Command cannot contain ' ': \"{command}\" of {type}");

                if (commandsExecutors.ContainsKey(command))
                    throw new Exception($"Duplicating {commandsExecutors[command]} command: \"{command}\" of {type}");

                commandsExecutors.Add(command, type);
            }
        }
    }
}
