using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Abstractions.Messaging;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.Executors;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Configuration.Parsing;
using DiscordMusicBot.Discord.Configuration;
using DiscordMusicBot.Discord.Messaging;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Youtube.Configuration;
using DiscordMusicBot.Youtube.Data;
using DiscordMusicBot.Youtube.Downloading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DiscordMusicBot
{
    public class Program
    {
        private const string LogsPath = "./logs/";

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            const string logTemplate = "{Timestamp:HH:mm:ss.fff} [{Level:u3}] ({ClassName}.{MemberName}:{LineNumber}) {Message:lj}{NewLine}{Exception}";
            ILogger logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: logTemplate
                )
                .WriteTo.File(
                    Path.Combine(LogsPath, "./.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logTemplate
                )
                .CreateLogger();

            CredentialsConfigReader reader = new(
                new YamlConfigParser(),
                new FileConfigStream("config.yml"),
                new FileConfigStream("credentials.yml")
            );
            Config config = new ConfigBuilder(reader).Build();

            var socketConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                               | GatewayIntents.GuildVoiceStates
                               | GatewayIntents.GuildMessages
                               | GatewayIntents.MessageContent
            };
            DiscordSocketClient client = new(socketConfig);

            ServiceCollection services = new();
            services.AddSingleton(logger);
            services.AddSingleton<IDiscordConfig>(config);
            services.AddSingleton<IYoutubeConfig>(config);
            services.AddSingleton(client);
            services.AddSingleton<DiscordBot>();
            services.AddSingleton<ICommandWorker, CommandWorker>();
            services.AddSingleton<IAudioDownloader, YoutubeAudioDownloader>();
            services.AddSingleton<IYoutubeDataProvider, YoutubeDataProvider>();

            services.AddScoped<ICommandExecutor, UnknownCommandExecutor>();
            services.AddScoped<ICommandExecutor, HelpCommandExecutor>();
            services.AddScoped<ICommandExecutor, PlayCommandExecutor>();
            services.AddScoped<ICommandExecutor, ListCommandExecutor>();
            services.AddScoped<ICommandExecutor, StopCommandExecutor>();
            services.AddScoped<ICommandExecutor, SkipCommandExecutor>();
            services.AddScoped<ICommandExecutor, UndoCommandExecutor>();
            services.AddScoped<ICommandExecutor, NowCommandExecutor>();

            services.AddScoped<IGuildWatcher, DiscordGuildWatcher>();
            services.AddScoped<IMessageSender, DiscordMessageSender>();
            services.AddScoped<ISuggestCleaner, SuggestCleaner>();
            services.AddScoped<IFloatingMessage, FloatingMessage>();
            services.AddScoped<IAudioPlayer, AudioPlayer>();
            services.AddScoped<IAudioStreamer, DiscordAudioStreamer>();
            services.AddScoped<RequestQueue>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            var bot = serviceProvider.GetRequiredService<DiscordBot>();

            var commandWorker = serviceProvider.GetRequiredService<ICommandWorker>();
            bot.CommandRecieved += commandWorker.OnCommandAsync;

            await bot.RunAsync();
        }
    }
}