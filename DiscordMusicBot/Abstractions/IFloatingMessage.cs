﻿namespace DiscordMusicBot.Abstractions
{
    public interface IFloatingMessage
    {
        Task UpdateAsync(string? message);
    }
}
