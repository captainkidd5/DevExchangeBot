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
// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class LevellingCommands : BaseCommandModule
    {
        [Command("rank"), Aliases("r"), Description("Show the rank for self or a given user.")]
        public async Task Rank(CommandContext ctx, [Description("User to show the rank of.")] DiscordMember mbr = null)
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
                .WithColor(new DiscordColor(Program.Config.Color))
                .WithThumbnail(mbr?.AvatarUrl ?? ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow)
                .Build());
        }

        [Command("leaderboard"), Aliases("lb"), Description("Shows the leaderboard for this server.")]
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
                    .WithColor(new DiscordColor(Program.Config.Color))
                    .WithTimestamp(DateTime.UtcNow));

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription(":no_mouth: There's nothing to see here!")
                    .WithColor(new DiscordColor(Program.Config.Color));

                await message.DeleteAsync();
                await ctx.Channel.SendMessageAsync(embed.Build());
            }
        }

        [Command("setlevel"), RequireUserPermissions(Permissions.Administrator), Description("Sets the level of a given user. Requires admin permissions.")]
        public async Task SetLevel(CommandContext ctx, [Description("Members to set the level of.")] DiscordMember member, [Description("Level to set.")] int level)
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
                .WithColor(new DiscordColor(Program.Config.Color)));
        }

        [Command("setmultiplier"), RequireUserPermissions(Permissions.Administrator), Description("Sets the global EXP multiplier. Requires admin permissions.")]
        public async Task SetXpMultiplier(CommandContext ctx, [Description("Global multiplier to apply.")] float multiplier)
        {
            if (multiplier <= 0)
            {
                await ctx.RespondAsync($"{Program.Config.Emoji.Failure} Multiplier needs to be strictly above 0!");
                return;
            }

            StorageContext.Model.ExpMultiplier = multiplier;

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithDescription($"{Program.Config.Emoji.Success} EXP multiplier correctly set to {multiplier}!")
                .WithColor(new DiscordColor(Program.Config.Color)));
        }
    }
}
