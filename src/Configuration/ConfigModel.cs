
namespace DevExchangeBot.Configuration
{
    public class ConfigModel
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public string Color { get; set; }

        public EmojiConfigModel Emoji { get; set; }
        public RoleMenuConfigModel RoleMenu { get; set; }
    }
}
