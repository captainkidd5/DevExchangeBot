using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExchangeBot.Configuration;
using DevExchangeBot.Storage;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class LevellingCommands : BaseCommandModule
    {
        [Command("rank"), Aliases("r")]
        public async Task Rank(CommandContext ctx, DiscordMember mbr = null)
        {
            var talked = StorageContext.Model.UserDictionary.TryGetValue(mbr?.Id ?? ctx.Member.Id, out var usrData);

            if (usrData == null || usrData.Xp == 0 || !talked)
            {
                await ctx.RespondAsync(":no_mouth: This user didn't talk yet");
                return;
            }

            var uList = StorageContext.Model.UserDictionary.Values
                .OrderByDescending(u => u.Level).ThenByDescending(u => u.Xp).ToList();

            var rank = uList.IndexOf(usrData) + 1;

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithTitle($"{mbr?.Username ?? ctx.Member.Username}#{mbr?.Discriminator ?? ctx.Member.Discriminator}'s {(rank == 1 ? Emojis.Medal : null)} ranking stats:")
                .WithDescription($"Level: **{usrData.Level}**\nEXP: **{usrData.Xp}**/{usrData.XpToNextLevel}\nRank: **{rank}**/{uList.Count}")
                .WithColor(new DiscordColor(34, 99, 131))
                .WithThumbnail(mbr?.AvatarUrl ?? ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow)
                .Build());
        }

        [Command("leaderboard"), Aliases("lb")]
        public async Task Leaderboard(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var message = await ctx.RespondAsync($"{Emojis.Loading} Bot is thinking...");

            var orderedList = StorageContext.Model.UserDictionary.Values
                .OrderByDescending(u => u.Level).ThenByDescending(u => u.Xp).ToList();

            var sb = new StringBuilder();

            var i = 1;
            foreach (var userData in orderedList)
            {
                var user = await ctx.Guild.GetMemberAsync(userData.Id);

                sb.AppendLine($"{i}. {user.Mention} Level: {userData.Level} | EXP: {userData.Xp}/{userData.XpToNextLevel} {(i == 1 ? Emojis.Medal : null)}");

                i++;
            }

            var interactivity = ctx.Client.GetInteractivity();

            await message.DeleteAsync();

            var pages = interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(34, 99, 131))
                .WithTimestamp(DateTime.UtcNow));
            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
        }
    }
}
