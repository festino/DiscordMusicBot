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
        public event AsyncEventHandler<MessageRecievedArgs>? MessageRecieved;

        public DiscordBot(ILogger logger, IDiscordConfig config, DiscordSocketClient client)
        {
            _client = client;
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

            DiscordMessageInfo? messageInfo = GetMessageInfo(message, message.Author);
            if (messageInfo is null) return;

            await TryMessageAsCommandAsync(message, messageInfo);

            await MessageRecieved.InvokeAsync(this, new MessageRecievedArgs(messageInfo, message.Content));
        }

        private async Task TryMessageAsCommandAsync(SocketUserMessage message, DiscordMessageInfo messageInfo)
        {
            string text = message.Content;
            if (message.Author.IsBot || !text.StartsWith(_commandPrefix))
                return;

            text = text[_commandPrefix.Length..];

            try
            {
                await HandleCommandAsync(text, messageInfo);
            }
            catch (Exception ex)
            {
                _logger.Here().Error("Unhandled exception!\n{Exception}", ex);
            }
        }

        private async Task HandleButtonAsync(SocketMessageComponent component)
        {
            DiscordMessageInfo? messageInfo = GetMessageInfo(component.Message, component.User);
            if (messageInfo is null) return;

            try
            {
                await HandleCommandAsync(component.Data.CustomId, messageInfo);
            }
            catch (Exception ex)
            {
                _logger.Here().Error("Unhandled exception!\n{Exception}", ex);
            }
            await component.DeferAsync();
            await component.Message.DeleteAsync();
        }

        private async Task HandleCommandAsync(string text, DiscordMessageInfo info)
        {
            if (CommandRecieved is null)
                return;

            int index = text.IndexOf(' ');
            index = index < 0 ? text.Length : index;
            string command = text[..index];
            string commandMessage = text[index..];

            await CommandRecieved.InvokeAsync(this, new CommandRecievedArgs(command, commandMessage, info));
        }

        private static DiscordMessageInfo? GetMessageInfo(SocketUserMessage message, SocketUser user)
        {
            ISocketMessageChannel channel = message.Channel;
            ulong? guildId = (channel as SocketGuildChannel)?.Guild.Id;
            if (guildId is null)
                return null;

            return new(user.Username, user.Id, (ulong)guildId, channel.Id, message.Id);
        }
    }
}
