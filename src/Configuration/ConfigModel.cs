using System.Collections.Generic;

namespace DevExchangeBot.Configuration
{
    /// <summary>
    /// This class represent all the global settings available for the bot and set by the owner.
    /// It reflect config.json file.
    /// </summary>
    public class ConfigModel
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public string Color { get; set; }
        public ulong GuildId { get; set; }

        public EmojiConfigModel Emoji { get; set; }

        public IList<string> RawHeartBoardEmojis { get; set; }
        public int HeartboardRequirement { get; set; }

        public RoleMenuConfigModel RoleMenu { get; set; }
    }
}
