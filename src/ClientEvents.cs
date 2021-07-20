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
        public static async Task OnMessageCreatedLevelling(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Message.Content.StartsWith(Program.Config.Prefix))
                return;

            if (!StorageContext.Model.Users.TryGetValue(e.Author.Id, out var user))
            {
                user = new UserModel(e.Author.Id);
                StorageContext.Model.AddUser(user);
            }

            if (user.LastMessageTime + TimeSpan.FromMinutes(1) > e.Message.CreationTimestamp.DateTime) return;

            var content = e.Message.Content;

            Regex.Replace(content, "<:[a-zA-Z]+:[0-9]+>", "0", RegexOptions.IgnoreCase);
            user.Exp += (int)Math.Round(content.Length / 2D * StorageContext.Model.ExpMultiplier, MidpointRounding.ToZero);

            if (user.Exp > user.ExpToNextLevel)
            {
                user.Exp -= user.ExpToNextLevel;
                user.Level += 1;

                await e.Channel.SendMessageAsync($"{Program.Config.Emoji.Confetti} {e.Author.Mention} advanced to level {user.Level}!");
            }

            user.LastMessageTime = e.Message.CreationTimestamp.DateTime;
        }

        public static async Task OnMessageCreatedAutoQuoter(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!StorageContext.Model.AutoQuoterEnabled) return;

            var match = Regex.Match(e.Message.Content, "^https://discord.com/channels/([0-9]+)/([0-9]+)/([0-9]+)$");

            if (!match.Success) return;

            if (!ulong.TryParse(match.Groups[1].Value, out var guildId) ||
                !ulong.TryParse(match.Groups[2].Value, out var channelId) ||
                !ulong.TryParse(match.Groups[3].Value, out var messageId)) return;

            DiscordMessage message;
            try
            {
                message = await (await sender.GetGuildAsync(guildId)).GetChannel(channelId).GetMessageAsync(messageId);
            }
            catch (Exception exception)
            {
                sender.Logger.LogWarning(exception, "Could not get message from the following link '{Link}'", e.Message.Content);
                return;
            }

            await e.Message.DeleteAsync();
            await e.Channel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(Program.Config.Color),
                    Description = message.Content
                }
                .WithAuthor($"{message.Author.Username}#{message.Author.Discriminator}", iconUrl:message.Author.AvatarUrl)
                .AddField("Quoted by", $"{e.Message.Author.Mention} from [#{message.Channel.Name}]({message.JumpLink})"));
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
