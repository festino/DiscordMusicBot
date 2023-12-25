using AsyncEvent;

namespace DiscordMusicBot.Abstractions
{
    public interface ICommandSender
    {
        record MessageRecievedArgs(DiscordMessageInfo MessageInfo, string Content);
        event AsyncEventHandler<MessageRecievedArgs>? MessageRecieved;

        record CommandRecievedArgs(string Command, string Message, DiscordMessageInfo MessageInfo);
        event AsyncEventHandler<CommandRecievedArgs>? CommandRecieved;
    }
}
