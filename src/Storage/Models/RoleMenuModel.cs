using DSharpPlus.Entities;
using System;
using System.Collections.Generic;

namespace DevExchangeBot.Storage.Models
{
    public class RoleMenuModel
    {
        public List<RoleBind> Roles { get; set; }

        public ulong MessageId { get; set; }
        public ulong ChannelId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        // TODO: Open an issue on the repository on this matter

        // DiscordEmoji.FromName() doesn't work with Unicode
        // and DiscordEmoji.FromUnicode() doesn't work with Custom Guild Emojis
        // So we have to do this stupid thing where Unicode emojis have no ID and Guild Emojis have no Unicode
        // and when getting the Role we check if the RoleBind id is 0
        // if it is then we yoink it via DiscordEmoji.FromUnicode() otherwise DiscordEmoji.FromGuildEmote()

        // If anyone wants to fix it => I wish you good luck.

        public struct RoleBind
        {
            public ulong RoleId;
            public ulong EmojiId;
            public string EmojiUnicode;
        }

        public DiscordEmoji[] GetAllEmojis()
        {
            DiscordEmoji[] emojis = new DiscordEmoji[Roles.Count];

            // Run through every RoleBind in the list and add it to the Array
            for (int i = 0; i < emojis.Length; i++)
            {
                if (Roles[i].EmojiId == 0) emojis[i] = DiscordEmoji.FromUnicode(Roles[i].EmojiUnicode);
                else emojis[i] = DiscordEmoji.FromGuildEmote(Program.Client, Roles[i].EmojiId);
            }

            return emojis;
        }

        public bool GetRoleId(DiscordEmoji emoji, out ulong roleId)
        {
            // Run through every RoleBind and if it matches the emoji return its RoleID
            foreach (RoleBind role in Roles)
            {
                if (emoji.Id == 0)
                {
                    if (role.EmojiUnicode == emoji.Name)
                    {
                        roleId = role.RoleId;
                        return true;
                    }
                }
                else
                {
                    if (role.EmojiId == emoji.Id)
                    {
                        roleId = role.RoleId;
                        return true;
                    }
                }
            }

            // No role has been found
            // TODO: Refactor the line below to use the built-in logging facilities
            Console.WriteLine($"No Role has been found for {emoji.Name}");

            roleId = 0;
            return false;
        }

        public void AddRole(DiscordRole role, DiscordEmoji emoji)
        {
            if (emoji.Id == 0) Roles?.Add(new RoleBind() { RoleId = role.Id, EmojiUnicode = emoji.Name });
            else Roles?.Add(new RoleBind() { RoleId = role.Id, EmojiId = emoji.Id });
        }
    }
}
