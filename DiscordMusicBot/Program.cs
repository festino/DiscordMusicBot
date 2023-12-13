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
		AudioStreamer streamer = new AudioStreamer(bot, downloader);
		RequestQueue queue = new RequestQueue(downloader, streamer);
		YoutubeDataProvider youtubeDataProvider = new YoutubeDataProvider(config);
		var executors = new Dictionary<string, ICommandExecutor>() {
			{ "play", new PlayCommandExecutor(queue, youtubeDataProvider) },
			{ "list", new ListCommandExecutor(queue) },
			{ "stop", new StopCommandExecutor(queue) },
			{ "skip", new SkipCommandExecutor(queue) },
			{ "now", new NowCommandExecutor(queue) }
		};
		bot.CommandRecieved += new CommandWorker(executors).OnCommand;
		await bot.RunAsync();
	}
}