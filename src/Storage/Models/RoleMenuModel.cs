using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;

namespace DevExchangeBot.Storage.Models
{
    public class RoleMenuModel
    {
        public Dictionary<string, ulong> Roles { get; set; }

        public ulong MessageId { get; set; }
        public ulong ChannelId { get; set; }

        public RoleMenuModel()
        {
            Roles = new Dictionary<string, ulong>();
        }

        public DiscordEmoji[] GetAllEmojis(DiscordClient client)
        {
            var emojis = new List<DiscordEmoji>();

            // Goes through every Emoji name in the dictionary and converts it into DiscordEmoji
            foreach (string emojiName in Roles.Keys)
            {
                if (DiscordEmoji.TryFromName(client, $":{emojiName}:", true, out DiscordEmoji emoji))
                    emojis.Add(emoji);
            }

            return emojis.ToArray();
        }

        public bool GetRoleId(DiscordEmoji emoji, out ulong roleId)
        {
            // Run through every RoleBind and if it matches the emoji return its RoleID
            foreach (KeyValuePair<string, ulong> role in Roles)
            {
                if (role.Key == emoji.GetDiscordName())
                {
                    roleId = role.Value;
                    return true;
                }
            }

            // No role has been found
            // TODO: Refactor the line below to use the built-in logging facilities
            Console.WriteLine($"No Role has been found for {emoji.GetDiscordName()}");

            roleId = 0;
            return false;
        }

        public bool HasRole(DiscordRole role)
        {
            foreach (ulong roleId in Roles.Values)
            {
                if (roleId == role.Id)
                    return true;
            }

            return false;
        }

        public void AddRole(DiscordRole role, DiscordEmoji emoji)
        {
            if (HasRole(role))
                return;

            Roles.Add(emoji.GetDiscordName(), role.Id);
        }
    }
}
