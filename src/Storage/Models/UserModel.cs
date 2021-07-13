
namespace DevExchangeBot.Storage.Models
{
    public class UserModel
    {
        public UserModel(ulong? id = null)
        {
            Id = id ?? 0;
        }

        public ulong Id { get; set; }
        public int Exp { get; set; }
        public int Level { get; set; }

        public int ExpToNextLevel => Level * 100 + 75;
    }
}
