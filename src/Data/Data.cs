using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using DevExchangeBot.Models;
using Newtonsoft.Json;

namespace DevExchangeBot
{
    public static class Data
    {
        public static StorageModel StorageData { get; set; } // TODO: Change the name of the object if needed

        private static Timer SaveTimer { get; set; }

        private const string FilePath = "Data.json"; // TODO: Change the file name if needed

        public static void InitializeStorage()
        {
            // Handle writing the data when the program exit and when any error happens
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.Exited += SaveData;

            AppDomain.CurrentDomain.UnhandledException += SaveData;

            // Save the data every 10 seconds
            SaveTimer = new Timer
            {
                AutoReset = true,
                Enabled = true,
                Interval = 30_000 // TODO: Change the interval if needed
            };
            SaveTimer.Elapsed += SaveData;

            if (!File.Exists(FilePath))
            {
                File.Create(FilePath);
                File.WriteAllText(FilePath, "{}");
            }

            // Retrieve the data and load the static object with it
            var storageObject = JsonConvert.DeserializeObject<StorageModel>(File.ReadAllText(FilePath));

            StorageData = storageObject;
        }

        /// <summary>
        /// Save the data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private static void SaveData(object sender, EventArgs eventArgs)
        {
            var json = JsonConvert.SerializeObject(StorageData, Formatting.Indented);
            if (!File.Exists(FilePath))
                File.Create(FilePath);
            File.WriteAllText(FilePath, json);
        }
    }
}
