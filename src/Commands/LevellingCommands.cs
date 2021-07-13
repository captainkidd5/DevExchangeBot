using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
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
            var talked = StorageContext.Model.Users.TryGetValue(mbr?.Id ?? ctx.Member.Id, out var user);

            if (user == null || user.Exp == 0 || !talked)
            {
                await ctx.RespondAsync(":no_mouth: This user didn't talk yet");
                return;
            }

            var orderedList = StorageContext.Model.Users.Values
                .OrderByDescending(u => u.Level)
                .ThenByDescending(u => u.Exp).ToList();

            var rank = orderedList.IndexOf(user) + 1;

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithTitle($"{mbr?.Username ?? ctx.Member.Username}#{mbr?.Discriminator ?? ctx.Member.Discriminator}'s {(rank == 1 ? Program.Config.Emoji.GoldMedal : null)} ranking stats:")
                .WithDescription($"Level: **{user.Level}**\nEXP: **{user.Exp}**/{user.ExpToNextLevel}\nRank: **{rank}**/{orderedList.Count}")
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

            if (StorageContext.Model.Users.Count > 0)
            {
                var orderedList = StorageContext.Model.Users.Values
                    .OrderByDescending(u => u.Level)
                    .ThenByDescending(u => u.Exp).ToList();

                var builder = new StringBuilder();
                var index = 1;

                foreach (var user in orderedList)
                {
                    var member = await ctx.Guild.GetMemberAsync(user.Id);

                    builder.AppendLine($"{index}. {member.Mention} Level: {user.Level} | EXP: {user.Exp}/{user.ExpToNextLevel} " +
                        $"{(index switch { 1 => Program.Config.Emoji.GoldMedal, 2 => Program.Config.Emoji.SilverMedal, 3 => Program.Config.Emoji.BronzeMedal, _ => null })}");

                    ++index;
                }

                var interactivity = ctx.Client.GetInteractivity();
                await message.DeleteAsync();

                var pages = interactivity.GeneratePagesInEmbed(builder.ToString(), SplitType.Line, new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(34, 99, 131))
                    .WithTimestamp(DateTime.UtcNow));

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("There's nothing to see here!");

                await message.DeleteAsync();
                await ctx.Channel.SendMessageAsync(embed.Build());
            }
        }

        [Command("setlevel"), RequireUserPermissions(Permissions.Administrator)]
        public async Task SetLevel(CommandContext ctx, DiscordMember member, int level)
        {
            if (member.IsBot)
            {
                await ctx.RespondAsync($"{Program.Config.Emoji.Failure} Cannot set level of a bot!");
                return;
            }

            if (!StorageContext.Model.Users.TryGetValue(member.Id, out var user))
            {
                user = new UserModel(member.Id);
                StorageContext.Model.AddUser(user);
            }

            user.Level = level;

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithDescription($"{Program.Config.Emoji.Success} Level of {member.Mention} correctly set to {level}!")
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

            StorageContext.Model.ExpMultiplier = multiplier;

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithDescription($"{Program.Config.Emoji.Success} EXP multiplier correctly set to {multiplier}!")
                .WithColor(new DiscordColor(34, 99, 131)));
        }
    }
}
