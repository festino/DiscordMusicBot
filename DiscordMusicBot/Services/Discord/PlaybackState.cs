namespace DiscordMusicBot.AudioRequesting
{
    internal enum PlaybackState
    {
        NoStream,
        TryingToJoin,
        ReadyToLeave,
        Playing,
        Paused
    }
}