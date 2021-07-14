using System.Collections.Generic;

namespace DevExchangeBot.Storage.Models
{
    public class NoodlesModel
    {
        private static readonly Dictionary<NoodlesMoodType, string> _moodStrings = new Dictionary<NoodlesMoodType, string>()
        {
            { NoodlesMoodType.Happy, "Noodles is feeling happy!" },
            { NoodlesMoodType.Hungry, "Noodles is looking hungry." },
            { NoodlesMoodType.VeryHungry, "Noodles is starving!" },
            { NoodlesMoodType.Dirty, "Noodles could use a bath." },
            { NoodlesMoodType.VeryDirty, "Noodles' fur is infested with fleas!" },
            { NoodlesMoodType.Bored, "Noodles is looking bored." },
            { NoodlesMoodType.VeryBored, "Noodles is dying from boredom!" },
        };

        public int Hunger { get; set; }
        public int Sanity { get; set; }
        public int Joy { get; set; }

        public int Level { get; set; }
        public int Exp { get; set; }

        public NoodlesModel()
        {
            Hunger = 100;
            Sanity = 100;
            Joy = 100;

            Level = 1;
            Exp = 0;
        }

        public NoodlesMoodType GetOverallMood()
        {
            if (Hunger < 25)
                return NoodlesMoodType.VeryHungry;

            if (Hunger < 50)
                return NoodlesMoodType.Hungry;

            if (Sanity < 25)
                return NoodlesMoodType.VeryDirty;

            if (Sanity < 50)
                return NoodlesMoodType.Dirty;

            if (Joy < 25)
                return NoodlesMoodType.VeryBored;

            if (Joy < 50)
                return NoodlesMoodType.Bored;

            return NoodlesMoodType.Happy;
        }

        public string GetString(NoodlesMoodType mood)
        {
            if (!_moodStrings.ContainsKey(mood))
                return null;

            return _moodStrings[mood];
        }
    }
}
