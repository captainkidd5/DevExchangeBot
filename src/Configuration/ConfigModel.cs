using System.Collections.Generic;

namespace DevExchangeBot.Configuration
{
    public class ConfigModel
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public string Color { get; set; }

        public EmojiConfigModel Emoji { get; set; }

        public IList<string> RawHeartBoardEmojis { get; set; }
        public int HeartboardRequirement { get; set; }
    }
}
