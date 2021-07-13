using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExchangeBot.Models;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

namespace DevExchangeBot.Commands
{
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
                // TODO: Add proper ranking for silver and bronze medals here
                .WithTitle($"{mbr?.Username ?? ctx.Member.Username}#{mbr?.Discriminator ?? ctx.Member.Discriminator}'s {(rank == 1 ? Program.Config.Emoji.GoldMedal : null)} ranking stats:")
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

            var message = await ctx.RespondAsync($"{Program.Config.Emoji.Loading} Bot is thinking...");

            var orderedList = StorageContext.Model.UserDictionary.Values
                .OrderByDescending(u => u.Level).ThenByDescending(u => u.Xp).ToList();

            var builder = new StringBuilder();
            var count = 1;

            foreach (var userData in orderedList)
            {
                var user = await ctx.Guild.GetMemberAsync(userData.Id);

                builder.AppendLine($"{count}. {user.Mention} Level: {userData.Level} | EXP: {userData.Xp}/{userData.XpToNextLevel} " +
                    $"{(count switch { 1 => Program.Config.Emoji.GoldMedal, 2 => Program.Config.Emoji.SilverMedal, 3 => Program.Config.Emoji.BronzeMedal, _ => null })}");

                count++;
            }

            var interactivity = ctx.Client.GetInteractivity();
            await message.DeleteAsync();

            var pages = interactivity.GeneratePagesInEmbed(builder.ToString(), SplitType.Line, new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(34, 99, 131))
                .WithTimestamp(DateTime.UtcNow));

            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
        }

        [Command("setlevel"), RequireUserPermissions(Permissions.Administrator)]
        public async Task SetLevel(CommandContext ctx, DiscordMember member, int level)
        {
            if (member.IsBot)
            {
                await ctx.RespondAsync($"{Program.Config.Emoji.Failure} Cannot set level of a bot!");
                return;
            }

            if (!StorageContext.Model.UserDictionary.TryGetValue(member.Id, out var usrData))
            {
                usrData = new UserData(member.Id);
                StorageContext.Model.UserDictionary.Add(member.Id, usrData);
            }

            usrData.Level = level;

            await ctx.RespondAsync(new DiscordEmbedBuilder
                {
                    Description = $"{Program.Config.Emoji.Success} Level of {member.Mention} correctly set to {level}!"
                }
                .WithColor(new DiscordColor(34, 99, 131)));
        }

        [Command("setmultiplier"), RequireUserPermissions(Permissions.Administrator)]
        public async Task SetXpMultiplier(CommandContext ctx, float multiplier)
        {
            if (multiplier <= 0)
            {
                await ctx.RespondAsync($"{Program.Config.Emoji.Failure} Multiplier needs to be strictly above 0!");
                return;
            }

            StorageContext.Model.XpMultiplier = multiplier;

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                {
                    Description = $"{Program.Config.Emoji.Success} EXP multiplier correctly set to {multiplier}!"
                }
                .WithColor(new DiscordColor(34, 99, 131)));
        }
    }
}
