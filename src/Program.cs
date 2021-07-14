using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExchangeBot.Commands;
using DSharpPlus;
using Newtonsoft.Json;
using DevExchangeBot.Configuration;
using DevExchangeBot.Storage;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace DevExchangeBot
{
    public static class Program
    {
        public static DiscordClient Client { get; private set; }
        public static ConfigModel Config { get; private set; }

        private static void Main()
        {
            // Ensure the config.json file is copied to the output directory.
            if (!File.Exists("config.json"))
                throw new FileNotFoundException("Configuration file could not be found.", "config.json");

            // Parse the content of the configuration file and fire up the MainAsync method.
            Config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText("config.json"));
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            // Initialize a new client to establish a connection toe the Discord API.
            Client = new DiscordClient(new DiscordConfiguration()
            {
                Token = Config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All // TODO: Enable intents in the bot's application page
            });

            Client.MessageCreated += ClientEvents.OnMessageCreated;
            Client.GuildMemberRemoved += ClientEvents.OnGuildMemberRemoved;

            var commands = Client.UseCommandsNext(new CommandsNextConfiguration()
            {
                EnableDms = false,
                EnableMentionPrefix = false,
                StringPrefixes = new [] { Program.Config.Prefix }
            });

            commands.CommandErrored += OnCommandErrored;
            commands.RegisterCommands<LevellingCommands>();

            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(30),
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteEmojis
            });

            StorageContext.InitializeStorage();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.LogError(new EventId(0, "Error"), e.Exception,
                "User '{Username}#{Discriminator}' ({UserId}) tried to execute '{Command}' "
                + "in #{ChannelName} ({ChannelId}) and failed with {ExceptionType}: {ExceptionMessage}",
                e.Context.User.Username, e.Context.User.Discriminator, e.Context.User.Id, e.Command?.QualifiedName ?? "<unknown command>", e.Context.Channel.Name, e.Context.Channel.Id, e.Exception.GetType(), e.Exception.Message);

            DiscordEmbedBuilder embed = null;

                var ex = e.Exception;
                while (ex is AggregateException)
                    ex = ex.InnerException;

                switch (ex)
                {
                    case CommandNotFoundException:
                        break; // Ignore

                    case ChecksFailedException cfe:
                    {
                        if (cfe.FailedChecks.Any(x => x is RequireUserPermissionsAttribute))
                            embed = new DiscordEmbedBuilder
                            {
                                Title = "Permission denied",
                                Description =
                                    $"{Program.Config.Emoji.AccessDenied} You lack permissions necessary to run this command.",
                                Color = new DiscordColor(0xFF0000)
                            };

                        if (cfe.FailedChecks.Any(x => x is RequireBotPermissionsAttribute))
                            embed = new DiscordEmbedBuilder
                            {
                                Title = "Permission denied",
                                Description = $"{Program.Config.Emoji.AccessDenied} The bot is lacking permissions necessary to run this command.",
                                Color = new DiscordColor(0xFF0000)
                            };

                        if (cfe.FailedChecks.Any(x => x is RequireOwnerAttribute))
                            embed = new DiscordEmbedBuilder
                            {
                                Title = "Permission denied",
                                Description = $"{Program.Config.Emoji.AccessDenied} Only the owner can run this command.",
                                Color = new DiscordColor(0xFF0000)
                            };

                        if (cfe.FailedChecks.Any(x => x is CooldownAttribute))
                            embed = new DiscordEmbedBuilder
                            {
                                Title = "Permission denied",
                                Description = $"{Program.Config.Emoji.Loading} This command is under cooldown, please wait.",
                                Color = new DiscordColor(0xFF0000)
                            };

                        break;
                    }

                    case ArgumentException:
                        await e.Context.RespondAsync($"{Program.Config.Emoji.Failure} Oops, you used a wrong argument. Use ``{e.Context.Prefix}help {e.Command?.QualifiedName}`` to get info.");
                        break;
                    default:
                        embed = new DiscordEmbedBuilder
                        {
                            Title = "A problem occured while executing the command",
                            Description = $"{Program.Config.Emoji.CriticalError} {Formatter.InlineCode(e.Command?.QualifiedName)} threw an exception: `{ex?.GetType()}: {ex?.Message}`",
                            Color = new DiscordColor(0xFF0000)
                        };
                        break;
                }

                if (embed != null)
                    await e.Context.RespondAsync(embed.Build());
        }
    }
}
