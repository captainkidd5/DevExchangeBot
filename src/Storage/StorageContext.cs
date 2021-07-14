using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using DevExchangeBot.Storage.Models;
using Newtonsoft.Json;

namespace DevExchangeBot.Storage
{
    public static class StorageContext
    {
        private const string FilePath = "storage.json";

        public static StorageModel Model { get; set; }
        private static Timer SaveTimer { get; set; }

        public static void InitializeStorage()
        {
            // Handle writing the data when the program exit and when any error happens
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.Exited += SaveData;

            // Set up a timer to save data every 30 seconds
            SaveTimer = new Timer
            {
                AutoReset = true,
                Enabled = true,
                Interval = 30_000,
            };

            SaveTimer.Elapsed += SaveData;

            if (!File.Exists(FilePath))
            {
                var fs = File.Create(FilePath);
                fs.Dispose();
            }

            // Retrieve the data and load the static object with it
            Model = JsonConvert.DeserializeObject<StorageModel>(File.ReadAllText(FilePath)) ?? new StorageModel
            {
                Users = new Dictionary<ulong, UserModel>(),
                Noodles = new NoodlesModel(),
                ExpMultiplier = 1
            };
        }

        public static void SaveData(object sender, EventArgs eventArgs)
        {
            var json = JsonConvert.SerializeObject(Model, Formatting.Indented);

            if (!File.Exists(FilePath))
            {
                var fs = File.Create(FilePath);
                fs.Dispose();
            }

            File.WriteAllText(FilePath, json);
        }
    }
}
