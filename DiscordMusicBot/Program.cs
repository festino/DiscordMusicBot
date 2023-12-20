// setup dependencies i.e. config
// resolve dependencies (+events in constructors)
// launch discord bot
using DiscordMusicBot;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.Executors;
using DiscordMusicBot.Configuration;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Program
{
	public static Task Main(string[] args) => new Program().MainAsync();

	public async Task MainAsync()
	{
		ConfigReader reader = new(
			new YamlConfigParser(),
			new FileConfigStream("config.yml"),
			new FileConfigStream("credentials.yml")
		);
		Config config = new ConfigBuilder(reader).Build();

		ServiceCollection services = new();
		services.AddLogging(builder => builder.AddConsole());
		services.AddSingleton<IDiscordConfig>(config);
		services.AddSingleton<IYoutubeConfig>(config);
		services.AddSingleton<DiscordBot>();
		services.AddSingleton<ICommandWorker, CommandWorker>();
		services.AddSingleton<IAudioDownloader, YoutubeAudioDownloader>();
		services.AddSingleton<IYoutubeDataProvider, YoutubeDataProvider>();

		services.AddScoped<ICommandExecutor, PlayCommandExecutor>();
		services.AddScoped<ICommandExecutor, ListCommandExecutor>();
		services.AddScoped<ICommandExecutor, StopCommandExecutor>();
		services.AddScoped<ICommandExecutor, SkipCommandExecutor>();
		services.AddScoped<ICommandExecutor, UndoCommandExecutor>();
		services.AddScoped<ICommandExecutor, NowCommandExecutor>();
		services.AddScoped<ICommandExecutor, HelpCommandExecutor>();

		services.AddScoped<IAudioStreamer, AudioStreamer>();
		services.AddScoped<RequestQueue>();

		ServiceProvider serviceProvider = services.BuildServiceProvider();

		var bot = serviceProvider.GetRequiredService<DiscordBot>();
		var commandWorker = serviceProvider.GetRequiredService<ICommandWorker>();
		bot.CommandRecieved += commandWorker.OnCommand;

		await bot.RunAsync();
	}
}