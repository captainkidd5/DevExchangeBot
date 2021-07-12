using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using Newtonsoft.Json;
using DevExchangeBot.Configuration;
using DevExchangeBot.Storage;

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
            });

            StorageContext.InitializeStorage();

            await _client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
