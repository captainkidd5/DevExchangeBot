using System;
using System.Linq;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [SlashCommandGroup("levelling", "Commands related to the levelling module")]
    public class LevellingCommands : ApplicationCommandModule
    {
        [SlashCommand("rank", "Show the rank for self or a given user.")]
        public async Task Rank(InteractionContext ctx,
            [Option("Member", "User to show the rank of.")] DiscordUser mbr = null)
        {
            // Verify we have data related to the user, if not, say the user has not talked yet
            var talked = StorageContext.Model.Users.TryGetValue(mbr?.Id ?? ctx.Member.Id, out var user);
            if (user == null || user.Exp == 0 || !talked)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(":no_mouth: This user didn't talk yet")
                        .AsEphemeral(true));
                return;
            }

            // Get the rank of the user by ordering the list of all users
            var orderedList = StorageContext.Model.Users.Values
                .OrderByDescending(u => u.Level)
                .ThenByDescending(u => u.Exp).ToList();

            var rank = orderedList.IndexOf(user) + 1;

            // Build a response and send it
            var embed = new DiscordEmbedBuilder()
                .WithTitle(
                    $"{mbr?.Username ?? ctx.Member.Username}#{mbr?.Discriminator ?? ctx.Member.Discriminator}'s {(rank == 1 ? Program.Config.Emoji.GoldMedal : null)} ranking stats:")
                .WithDescription(
                    $"Level: **{user.Level}**\nEXP: **{user.Exp}**/{user.ExpToNextLevel}\nRank: **{rank}**/{orderedList.Count}")
                .WithColor(new DiscordColor(Program.Config.Color))
                .WithThumbnail(mbr?.AvatarUrl ?? ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow)
                .Build();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(true));
        }

        [SlashCommand("leaderboard", "Shows the leaderboard for this server.")]
        public async Task Leaderboard(InteractionContext ctx,
            [Option("Page", "Page of the leaderboard")] long pageLong = 1)
        {
            // Parse the long as an int for practical issues
            if (!int.TryParse(pageLong.ToString(), out var page))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Oops, this number is too big!")
                        .AsEphemeral(true));
                return;
            }

            // Return if no users
            if (StorageContext.Model.Users.Count < 1)
                return; // TODO: Make a response for this case

            var orderedList = StorageContext.Model.Users.Values
                .OrderByDescending(u => u.Level)
                .ThenByDescending(u => u.Exp).ToList();

            var length = orderedList.Count;

            var pageNumber = length;

            // Do some maths to get the number of available pages
            while (pageNumber % 10 != 0)
                pageNumber++;

            pageNumber /= 10;

            // Indicates to the user if the asked page does not exists
            if (page > pageNumber)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This page does not exist!")
                        .AsEphemeral(true));
                return;
            }

            // Manipulates the list to be able to take what need from it
            orderedList.RemoveRange(0, (page - 1) * 10);

            var builder = new DiscordEmbedBuilder();
            var index = 1;

            // Cycle trough 10 users and add them to the embed
            foreach (var user in orderedList.Take(10))
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);

                builder.AddField($"{index}. {member.Username}#{member.Discriminator}",
                    $"Level: {user.Level} | EXP: {user.Exp}/{user.ExpToNextLevel} " +
                    $"{index switch { 1 => Program.Config.Emoji.GoldMedal, 2 => Program.Config.Emoji.SilverMedal, 3 => Program.Config.Emoji.BronzeMedal, _ => null }}");

                ++index;
            }

            builder.WithFooter($"{page}/{pageNumber}");

            // Send the embed with the leaderboard inside
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(builder)
                    .AsEphemeral(true));
        }

        [SlashCommand("setlevel", "Sets the level of a given user. Requires admin permissions.")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task SetLevel(InteractionContext ctx,
            [Option("Member", "Member to set the level of")] DiscordUser member,
            [Option("Level", "Level to set.")] long levelLong)
        {
            // Parse the long as an int for practical issues
            if (!int.TryParse(levelLong.ToString(), out var level))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Oops, this number is too big!")
                        .AsEphemeral(true));
                return;
            }

            // Check is the given member is a bot
            if (member.IsBot)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Cannot set the level of a bot!")
                        .AsEphemeral(true));
                return;
            }

            // Check if there is data associated with the given member, if no, create it
            if (!StorageContext.Model.Users.TryGetValue(member.Id, out var user))
            {
                user = new UserModel(member.Id);
                StorageContext.Model.AddUser(user);
            }

            // Set the new level
            user.Level = level;

            var builder = new DiscordEmbedBuilder()
                .WithDescription($"{Program.Config.Emoji.Success} Level of {member.Mention} correctly set to {level}!")
                .WithColor(new DiscordColor(Program.Config.Color));

            // Build a response (above) and send it
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(builder)
                    .AsEphemeral(true));
        }

        [SlashCommand("setmultiplier", "Sets the global EXP multiplier. Requires admin permissions.")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task SetXpMultiplier(InteractionContext ctx,
            [Option("Multiplier", "Global multiplier to apply.")] double multiplier)
        {
            // Check if the multiplier is negative
            if (multiplier <= 0)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Cannot set the level of a bot!")
                        .AsEphemeral(true));
                return;
            }

            // Set the new multiplier
            StorageContext.Model.ExpMultiplier = (float)multiplier;

            var builder = new DiscordEmbedBuilder()
                .WithDescription($"{Program.Config.Emoji.Success} EXP multiplier correctly set to {multiplier}!")
                .WithColor(new DiscordColor(Program.Config.Color));

            // Build a response (above) and send it
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(builder)
                    .AsEphemeral(true));
        }

        [SlashCommand("setlevelupchannel", "Sets the channel where level-up messages should be displayed.")]
        public async Task SetLevelUpChannel(InteractionContext ctx,
            [Option("Channel", "Channel to display the level-up messages in")]
            DiscordChannel channel,
            [Option("Enable", "If false, will send the level-up message in the current channel. Defaults to true.")]
            bool enable = true)
        {
            // Check if the given channel is a text channel
            if (channel.Type != ChannelType.Text)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} You can only use a text channel!")
                        .AsEphemeral(true));
                return;
            }

            // Set the new level-up channel and whether or not it's enabled
            StorageContext.Model.LevelUpChannelId = channel.Id;
            StorageContext.Model.EnableLevelUpChannel = enable;

            // Create and send a response
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{Program.Config.Emoji.Success} Settings successfully updated!")
                    .AsEphemeral(true));
        }
    }
}
