using System;
using System.IO;
using System.Threading.Tasks;
using DevExchangeBot.Commands;
using DSharpPlus;
using Newtonsoft.Json;
using DevExchangeBot.Configuration;
using DevExchangeBot.Storage;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace DevExchangeBot
{
    public static class Program
    {
        private static DiscordClient _client;
        private static ConfigModel _config;

        private static void Main(string[] args)
        {
            // Ensure the config.json file is copied to the output directory.
            if (!File.Exists("config.json"))
                throw new FileNotFoundException("Configuration file could not be found.", "config.json");

            // Parse the content of the configuration file and fire up the MainAsync method.
            _config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText("config.json"));
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            // Initialize a new client to establish a connection toe the Discord API.
            _client = new DiscordClient(new DiscordConfiguration()
            {
                Token = _config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All // TODO: Enable intents in the bot's application page
            });

            _client.MessageCreated += ClientEvents.OnMessageCreated;

            var commands = _client.UseCommandsNext(new CommandsNextConfiguration()
            {
                EnableDms = false,
                EnableMentionPrefix = false,
                StringPrefixes = new [] {"dx!"} // TODO: Change the prefix if needed
            });

            commands.CommandErrored += OnCommandErrored;

            commands.RegisterCommands<LevellingCommands>();

            _client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(30),
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteEmojis
            });

            Console.WriteLine(_config.Token);

            StorageContext.InitializeStorage();

            await _client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.LogError(new EventId(0, "Error"), e.Exception,
                "User '{Username}#{Discriminator}' ({UserId}) tried to execute '{Command}' "
                + "in #{ChannelName} ({ChannelId}) and failed with {ExceptionType}: {ExceptionMessage}",
                e.Context.User.Username, e.Context.User.Discriminator, e.Context.User.Id, e.Command?.QualifiedName ?? "<unknown command>", e.Context.Channel.Name, e.Context.Channel.Id, e.Exception.GetType(), e.Exception.Message);
            return Task.CompletedTask;
        }
    }
}
