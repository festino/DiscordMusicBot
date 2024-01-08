using Newtonsoft.Json.Linq;

namespace DiscordMusicBot.Services.Youtube
{
    public static class JTokenExtensions
    {
        public static List<string> GetInnerStrings(this JToken containerToken)
        {
            List<string> strings = new();
            GetInnerStrings(containerToken, strings);
            return strings;
        }

        public static List<JToken> FindTokens(this JToken containerToken, string name)
        {
            List<JToken> matches = new();
            FindTokens(containerToken, name, matches);
            return matches;
        }

        private static void FindTokens(JToken containerToken, string name, List<JToken> matches)
        {
            if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {
                    if (child.Name == name)
                    {
                        matches.Add(child.Value);
                    }
                    FindTokens(child.Value, name, matches);
                }
            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    FindTokens(child, name, matches);
                }
            }
        }

        private static void GetInnerStrings(JToken containerToken, List<string> strings)
        {
            if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {
                    GetInnerStrings(child.Value, strings);
                }
            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    GetInnerStrings(child, strings);
                }
            }
            else if (containerToken.Type == JTokenType.String)
            {
                strings.Add(containerToken.ToString());
            }
        }
    }
}
