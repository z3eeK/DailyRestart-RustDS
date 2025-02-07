using System;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins

{
    [Info("DailyRestart", "z3eeK", "1.6.0")]
    [Description("Restart Scheduler for Daily Restarts.")]
    public class DailyRestart : CovalencePlugin
    {
        #region Variables

        private Timer _restartTimer;
        private TimeSpan _restartTime;
        private TimeSpan _countdownTime;
        private string _broadcastMessage;
        private string _restartReason;
        private int _restartDelay = 300;
        private Configuration _config;
        private bool _isRestartScheduled = false;
        
        #endregion
        #region Configuration

        private class Configuration
        {
            [JsonProperty("RestartTime")]
            public string RestartTime { get; set; } = "09:00";
            [JsonProperty("BroadcastMessage")]
            public string BroadcastMessage { get; set; } = "The server will restart in 30 minutes. Your progress will be automatically saved.";
            [JsonProperty("RestartReason")]
            public string RestartReason { get; set; } = "Scheduled daily restart. Server downtime will be no longer than 15 minutes.";
            [JsonProperty("CountdownTime")]
            public int CountdownTime { get; set; } = 1800;
            [JsonProperty("RestartDelay")]
            public int RestartDelay { get; set; } = 300;
            [JsonProperty("Debug")]
            public int DebugEnabled { get;} ;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    SaveConfig();
                }
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);
        protected override void LoadDefaultConfig() => _config = new Configuration();
        
        private void Init()
        {
            LoadConfigValues();
            ScheduleRestart();
        }

        private void LoadConfigValues()
        {
            _broadcastMessage = _config.BroadcastMessage;
            _restartReason = _config.RestartReason;
            _countdownTime = TimeSpan.FromSeconds(_config.CountdownTime);
            _restartDelay = _config.RestartDelay;
            if (_config == null)
            {
                PrintError("Configuration is not loaded.");
                return;
            }

            if (!TimeSpan.TryParse(_config.RestartTime, out _restartTime))
            {
                PrintError($"Invalid restart time format in config: {_config.RestartTime}");
                _restartTime = new TimeSpan(9, 0, 0);
            }
            if (TimeSpan.TryParse(_config.RestartDelay, out _restartDelay))
            {
                PrintError("RestartDelay must be a positive integer.");
                _restartDelay = 300;
            }
            if (_countdownTime.TotalSeconds <= 0)
            {
                PrintError("CountdownTime must be a positive integer.");
                _countdownTime = TimeSpan.FromSeconds(300);
            }
            if (_config.RestartDelay.Debug = 1)
            {
                Puts($"Restart Time: {_restartTime}");
                Puts($"Broadcast Message: {_broadcastMessage}");
                Puts($"Restart Reason: {_restartReason}");
                Puts($"Countdown Time: {_countdownTime.TotalSeconds} seconds");
                Puts($"Restart Delay: {_restartDelay} seconds");
            }
            else
            {
                Puts($"Config Loaded.");
            }
        }
        
        #endregion

        private void ScheduleRestart()
        {
            TimeSpan timeUntilRestart = GetTimeUntilNextRestart();
            float totalSeconds = (float)timeUntilRestart.TotalSeconds;
            Puts($"Restart in: {timeUntilRestart}");
            Puts($"Total seconds for timer: {totalSeconds}");
            _restartTimer?.Destroy();
            _restartTimer = timer.Every(60, () =>
            {
                TimeSpan remainingTime = GetTimeUntilNextRestart();
                float remainingSeconds = (float)remainingTime.TotalSeconds;
                Puts($"Checking timer... Remaining time until next restart: {remainingTime}");
                if (!_isRestartScheduled && remainingSeconds <= _countdownTime.TotalSeconds && remainingSeconds > _countdownTime.TotalSeconds - 60)
                {
                    BroadcastRestartMessage();
                }
                if (!_isRestartScheduled && remainingSeconds <= _restartDelay)
                {
                    _isRestartScheduled = true;
                    RestartServer();
                }
            });
            Puts("Restart scheduled.");
        }

        private TimeSpan GetTimeUntilNextRestart()
        {
            DateTime now = DateTime.UtcNow;
            DateTime todayRestart = DateTime.Today.Add(_restartTime);
            if (now > todayRestart)
            {
                todayRestart = todayRestart.AddDays(1);
            }
            TimeSpan timeUntilNextRestart = todayRestart - now;
            if (_config.RestartDelay.Debug = 1) // To prevent flooding terminal with junk
            {
                Puts($"Calculated time until next restart: {timeUntilNextRestart}");
            }
            return timeUntilNextRestart;
        }

        private void BroadcastRestartMessage()
        {
            if (!string.IsNullOrWhiteSpace(_broadcastMessage))
            {
                covalence.Server.Broadcast(_broadcastMessage);
                Puts($"Broadcasting message: {_broadcastMessage}"); // For terminal print
            }
        }

        private void RestartServer()
        {
            string restartCommand = $"restart {_restartDelay} \"{_restartReason}\"";
            Puts($"Executing restart command: {restartCommand}");
            covalence.Server.Command(restartCommand);
        }

        private void Unload()
        {
            _restartTimer?.Destroy();
        }
    }
}
