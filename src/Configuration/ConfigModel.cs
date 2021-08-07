using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace DevExchangeBot.Configuration
{
    /// <summary>
    ///     This class represent all the global settings available for the bot and set by the owner.
    ///     It reflect config.json file.
    /// </summary>
    public class ConfigModel
    {
        public string Token { get; set; }
        public string Color { get; set; }
        public ulong GuildId { get; set; }

        public EmojiConfigModel Emoji { get; set; }

        // ReSharper disable once CollectionNeverUpdated.Global
        public IList<string> RawHeartBoardEmojis { get; set; }
        public int HeartboardRequirement { get; set; }

        public static string GetEmbedConfiguration()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DevExchangeBot.config.json");
            using var streamReader =
                new StreamReader(stream ??
                                 throw new InvalidOperationException("Embed configuration could not be found!"));
            return streamReader.ReadToEnd();
        }
    }
}
