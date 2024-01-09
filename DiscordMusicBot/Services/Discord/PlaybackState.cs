namespace DiscordMusicBot.AudioRequesting
{
    internal enum PlaybackState
    {
        NoStream,
        TryingToJoin,
        Reconnecting,
        ReadyToLeave,
        Playing,
        Paused
    }
}