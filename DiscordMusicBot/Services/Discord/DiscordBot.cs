using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Extensions;
using Serilog;

namespace DiscordMusicBot.Services.Discord
{
    public class DiscordBot
    {
        private const int MAX_MESSAGE_LENGTH = 2000;

        private readonly ILogger _logger;

        private readonly string _commandPrefix;
        private readonly DiscordSocketClient _client;
        private readonly string _token;

        public delegate Task<CommandResponse> OnCommandRecieved(string command, string message, DiscordMessageInfo messageInfo);
        public event OnCommandRecieved? CommandRecieved;

        public DiscordSocketClient Client => _client;

        public DiscordBot(ILogger logger, IDiscordConfig config)
        {
            var socketConfig = new DiscordSocketConfig();
            socketConfig.GatewayIntents = GatewayIntents.Guilds
                | GatewayIntents.GuildVoiceStates
                | GatewayIntents.GuildMessages
                | GatewayIntents.MessageContent;
            _client = new DiscordSocketClient(socketConfig);
            _client.Log += Log;
            _client.MessageReceived += HandleCommandAsync;
            _token = config.DiscordToken;
            _commandPrefix = config.CommandPrefix;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }

        private Task Log(LogMessage msg)
        {
            _logger.Here().Information("{Message}", msg.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (CommandRecieved is null)
                return;

            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message is null)
                return;

            string text = message.Content;
            if (message.Author.IsBot || !text.StartsWith(_commandPrefix))
                return;

            int index = text.IndexOf(' ', _commandPrefix.Length);
            index = index < 0 ? text.Length : index;
            string command = text[_commandPrefix.Length..index];
            string commandMessage = text[index..];
            ulong? guildId = (message.Channel as SocketGuildChannel)?.Guild.Id;
            if (guildId is null)
                return;

            DiscordMessageInfo info = new(message.Author.Username, message.Author.Id, (ulong)guildId, message.Channel.Id, message.Id);
            CommandResponse response = await CommandRecieved.Invoke(command, commandMessage, info);
            if (response.Message.Length == 0)
                return;

            string responseMessage = response.Message;
            if (responseMessage.Length > MAX_MESSAGE_LENGTH)
            {
                responseMessage = responseMessage[..(MAX_MESSAGE_LENGTH - 3)] + "...";
            }
            await message.Channel.SendMessageAsync(responseMessage);
        }
    }
}
