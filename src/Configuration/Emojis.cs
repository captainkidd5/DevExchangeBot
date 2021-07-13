using System.IO;
using Newtonsoft.Json.Linq;

namespace DevExchangeBot.Configuration
{
    public static class Emojis
    {
        static Emojis()
        {
            if (!File.Exists("emojis.json"))
                throw new FileNotFoundException("Emojis file has not been found", "emojis.json");

            var json = JObject.Parse(File.ReadAllText("emojis.json"));

            Confetti = (ulong) json[nameof(Confetti)] == 0 ? ":tada:" : $"<:_:{(ulong) json[nameof(Confetti)]}>";
            Medal = (ulong) json[nameof(Medal)] == 0 ? ":medal:" : $"<:_:{(ulong) json[nameof(Medal)]}>";
            Ok = (ulong) json[nameof(Ok)] == 0 ? ":white_check_mark:" : $"<:_:{(ulong) json[nameof(Ok)]}>";
            CriticalError = (ulong) json[nameof(CriticalError)] == 0 ? ":bangbang:" : $"<:_:{(ulong) json[nameof(CriticalError)]}>";
            AccessDenied = (ulong) json[nameof(AccessDenied)] == 0 ? ":raised_hand:" : $"<:_:{(ulong) json[nameof(AccessDenied)]}>";
            Loading = (ulong) json[nameof(Loading)] == 0 ? ":pause_button:" : $"<:_:{(ulong) json[nameof(Loading)]}>";
            Fail = (ulong) json[nameof(Fail)] == 0 ? ":x:" : $"<:_:{(ulong) json[nameof(Fail)]}>";
        }

        public static string Confetti { get; set; }

        public static string Medal { get; set; }

        public static string Ok { get; set; }

        public static string CriticalError { get; set; }

        public static string AccessDenied { get; set; }

        public static string Loading { get; set; }

        public static string Fail { get; set; }
    }
}
