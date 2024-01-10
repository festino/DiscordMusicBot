namespace DiscordMusicBot.AudioRequesting
{
    public enum PlaybackState
    {
        NoStream,
        TryingToJoin,
        Reconnecting,
        ReadyToLeave,
        Playing,
        Paused
    }
}