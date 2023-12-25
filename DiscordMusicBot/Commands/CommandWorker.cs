using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.Executors;
using DiscordMusicBot.Extensions;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using static DiscordMusicBot.Abstractions.ICommandSender;

namespace DiscordMusicBot
{
    public class CommandWorker : ICommandWorker
    {
        private const string UnknownCommandKey = "";
        private readonly bool ReplyUnknownCommand = false;

        private readonly Dictionary<Type, string> ExecutorsCommands = new()
        {
            { typeof(UnknownCommandExecutor), UnknownCommandKey },
            { typeof(HelpCommandExecutor), "help" },
            { typeof(PlayCommandExecutor), "play" },
            { typeof(ListCommandExecutor), "list" },
            { typeof(StopCommandExecutor), "stop" },
            { typeof(SkipCommandExecutor), "skip" },
            { typeof(UndoCommandExecutor), "undo" },
            { typeof(NowCommandExecutor), "now" },
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

        public async Task OnCommandAsync(object sender, CommandRecievedArgs args)
        {
            Dictionary<string, ICommandExecutor> guildExecutors = GetGuildExecutors(args);

            string command = args.Command.ToLower();
            _logger.Here().Information("{UserName} issued command \"{Command}\"", args.MessageInfo.RequesterName, command);
            if (guildExecutors.ContainsKey(command))
                await guildExecutors[command].ExecuteAsync(args.Message, args.MessageInfo);

            if (ReplyUnknownCommand)
                await guildExecutors[UnknownCommandKey].ExecuteAsync(args.Message, args.MessageInfo);
        }

        private Dictionary<string, ICommandExecutor> GetGuildExecutors(CommandRecievedArgs args)
        {
            ulong guildId = args.MessageInfo.GuildId;
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

            var guildWatcher = services.GetRequiredService<IGuildWatcher>();
            var bot = services.GetRequiredService<DiscordBot>();
            guildWatcher.OnCommandAsync(this, args).Wait();
            bot.CommandRecieved += guildWatcher.OnCommandAsync;

            var floatingMessage = services.GetRequiredService<IFloatingMessage>();
            var audioTimer = services.GetRequiredService<IAudioTimer>();
            audioTimer.TimeUpdated += async (s, args) => await floatingMessage.UpdateAsync(MessageFormatUtils.FormatPlayingMessage(args.AudioInfo));
            Task.Run(audioTimer.RunAsync);

            _executors.Add(guildId, guildExecutors);
            return guildExecutors;
        }

        private void ValidateCommands()
        {
            Dictionary<string, Type> commandsExecutors = new();
            foreach ((Type type, string command) in ExecutorsCommands.AsEnumerable())
            {
                if (command.Contains(' '))
                    throw new Exception($"Command cannot contain ' ': \"{command}\" of {type}");

                if (commandsExecutors.ContainsKey(command))
                    throw new Exception($"Duplicating {commandsExecutors[command]} command: \"{command}\" of {type}");

                commandsExecutors.Add(command, type);
            }
        }
    }
}
