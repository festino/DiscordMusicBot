using AsyncEvent;
using DiscordMusicBot.Services.Discord;

namespace DiscordMusicBot.Abstractions
{
    public interface ICommandSender
    {
        record CommandRecievedArgs(string Command, string Message, DiscordMessageInfo MessageInfo);
        event AsyncEventHandler<CommandRecievedArgs>? CommandRecieved;
    }
}
