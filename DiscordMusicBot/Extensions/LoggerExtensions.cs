using System.Runtime.CompilerServices;

namespace DiscordMusicBot.Extensions
{
    public static class LoggerExtensions
    {
        public static Serilog.ILogger Here(
            this Serilog.ILogger logger,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return logger
            .ForContext("ClassName", Path.GetFileNameWithoutExtension(sourceFilePath))
            .ForContext("MemberName", memberName)
            .ForContext("LineNumber", sourceLineNumber);

        }
    }
}
