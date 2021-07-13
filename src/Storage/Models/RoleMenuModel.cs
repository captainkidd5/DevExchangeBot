using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExchangeBot.Storage.Models
{
    public class RoleMenuModel
    {
        public List<RoleBind> Roles { get; set; }
        public ulong RoleMenuMsgID { get; set; }
        public ulong RoleMenuChannelID { get; set; }
        public string RoleMenuTitle { get; set; }
        public string RoleMenuDescription { get; set; }

        // DiscordEmoji.FromName() doesn't work with Unicode
        // and DiscordEmoji.FromUnicode() doesn't work with Custom Guild Emojis
        // So we have to do this stupid thing where Unicode emojis have no ID and Guild Emojis have no Unicode
        // and when getting the Role we check if the RoleBind id is 0
        // if it is then we yoink it via DiscordEmoji.FromUnicode() otherwise DiscordEmoji.FromGuildEmote()

        // If anyone wants to fix it => I wish you good luck.

        public struct RoleBind
        {
            public ulong RoleID;
            public ulong EmojiID;
            public string EmojiUnicode;
        }

        public DiscordEmoji[] GetAllEmojis()
        {
            DiscordEmoji[] discordEmojis = new DiscordEmoji[Roles.Count];

            // Run through every RoleBind in the list and add it to the Array
            for (int i = 0; i < discordEmojis.Length; i++)
            {
                if (Roles[i].EmojiID == 0) discordEmojis[i] = DiscordEmoji.FromUnicode(Roles[i].EmojiUnicode);
                else discordEmojis[i] = DiscordEmoji.FromGuildEmote(Program.Client, Roles[i].EmojiID);
            }

            return discordEmojis;
        }

        public ulong GetRoleID(DiscordEmoji _emoji)
        {
            // Run through every RoleBind and if it matches the emoji return its RoleID
            foreach (RoleBind role in Roles)
            {
                if (_emoji.Id == 0)
                {
                    if (role.EmojiUnicode == _emoji.Name)
                    {
                        return role.RoleID;
                    }
                }
                else
                {
                    if (role.EmojiID == _emoji.Id)
                    {
                        return role.RoleID;
                    }
                }
            }

            // No role has been found
            Console.WriteLine($"No Role has been found for {_emoji.Name}");
            return 0;
        }

        public void AddRole(DiscordRole _role, DiscordEmoji _emoji)
        {
            if (_emoji.Id == 0) Roles?.Add(new RoleBind() { RoleID = _role.Id, EmojiUnicode = _emoji.Name });
            else Roles?.Add(new RoleBind() { RoleID = _role.Id, EmojiID = _emoji.Id });
        }
    }
}
