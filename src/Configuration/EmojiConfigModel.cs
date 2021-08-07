// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace DevExchangeBot.Configuration
{
    /// <summary>
    ///     This class contains all the custom emoji strings for the bot.
    ///     It's part of <see cref="ConfigModel" />
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EmojiConfigModel
    {
        public string Confetti { get; set; }
        public string GoldMedal { get; set; }
        public string SilverMedal { get; set; }
        public string BronzeMedal { get; set; }
        public string Success { get; set; }
        public string Failure { get; set; }
        public string CriticalError { get; set; }
        public string AccessDenied { get; set; }
        public string Warning { get; set; }
    }
}
