using DiscordMusicBot.Abstractions;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.Executors;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;
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
                    Path.Combine(LogsPath, "log-{Date}.txt"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logTemplate
                )
                .CreateLogger();

            ConfigReader reader = new(
                new YamlConfigParser(),
                new FileConfigStream("config.yml"),
                new FileConfigStream("credentials.yml")
            );
            Config config = new ConfigBuilder(reader).Build();

            ServiceCollection services = new();
            services.AddSingleton(logger);
            services.AddSingleton<IDiscordConfig>(config);
            services.AddSingleton<IYoutubeConfig>(config);
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
            services.AddScoped<INotificationService, DiscordNotificationService>();
            services.AddScoped<IFloatingMessage, FloatingMessage>();
            services.AddScoped<IFloatingMessageController, FloatingMessageController>();
            services.AddScoped<IAudioStreamer, AudioStreamer>();
            services.AddScoped<RequestQueue>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            var bot = serviceProvider.GetRequiredService<DiscordBot>();

            var commandWorker = serviceProvider.GetRequiredService<ICommandWorker>();
            bot.CommandRecieved += commandWorker.OnCommandAsync;

            await bot.RunAsync();
        }
    }
}