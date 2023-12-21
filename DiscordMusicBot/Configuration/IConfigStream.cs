namespace DiscordMusicBot.Configuration
{
    public interface IConfigStream
    {

        string Read();

        void Rewrite(string configStr);
    }
}
