﻿using AsyncEvent;
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
            _logger.Here().Information("{Source}\t{Message}", msg.Source, msg.Message);
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
            await CommandRecieved.InvokeAsync(this, new CommandRecievedArgs(command, commandMessage, info));
        }
    }
}
