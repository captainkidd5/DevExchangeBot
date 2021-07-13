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

        // DiscordEmoji.FromName doesn't work with Unicode
        // and DiscordEmoji.FromUnicode doesn't work with Custom Guild Emojis
        // So we have to do this stupid thing where Unicode emojis have no ID and Guild Emojis have no Unicode
        // and when getting the Role we check if the RoleBind id is 0
        // if it is then we yoink it via DiscordEmoji.FromUnicode otherwise DiscordEmoji.FromGuildEmote

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

            for (int i = 0; i < discordEmojis.Length; i++)
            {
                if (Roles[i].EmojiID == 0) discordEmojis[i] = DiscordEmoji.FromUnicode(Roles[i].EmojiUnicode);
                else discordEmojis[i] = DiscordEmoji.FromGuildEmote(Program.Client, Roles[i].EmojiID);
            }

            return discordEmojis;
        }

        public DiscordEmoji GetEmoji(string _emojiName)
        {
            foreach (RoleBind role in Roles)
            {
                if (role.EmojiUnicode == _emojiName)
                {
                    return DiscordEmoji.FromUnicode(_emojiName);
                }
            }

            // No Emoji has been found
            Console.WriteLine($"No Role has been found for {_emojiName}");
            return null;
        }

        public DiscordEmoji GetEmoji(ulong _emojiID)
        {
            foreach (RoleBind role in Roles)
            {
                if (role.EmojiID == _emojiID)
                {
                    return DiscordEmoji.FromGuildEmote(Program.Client, _emojiID);
                }
            }

            // No Emoji has been found
            Console.WriteLine($"No Role has been found for {_emojiID}");
            return null;
        }

        public ulong GetRoleID(DiscordEmoji _emoji)
        {
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

        public void AddRole(ulong _roleID, ulong _emojiID)
        {
            Roles?.Add(new RoleBind() { RoleID = _roleID, EmojiID = _emojiID });
        }

        public void AddRole(ulong _roleID, string _emojiName)
        {
            Roles?.Add(new RoleBind() { RoleID = _roleID, EmojiUnicode = _emojiName });
        }
    }
}
