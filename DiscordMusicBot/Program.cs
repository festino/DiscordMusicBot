// setup dependencies i.e. config
// resolve dependencies (+events in constructors)
// launch discord bot
using DiscordMusicBot;
using DiscordMusicBot.AudioRequesting;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.Executors;
using DiscordMusicBot.Services.Discord;
using DiscordMusicBot.Services.Youtube;

public class Program
{
	public static Task Main(string[] args) => new Program().MainAsync();

	public async Task MainAsync()
	{
		Config config = new Config("config.yml", "credentials.yml");
		DiscordBot bot = new DiscordBot(config);
		IAudioDownloader downloader = new YoutubeAudioDownloader();
		YoutubeDataProvider youtubeDataProvider = new YoutubeDataProvider(config);
		var executors = new Dictionary<string, Func<RequestQueue, ICommandExecutor>>() {
			{ "play", (queue) => new PlayCommandExecutor(queue, youtubeDataProvider) },
			{ "list", (queue) => new ListCommandExecutor(queue) },
			{ "stop", (queue) => new StopCommandExecutor(queue) },
			{ "skip", (queue) => new SkipCommandExecutor(queue) },
			{ "undo", (queue) => new UndoCommandExecutor(queue) },
			{ "now", (queue) => new NowCommandExecutor(queue) },
			{ "help", (queue) => new HelpCommandExecutor() },
		};
		CommandWorker worker = new(executors, downloader, (guildId) => new AudioStreamer(bot, guildId));
		bot.CommandRecieved += worker.OnCommand;
		await bot.RunAsync();
	}
}