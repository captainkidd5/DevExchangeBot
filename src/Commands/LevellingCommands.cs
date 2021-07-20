using System;
using System.Linq;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [SlashCommandGroup("levelling", "Commands related to the levelling module")]
    public class LevellingCommands : SlashCommandModule
    {
        [SlashCommand("rank", "Show the rank for self or a given user.")]
        public async Task Rank(InteractionContext ctx, [Option("Member", "User to show the rank of.")] DiscordUser mbr = null)
        {
            var talked = StorageContext.Model.Users.TryGetValue(mbr?.Id ?? ctx.Member.Id, out var user);

            if (user == null || user.Exp == 0 || !talked)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(":no_mouth: This user didn't talk yet")
                        .AsEphemeral(true));
                return;
            }

            var orderedList = StorageContext.Model.Users.Values
                .OrderByDescending(u => u.Level)
                .ThenByDescending(u => u.Exp).ToList();

            var rank = orderedList.IndexOf(user) + 1;

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{mbr?.Username ?? ctx.Member.Username}#{mbr?.Discriminator ?? ctx.Member.Discriminator}'s {(rank == 1 ? Program.Config.Emoji.GoldMedal : null)} ranking stats:")
                .WithDescription($"Level: **{user.Level}**\nEXP: **{user.Exp}**/{user.ExpToNextLevel}\nRank: **{rank}**/{orderedList.Count}")
                .WithColor(new DiscordColor(Program.Config.Color))
                .WithThumbnail(mbr?.AvatarUrl ?? ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow)
                .Build();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(true));
        }

        [SlashCommand("leaderboard", "Shows the leaderboard for this server.")]
        public async Task Leaderboard(InteractionContext ctx, [Option("Page", "Page of the leaderboard")] long pageLong = 1)
        {
            if (!int.TryParse(pageLong.ToString(), out var page))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Oops, this number is too big!")
                        .AsEphemeral(true));
                return;
            }

            if (StorageContext.Model.Users.Count < 1)
                return;

            var orderedList = StorageContext.Model.Users.Values
                .OrderByDescending(u => u.Level)
                .ThenByDescending(u => u.Exp).ToList();

            var length = orderedList.Count;

            var pageNumber = length;

            while (pageNumber % 10 != 0)
                pageNumber++;

            pageNumber /= 10;

            if (page > pageNumber)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This page does not exist!")
                        .AsEphemeral(true));
                return;
            }

            orderedList.RemoveRange(0, (page - 1) * 10);

            var builder = new DiscordEmbedBuilder();
            var index = 1;

            foreach (var user in orderedList.Take(10))
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);

                builder.AddField($"{index}. {member.Username}#{member.Discriminator}", $"Level: {user.Level} | EXP: {user.Exp}/{user.ExpToNextLevel} " +
                    $"{(index switch { 1 => Program.Config.Emoji.GoldMedal, 2 => Program.Config.Emoji.SilverMedal, 3 => Program.Config.Emoji.BronzeMedal, _ => null })}");

                ++index;
            }

            builder.WithFooter($"{page}/{pageNumber}");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(builder)
                    .AsEphemeral(true));
        }

        [SlashCommand("setlevel", "Sets the level of a given user. Requires admin permissions."), SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task SetLevel(InteractionContext ctx, [Option("Member", "Member to set the level of")] DiscordUser member, [Option("Level", "Level to set.")] long levelLong)
        {
            if (!int.TryParse(levelLong.ToString(), out var level))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Oops, this number is too big!")
                        .AsEphemeral(true));
                return;
            }

            if (member.IsBot)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Cannot set the level of a bot!")
                        .AsEphemeral(true));
                return;
            }

            if (!StorageContext.Model.Users.TryGetValue(member.Id, out var user))
            {
                user = new UserModel(member.Id);
                StorageContext.Model.AddUser(user);
            }

            user.Level = level;

            var builder = new DiscordEmbedBuilder()
                .WithDescription($"{Program.Config.Emoji.Success} Level of {member.Mention} correctly set to {level}!")
                .WithColor(new DiscordColor(Program.Config.Color));

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(builder)
                    .AsEphemeral(true));
        }

        [SlashCommand("setmultiplier", "Sets the global EXP multiplier. Requires admin permissions."), SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task SetXpMultiplier(InteractionContext ctx, [Option("Multiplier", "Global multiplier to apply.")] string multiplierString)
        {
            if (!float.TryParse(multiplierString, out var multiplier))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Oops, this number is too big!")
                        .AsEphemeral(true));
                return;
            }

            if (multiplier <= 0)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Cannot set the level of a bot!")
                        .AsEphemeral(true));
                return;
            }

            StorageContext.Model.ExpMultiplier = multiplier;

            var builder = new DiscordEmbedBuilder()
                .WithDescription($"{Program.Config.Emoji.Success} EXP multiplier correctly set to {multiplier}!")
                .WithColor(new DiscordColor(Program.Config.Color));

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(builder)
                    .AsEphemeral(true));
        }
    }
}
