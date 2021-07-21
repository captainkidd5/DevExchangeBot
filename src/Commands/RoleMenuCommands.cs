using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    [Group("rolemenu")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RoleMenuCommands : BaseCommandModule
    {
        [Command("create")]
        public async Task Create(CommandContext ctx, string menuName, bool allowMultipleSelection)
        {
            if (StorageContext.Model.RoleMenus.Any(m => m.Name == menuName))
            {
                await ctx.RespondAsync("This menu already exists!");
                return;
            }

            StorageContext.Model.RoleMenus.Add(new RoleMenuModel
            {
                Options = new List<RoleOption>(),
                AllowMultipleSelection = allowMultipleSelection,
                Name = menuName
            });

            await ctx.RespondAsync("Entry added, add roles with `addrole` command");
        }

        [Command("suppress")]
        public async Task Suppress(CommandContext ctx, string menuName)
        {
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.RespondAsync("This menu does not exist!");
                return;
            }

            var message = await ctx.RespondAsync("React below to confirm menu deletion.");
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));

            var interactivity = ctx.Client.GetInteractivity();

            var result = await interactivity.WaitForReactionAsync(
                r => r.Emoji == DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"),
                message, ctx.User);

            if (result.TimedOut)
            {
                await message.DeleteAllReactionsAsync();
                await message.ModifyAsync("Timed out!");
                return;
            }

            StorageContext.Model.RoleMenus.Remove(StorageContext.Model.RoleMenus.First(m => m.Name == menuName));

            await ctx.RespondAsync("Menu removed!");
        }

        [Command("changeselectiontype")]
        public async Task ChangeSelectionType(CommandContext ctx, string menuName, bool multipleSelection)
        {
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.RespondAsync("This menu does not exist!");
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            menu.AllowMultipleSelection = multipleSelection;
        }

        [Command("spawn"), Aliases("s"), Description("Creates a role menu in the current channel"), RequireUserPermissions(Permissions.Administrator)]
        public async Task Spawn(CommandContext ctx, string menuName)
        {
            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.RespondAsync("This menu does not exist!");
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            if (!menu.Options.Any())
            {
                await ctx.RespondAsync("This menu is empty!");
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
                .WithContent("Select your roles below")
                .AddComponents(roleMenu);

            await builder.SendAsync(ctx.Channel);
        }

        [Command("addrole"), Aliases("add"), Description("Adds a role to the menu"), RequireUserPermissions(Permissions.Administrator)]
        public async Task AddRole(CommandContext ctx, string menuName, DiscordRole role, DiscordEmoji emoji, [RemainingText] string description)
        {
            await ctx.TriggerTypingAsync();

            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.RespondAsync("This menu does not exist!");
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            if (menu.Options
                .Any(o => o.RoleId == role.Id))
            {
                await ctx.RespondAsync("This role is already in this menu!");
                return;
            }

            try
            {
                if (emoji.Id != 0)
                {
                    var _ = DiscordEmoji.FromGuildEmote(ctx.Client, emoji.Id);
                }
            }
            catch
            {
                await ctx.RespondAsync(
                    "It looks like the emoji you provided is an external emoji the bot does not have access to, please try again with another emoji.");
                return;
            }

            menu.Options.Add(new RoleOption
            {
                RoleId = role.Id,
                Description = description,
                Emoji = emoji.Id == 0 ? emoji.GetDiscordName() : emoji.Id.ToString()
            });

            await ctx.RespondAsync("Succesfully added the role to the menu!");
        }

        [Command("delrole"), Aliases("del"), Description("Dels a role from the menu"), RequireUserPermissions(Permissions.Administrator)]
        public async Task DelRole(CommandContext ctx, string menuName, DiscordRole role)
        {
            await ctx.TriggerTypingAsync();

            if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
            {
                await ctx.RespondAsync("This menu does not exist!");
                return;
            }

            var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

            if (menu.Options.All(o => o.RoleId != role.Id))
            {
                await ctx.RespondAsync("This role is not in this menu!");
                return;
            }

            menu.Options.RemoveAt(menu.Options.IndexOf(menu.Options.First(o => o.RoleId == role.Id)));

            await ctx.RespondAsync("Succesfully removed the role to the menu!");
        }
    }
}
