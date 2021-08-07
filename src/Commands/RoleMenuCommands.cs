using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExchangeBot.Commands.Slash_Commands_Utilities;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    [SlashCommandGroup("rolemenu", "Role-menu related commands")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RoleMenuCommands : SlashCommandModule
    {
        [SlashCommand("create", "Creates an entry for a menu")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task Create(InteractionContext ctx,
            [Option("Name", "Name of the menu to create")]
            string menuName,
            [ChoiceProvider(typeof(SelectionTypeChoiceProvider))] [Option("Selection", "Selection type")]
            string stringAllowMultipleSelection = "True")
        {
            // Check if the menu exists
            if (StorageContext.Model.RoleMenus.Any(m => m.Name == menuName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu already exists!")
                        .AsEphemeral(true));
                return;
            }

            // Check if we're not over the limit
            if (StorageContext.Model.RoleMenus.Count == 25)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            $"{Program.Config.Emoji.Failure} Menu limit reached! You cannot have more than 25 menus.")
                        .AsEphemeral(true));
                return;
            }

            // Parse our string from the choice provider to a bool
            var allowMultipleSelection = bool.Parse(stringAllowMultipleSelection);

            // Created a response with deferred update
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AsEphemeral(true));

            // Add the new entry to the storage
            StorageContext.Model.RoleMenus.Add(new RoleMenuModel
            {
                Options = new List<RoleOption>(),
                AllowMultipleSelection = allowMultipleSelection,
                Name = menuName
            });

            // Refresh the commands for our new menu to appear in the choice provider
            // This can take a considerable amount of time, this is why we made a deferred response
            await ctx.SlashCommandsExtension.RefreshCommands();

            // Respond with a success message
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(
                    $"{Program.Config.Emoji.Success} Entry added, start adding roles by using the `addrole` command!"));
        }

        [SlashCommand("suppress", "Completely deletes a menu from the module.")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task Suppress(InteractionContext ctx,
            [ChoiceProvider(typeof(MenuNamesChoiceProvider))] [Option("Menu", "Menu to delete")]
            string menuName)
        {
            // Check if the menu exists
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu does not exists!")
                        .AsEphemeral(true));
                return;
            }

            // Make a fancy message with buttons and let the event handle the deletion
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddComponents(
                        new DiscordButtonComponent(ButtonStyle.Danger, $"deleteMenu_{menuName}", "Confirm"),
                        new DiscordButtonComponent(ButtonStyle.Secondary, "cancelMenuSuppression", "Cancel"))
                    .WithContent(
                        $"Click the button below to confirm menu named `{menuName}` deletion. {Program.Config.Emoji.Warning} This action is irreversible!")
                    .AsEphemeral(true));
        }

        [SlashCommand("changeselectiontype", "Change the selection type of the menu")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task ChangeSelectionType(InteractionContext ctx,
            [ChoiceProvider(typeof(MenuNamesChoiceProvider))] [Option("Menu", "Menu to delete")]
            string menuName,
            [ChoiceProvider(typeof(SelectionTypeChoiceProvider))] [Option("Selection", "Selection type")]
            string stringMultipleSelection)
        {
            // Check if the menu exists
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu does not exist!")
                        .AsEphemeral(true));
                return;
            }

            // Parse our string from the choice provider to a bool
            var multipleSelection = bool.Parse(stringMultipleSelection);

            // Get the menu from the storage and set the new value
            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            menu.AllowMultipleSelection = multipleSelection;

            // Send a response
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{Program.Config.Emoji.Success} Selection type successfully changed!")
                    .AsEphemeral(true));
        }

        [SlashCommand("spawn", "Spawns a menu")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task Spawn(InteractionContext ctx,
            [ChoiceProvider(typeof(MenuNamesChoiceProvider))] [Option("Menu", "Menu to delete")]
            string menuName,
            [Option("InEmbed", "Whether to put the message in a embed")]
            bool inEmbed = false,
            [Option("Message", "Customize the message above the menu")]
            string message = "Select your roles below")
        {
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu does not exist!")
                        .AsEphemeral(true));
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            if (!menu.Options.Any())
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu is empty!")
                        .AsEphemeral(true));
                return;
            }

            var dropdownOptions = new List<DiscordSelectComponentOption>();

            foreach (var option in menu.Options)
            {
                DiscordEmoji emoji = null;
                try
                {
                    emoji = ulong.TryParse(option.Emoji, out var emojiId)
                        ? DiscordEmoji.FromGuildEmote(ctx.Client, emojiId)
                        : DiscordEmoji.FromName(ctx.Client, option.Emoji);
                }
                catch (Exception e)
                {
                    ctx.Client.Logger.LogWarning(e, "Could not get emoji of ID '{EmojiID}'", option.Emoji);
                }


                var role = ctx.Guild.GetRole(option.RoleId);

                dropdownOptions.Add(new DiscordSelectComponentOption(role.Name, role.Id.ToString(), option.Description,
                    false, emoji != null ? new DiscordComponentEmoji(emoji) : null));
            }

            var roleMenu = new DiscordSelectComponent($"roleMenu_{menu.Name}", "Select your role",
                dropdownOptions, false, 0, menu.AllowMultipleSelection ? dropdownOptions.Count : 1);

            var builder = new DiscordMessageBuilder()
                .AddComponents(roleMenu);

            if (inEmbed)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(Program.Config.Color))
                    .WithDescription(message);
                builder.AddEmbed(embed);
            }
            else
            {
                builder.WithContent(message);
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"{Program.Config.Emoji.Success} Success!")
                    .AsEphemeral(true));
            await builder.SendAsync(ctx.Channel);
        }

        [SlashCommand("addrole", "Adds a role to a menu")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task AddRole(InteractionContext ctx,
            [ChoiceProvider(typeof(MenuNamesChoiceProvider))] [Option("Menu", "Menu to delete")]
            string menuName,
            [Option("Role", "Role to add")] DiscordRole role,
            [Option("Emoji", "Emoji to use for the option")]
            string rawEmoji,
            [Option("Description", "Description of the option")]
            string description)
        {
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu does not exist!")
                        .AsEphemeral(true));
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            if (menu.Options
                .Any(o => o.RoleId == role.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This role is already added in the menu!")
                        .AsEphemeral(true));
                return;
            }

            if (menu.Options.Count == 25)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            $"{Program.Config.Emoji.Failure} Options limit reached! You cannot have more than 25 options in your menu.")
                        .AsEphemeral(true));
                return;
            }

            DiscordEmoji emoji;

            var rawEmojiRegex = Regex.Match(rawEmoji.Trim(), "^<a?:[0-9a-zA-Z_]+:([0-9]+)>$", RegexOptions.IgnoreCase);

            if (rawEmojiRegex.Success)
            {
                if (!ulong.TryParse(rawEmojiRegex.Groups[1].Value, out var emojiId) ||
                    !DiscordEmoji.TryFromGuildEmote(ctx.Client, emojiId, out emoji))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                $"{Program.Config.Emoji.Failure} You did not provide a correct emoji! Be sure the bot has access to the emoji.")
                            .AsEphemeral(true));
                    return;
                }
            }
            else
            {
                if (!DiscordEmoji.TryFromUnicode(ctx.Client, rawEmoji.Trim(), out emoji))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                $"{Program.Config.Emoji.Failure} You did not provide a correct emoji! Be sure the bot has access to the emoji.")
                            .AsEphemeral(true));
                    return;
                }
            }

            menu.Options.Add(new RoleOption
            {
                RoleId = role.Id,
                Description = description,
                Emoji = emoji.Id == 0 ? emoji.GetDiscordName() : emoji.Id.ToString()
            });

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{Program.Config.Emoji.Success} Role successfully added to the menu!")
                    .AsEphemeral(true));
        }

        [SlashCommand("delrole", "Deletes a role from a menu")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task DelRole(InteractionContext ctx,
            [Option("Menu", "Menu to delete the role from")] [ChoiceProvider(typeof(MenuNamesChoiceProvider))]
            string menuName,
            [Option("Role", "Role to remove")] DiscordRole role)
        {
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu does not exist!")
                        .AsEphemeral(true));
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            if (menu.Options.All(o => o.RoleId != role.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} The role was not found in the menu!")
                        .AsEphemeral(true));
                return;
            }

            menu.Options.RemoveAt(menu.Options.IndexOf(menu.Options.First(o => o.RoleId == role.Id)));

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{Program.Config.Emoji.Success} Role successfully removed!")
                    .AsEphemeral(true));
        }

        [SlashCommand("getmenuinfo", "Gets all available information about a menu.")]
        public async Task GetMenuInfo(InteractionContext ctx,
            [Option("Menu", "Menu to delete the role from")] [ChoiceProvider(typeof(MenuNamesChoiceProvider))]
            string menuName)
        {
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} This menu does not exist!")
                        .AsEphemeral(true));
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Menu information")
                .WithColor(new DiscordColor(Program.Config.Color))
                .WithDescription(
                    $"Name: `{menu.Name}`\nSelection type: `{(menu.AllowMultipleSelection ? "Multiple" : "Single")}`\n\nSee below for options");

            foreach (var option in menu.Options)
            {
                var role = ctx.Guild.GetRole(option.RoleId);
                var emoji = ulong.TryParse(option.Emoji, out var emojiId)
                    ? DiscordEmoji.FromGuildEmote(ctx.Client, emojiId)
                    : DiscordEmoji.FromName(ctx.Client, option.Emoji);
                embed.AddField(role.Name, $"{emoji} {option.Description}");
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AsEphemeral(true));
        }
    }
}
