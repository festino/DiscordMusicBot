namespace DiscordMusicBot.AudioRequesting
{
    internal enum PlaybackState
    {
        NoStream,
        TryingToJoin,
        Playing,
        Paused
    }
}