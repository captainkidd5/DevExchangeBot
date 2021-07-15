using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace DevExchangeBot
{
    public static class ClientEvents
    {
        public static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Message.Content.StartsWith(Program.Config.Prefix))
                return;

            if (!StorageContext.Model.Users.TryGetValue(e.Author.Id, out var user))
            {
                user = new UserModel(e.Author.Id);
                StorageContext.Model.AddUser(user);
            }

            var content = e.Message.Content;

            Regex.Replace(content, ":[0-9a-z]+:", "0", RegexOptions.IgnoreCase);
            user.Exp += (int)Math.Round(content.Length / 2D * StorageContext.Model.ExpMultiplier, MidpointRounding.ToZero);

            if (user.Exp > user.ExpToNextLevel)
            {
                user.Exp -= user.ExpToNextLevel;
                user.Level += 1;

                await e.Channel.SendMessageAsync($"{Program.Config.Emoji.Confetti} {e.Author.Mention} advanced to level {user.Level}!");
            }
        }

        public static Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs guildMemberRemoveEventArgs)
        {
            StorageContext.Model.Users.Remove(guildMemberRemoveEventArgs.Member.Id);
            return Task.CompletedTask;
        }

        public static async Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            if (!StorageContext.Model.HeartBoardEnabled || StorageContext.Model.HeartBoardChannel == 0) return;

            var emojiList = Program.Config.RawHeartBoardEmojis.Select(configRawHeartBoardEmoji => ulong.TryParse(configRawHeartBoardEmoji, out var emojiId)
                    ? DiscordEmoji.FromGuildEmote(sender, emojiId)
                    : DiscordEmoji.FromName(sender, configRawHeartBoardEmoji))
                .ToList();

            DiscordMessage originalMessage;
            try
            {
                originalMessage = await e.Channel.GetMessageAsync(e.Message.Id);
            }
            catch (Exception exception)
            {
                sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not get the original message");
                return;
            }

            var heartReactions = originalMessage.Reactions.Where(r => emojiList.Contains(r.Emoji)).ToList();

            if (!heartReactions.Any()) return;

            var reacNumber = heartReactions.Sum(discordReaction => discordReaction.Count);

            DiscordChannel channel;
            try
            {
                channel = e.Guild.GetChannel(StorageContext.Model.HeartBoardChannel);
            }
            catch (Exception exception)
            {
                sender.Logger.LogWarning(new EventId(1, "Warning"), exception, "Could not get starboard channel");
                return;
            }

            var embed = new DiscordEmbedBuilder
                {
                    Description = originalMessage.Content,
                    Color = new DiscordColor(reacNumber switch
                    {
                        >=25 => "#226383", >=10 => "#3c7f9e", >5 => "#5ca0bf", _ => "#8fc9e6",
                    })
                }
                .WithAuthor($"{originalMessage.Author.Username}", null, originalMessage.Author.AvatarUrl)
                .AddField("Original", $"[Click me!]({originalMessage.JumpLink})", true)
                .AddField("Stars",
                    $"{reacNumber switch {>=25 => ":sparkles:", >=10 => ":dizzy:", >5 => ":star2:", _ => ":star:"}} {reacNumber}", true)
                .WithFooter($"{originalMessage.CreationTimestamp.Date.ToLongDateString()}")
                .Build();

            StorageContext.Model.HeartboardMessages ??= new Dictionary<ulong, ulong>();

            if (StorageContext.Model.HeartboardMessages.TryGetValue(originalMessage.Id, out var starboardMessage))
            {
                try
                {
                    var message = await channel.GetMessageAsync(starboardMessage);
                    await message.ModifyAsync(embed);
                }
                catch (Exception exception)
                {
                    sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not update starboard message");
                }
                return;
            }

            if (reacNumber < Program.Config.HeartboardRequirement) return;

            var hbMessage = await channel.SendMessageAsync(embed);

            StorageContext.Model.HeartboardMessages.Add(originalMessage.Id, hbMessage.Id);
        }

        public static async Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            if (!StorageContext.Model.HeartBoardEnabled || StorageContext.Model.HeartBoardChannel == 0) return;

            var emojiList = Program.Config.RawHeartBoardEmojis.Select(configRawHeartBoardEmoji => ulong.TryParse(configRawHeartBoardEmoji, out var emojiId)
                    ? DiscordEmoji.FromGuildEmote(sender, emojiId)
                    : DiscordEmoji.FromName(sender, configRawHeartBoardEmoji))
                .ToList();

            DiscordMessage originalMessage;
            try
            {
                originalMessage = await e.Channel.GetMessageAsync(e.Message.Id);
            }
            catch (Exception exception)
            {
                sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not get the original message");
                return;
            }

            var heartReactions = originalMessage.Reactions.Where(r => emojiList.Contains(r.Emoji)).ToList();

            var reacNumber = heartReactions.Sum(discordReaction => discordReaction.Count);

            DiscordChannel channel;
            try
            {
                channel = e.Guild.GetChannel(StorageContext.Model.HeartBoardChannel);
            }
            catch (Exception exception)
            {
                sender.Logger.LogWarning(new EventId(1, "Warning"), exception, "Could not get starboard channel");
                return;
            }

            var embed = new DiscordEmbedBuilder
                {
                    Description = originalMessage.Content,
                    Color = new DiscordColor(reacNumber switch
                    {
                        >=25 => "#226383", >=10 => "#3c7f9e", >5 => "#5ca0bf", _ => "#8fc9e6",
                    })
                }
                .WithAuthor($"{originalMessage.Author.Username}", null, originalMessage.Author.AvatarUrl)
                .AddField("Original", $"[Click me!]({originalMessage.JumpLink})", true)
                .AddField("Stars",
                    $"{reacNumber switch {>=25 => ":sparkles:", >=10 => ":dizzy:", >5 => ":star2:", _ => ":star:"}} {reacNumber}", true)
                .WithFooter($"{originalMessage.CreationTimestamp.Date.ToLongDateString()}")
                .Build();

            StorageContext.Model.HeartboardMessages ??= new Dictionary<ulong, ulong>();

            if (StorageContext.Model.HeartboardMessages.TryGetValue(originalMessage.Id, out var starboardMessage))
            {
                try
                {
                    var message = await channel.GetMessageAsync(starboardMessage);
                    await message.ModifyAsync(embed);
                }
                catch (Exception exception)
                {
                    sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not update starboard message");
                }
            }
        }
    }
}
