using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext.Attributes;
using DevExchangeBot.Storage;

namespace DevExchangeBot.RoleMenuSystem
{
    public class RoleReaction : BaseCommandModule
    {
        [Command("addRole"), Description("Adds a role to Display in the Role Menu (ulong roleID, ulong/string emojiID)")]
        public async Task AddRole(CommandContext _ctx, ulong _roleID, ulong _emojiID)
        {
            DiscordRole role = _ctx.Guild.GetRole(_roleID);

            if (!DiscordEmoji.TryFromGuildEmote(_ctx.Client, _emojiID, out DiscordEmoji emoji))
            {
                await _ctx.RespondAsync($"Unable to find Emoji {_emojiID}");
            }

            if (StorageContext.Model.RolesByID == null) StorageContext.Model.RolesByID = new Dictionary<ulong, ulong>();
            StorageContext.Model.RolesByID.Add(_emojiID, _roleID);

            DiscordChannel channel = await _ctx.Client.GetChannelAsync(StorageContext.Model.RoleMenuChannelID);
            await UpdateRoleMenu(await channel.GetMessageAsync(StorageContext.Model.RoleMenuMsgID));

            await _ctx.RespondAsync($"Added role: {role.Name} {emoji}");
        }

        [Command("addRole"), Description("Adds a role to Display in the Role Menu (ulong roleID, ulong/string emojiID)")]
        public async Task AddRole(CommandContext _ctx, ulong _roleID, string _emojiName)
        {
            DiscordRole role = _ctx.Guild.GetRole(_roleID);

            if (!DiscordEmoji.TryFromUnicode(_ctx.Client, _emojiName, out DiscordEmoji emoji))
            {
                await _ctx.RespondAsync($"Unable to find Emoji {_emojiName}");
            }

            if (StorageContext.Model.RolesByName == null) StorageContext.Model.RolesByName = new Dictionary<string, ulong>();
            StorageContext.Model.RolesByName.Add(_emojiName, _roleID);

            DiscordChannel channel = await _ctx.Client.GetChannelAsync(StorageContext.Model.RoleMenuChannelID);
            await UpdateRoleMenu(await channel.GetMessageAsync(StorageContext.Model.RoleMenuMsgID));

            await _ctx.RespondAsync($"Added role: {role.Name} {emoji}");
        }

        /// <summary>
        /// Creates A Role Menu in the channel which the command is Executed
        /// </summary>
        [Command("roleMenu")]
        public async Task CreateRoleMenu(CommandContext _ctx)
        {
            DiscordEmbed embed = CreateMenuEmbed(_ctx.Guild);

            DiscordMessage msg = await _ctx.Client.SendMessageAsync(_ctx.Channel, embed);

            if (StorageContext.Model.RolesByName != null) // Unicode Emojis
            {
                foreach (string emojiName in StorageContext.Model.RolesByName.Keys)
                {
                    DiscordEmoji.TryFromUnicode(emojiName, out DiscordEmoji emoji);
                    await msg.CreateReactionAsync(emoji);
                }
            }

            if (StorageContext.Model.RolesByID != null) // Unicode Emojis
            {
                foreach (ulong emojiID in StorageContext.Model.RolesByID.Keys)
                {
                    DiscordEmoji.TryFromGuildEmote(_ctx.Client, emojiID, out DiscordEmoji emoji);
                    await msg.CreateReactionAsync(emoji);
                }
            }

            StorageContext.Model.RoleMenuMsgID = msg.Id;
            StorageContext.Model.RoleMenuChannelID = msg.ChannelId;
        }

        /// <summary>
        /// Assigns corrosponding Role to the User
        /// </summary>
        public static async Task OnReacted(DiscordClient _sender, MessageReactionAddEventArgs _event)
        {
            if (StorageContext.Model.RoleMenuMsgID == 0 || StorageContext.Model.RoleMenuChannelID == 0) return; // Role Menu Not Created
            if (_event.User.IsBot || _event.Message.Id != StorageContext.Model.RoleMenuMsgID) return; // Reaction Not On Role Menu

            ulong roleID;

            if (_event.Emoji.Id == 0) // Unicode
            {
                StorageContext.Model.RolesByName.TryGetValue(_event.Emoji.Name, out roleID);
            }
            else // Custom Guild Emoji
            {
                StorageContext.Model.RolesByID.TryGetValue(_event.Emoji.Id, out roleID);
            }

            DiscordMember member = (DiscordMember)_event.User;

            await member.GrantRoleAsync(_event.Guild.GetRole(roleID)).ConfigureAwait(false);
            await _event.Message.DeleteReactionAsync(_event.Emoji, _event.User).ConfigureAwait(false);
        }

        private async Task UpdateRoleMenu(DiscordMessage _msg)
        {
            DiscordEmbed embed = CreateMenuEmbed(_msg.Channel.Guild);

            await _msg.ModifyAsync(embed).ConfigureAwait(false);

            await _msg.DeleteAllReactionsAsync();

            foreach (string emojiName in StorageContext.Model.RolesByName.Keys) // Unicode
            {
                DiscordEmoji.TryFromUnicode(emojiName, out DiscordEmoji emoji);
                await _msg.CreateReactionAsync(emoji);
            }

            foreach (ulong emojiID in StorageContext.Model.RolesByID.Keys) // Custom Guild ID
            {
                DiscordEmoji.TryFromGuildEmote(Program.Client, emojiID, out DiscordEmoji emoji);
                await _msg.CreateReactionAsync(emoji);
            }
        }

        private static DiscordEmbed CreateMenuEmbed(DiscordGuild _guild)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Welcome to {_guild.Name}! These roles will help you present yourself in our server.");
            sb.AppendLine($"Simply react to the message with the role you want and I will automagically give it to you.\n");
            sb.AppendLine($"Note: If you wish you can click multiple reactions for more roles!\n");

            foreach (KeyValuePair<string, ulong> pair in StorageContext.Model.RolesByName) // Unicode
            {
                DiscordRole role = _guild.GetRole(pair.Value);
                sb.AppendLine($"{pair.Key} - {role.Name}");
            }

            foreach (KeyValuePair<ulong, ulong> pair in StorageContext.Model.RolesByID) // Custom Guild ID
            {
                DiscordRole role = _guild.GetRole(pair.Value);
                DiscordEmoji.TryFromGuildEmote(Program.Client, pair.Key, out DiscordEmoji emoji);
                sb.AppendLine($"{emoji} - {role.Name}");
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#5c89fb"),
                Title = "Automatic Role Assignment",
                Description = sb.ToString()
            };

            return embedBuilder.Build();
        }

        public static void Initialize(DiscordClient _client)
        {
            if (StorageContext.Model.RolesByID == null)
            {
                StorageContext.Model.RolesByID = new Dictionary<ulong, ulong>();
            }
            if (StorageContext.Model.RolesByName == null)
            {
                StorageContext.Model.RolesByName = new Dictionary<string, ulong>();
            }

            _client.MessageReactionAdded += OnReacted;
        }
    }
}
