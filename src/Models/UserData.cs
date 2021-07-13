namespace DevExchangeBot.Models
{
    public class UserData
    {
        public UserData(ulong id = 0)
        {
            Id = id;
        }

        public ulong Id { get; set; }

        public int Xp { get; set; }

        public int Level { get; set; }

        public int XpToNextLevel => Level * 100 + 75;
    }
}
