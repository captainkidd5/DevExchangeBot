using System;

namespace DevExchangeBot.Storage.Models
{
    /// <summary>
    ///     This class contains the data for a single user.
    ///     It's mainly used for levelling for now.
    /// </summary>
    public class UserModel
    {
        public UserModel(ulong? id = null)
        {
            Id = id ?? 0;
        }

        public ulong Id { get; }
        public int Exp { get; set; }
        public int Level { get; set; }

        public int ExpToNextLevel => Level * 100 + 75;

        public DateTime LastMessageTime { get; set; }
    }
}
