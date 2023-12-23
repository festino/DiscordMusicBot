using AsyncEvent;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Abstractions;
using DiscordMusicBot.Extensions;
using Serilog;
using static DiscordMusicBot.Abstractions.ICommandSender;

namespace DiscordMusicBot.Services.Discord
{
    public class DiscordBot : ICommandSender
    {
        private readonly ILogger _logger;

        private readonly string _commandPrefix;
        private readonly DiscordSocketClient _client;
        private readonly string _token;

        public event AsyncEventHandler<CommandRecievedArgs>? CommandRecieved;

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
            _client.MessageReceived += HandleMessageAsync;
            _client.ButtonExecuted += HandleButtonAsync;
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
            _logger.Here().Information("{Source}\t{Message}", msg.Source, msg.Message);
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (messageParam is not SocketUserMessage message)
                return;

            string text = message.Content;
            if (message.Author.IsBot || !text.StartsWith(_commandPrefix))
                return;

            text = text[_commandPrefix.Length..];
            await HandleCommandAsync(text, message.Author, message.Id, message.Channel);
        }

        private async Task HandleButtonAsync(SocketMessageComponent component)
        {
            await HandleCommandAsync(component.Data.CustomId, component.User, component.Message.Id, component.Message.Channel);
            await component.DeferAsync();
            await component.Message.DeleteAsync();
        }

        private async Task HandleCommandAsync(string text, SocketUser user, ulong messageId, ISocketMessageChannel channel)
        {
            if (CommandRecieved is null)
                return;

            ulong? guildId = (channel as SocketGuildChannel)?.Guild.Id;
            if (guildId is null)
                return;

            int index = text.IndexOf(' ');
            index = index < 0 ? text.Length : index;
            string command = text[..index];
            string commandMessage = text[index..];

            DiscordMessageInfo info = new(user.Username, user.Id, (ulong)guildId, channel.Id, messageId);
            await CommandRecieved.InvokeAsync(this, new CommandRecievedArgs(command, commandMessage, info));
        }
    }
}
