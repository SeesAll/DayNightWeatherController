
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("DayNightWeatherController", "SeesAll", "0.5.2")]
    [Description("Controls day/night cycles and weather with smart runtime strategies.")]
    public class DayNightWeatherController : RustPlugin
    {
        private PluginConfig config;
        private RuntimeStrategy runtimeStrategy = RuntimeStrategy.LocalVisualOverride;
        private Timer worldControlTimer;
        private Timer weatherVerifyTimer;

        private const string DefaultAdminOverridePermission = "daynightweathercontroller.adminoverride";
        private const float WorldControlIntervalSeconds = 10f;
        private const float WeatherVerifyIntervalSeconds = 180f;

        private enum RuntimeStrategy
        {
            LocalVisualOverride,
            TrueWorldControl,
            NoControl
        }

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "EnvironmentControl")]
            public EnvironmentControlSettings EnvironmentControl = new EnvironmentControlSettings();

            [JsonProperty(PropertyName = "TimeControl")]
            public TimeControlSettings TimeControl = new TimeControlSettings();

            [JsonProperty(PropertyName = "WeatherControl")]
            public WeatherControlSettings WeatherControl = new WeatherControlSettings();

            [JsonProperty(PropertyName = "Permissions")]
            public PermissionSettings Permissions = new PermissionSettings();
        }

        private class EnvironmentControlSettings
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled = true;
        }

        private class TimeControlSettings
        {
            [JsonProperty(PropertyName = "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)")]
            public int Mode = 1;

            [JsonProperty(PropertyName = "DayLengthMinutes (Custom Only)")]
            public int DayLengthMinutes = 55;

            [JsonProperty(PropertyName = "NightLengthMinutes (Custom Only)")]
            public int NightLengthMinutes = 5;

            [JsonProperty(PropertyName = "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)")]
            public float LockedHour = 12f;
        }

        private class WeatherControlSettings
        {
            [JsonProperty(PropertyName = "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)")]
            public string Mode = "Clear";
        }

        private class PermissionSettings
        {
            [JsonProperty(PropertyName = "UseAdminOverridePermission")]
            public string UseAdminOverridePermission = DefaultAdminOverridePermission;
        }

        protected override void LoadDefaultConfig()
        {
            config = CreateDefaultConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                var raw = Config.ReadObject<Dictionary<string, object>>();
                config = MigrateConfig(raw);
                SaveConfig();
            }
            catch (Exception ex)
            {
                PrintWarning($"Failed to load config, regenerating default config. Reason: {ex.Message}");
                config = CreateDefaultConfig();
                SaveConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        private PluginConfig CreateDefaultConfig()
        {
            return new PluginConfig();
        }

        private PluginConfig MigrateConfig(Dictionary<string, object> raw)
        {
            var migrated = CreateDefaultConfig();

            if (raw == null)
                return migrated;

            var environment = GetMap(raw, "EnvironmentControl");
            if (environment != null)
                migrated.EnvironmentControl.Enabled = GetBool(environment, "Enabled", migrated.EnvironmentControl.Enabled);

            var time = GetMap(raw, "TimeControl");
            if (time != null)
            {
                migrated.TimeControl.Mode =
                    GetInt(time, "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)",
                    GetInt(time, "Mode", migrated.TimeControl.Mode));

                migrated.TimeControl.DayLengthMinutes =
                    GetInt(time, "DayLengthMinutes (Custom Only)",
                    GetInt(time, "DayLengthMinutes", migrated.TimeControl.DayLengthMinutes));

                migrated.TimeControl.NightLengthMinutes =
                    GetInt(time, "NightLengthMinutes (Custom Only)",
                    GetInt(time, "NightLengthMinutes", migrated.TimeControl.NightLengthMinutes));

                migrated.TimeControl.LockedHour =
                    GetFloat(time, "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)",
                    GetFloat(time, "LockedHour", migrated.TimeControl.LockedHour));
            }

            var weather = GetMap(raw, "WeatherControl");
            if (weather != null)
            {
                migrated.WeatherControl.Mode =
                    GetString(weather, "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)",
                    GetString(weather, "Mode (Clear, Rain, Storm, Fog, Overcast, Vanilla)",
                    GetString(weather, "Mode", migrated.WeatherControl.Mode)));
            }

            var permissionsMap = GetMap(raw, "Permissions");
            if (permissionsMap != null)
            {
                migrated.Permissions.UseAdminOverridePermission =
                    GetString(permissionsMap, "UseAdminOverridePermission", migrated.Permissions.UseAdminOverridePermission);
            }

            NormalizeConfig(migrated);
            return migrated;
        }

        private void NormalizeConfig(PluginConfig cfg)
        {
            if (cfg.TimeControl.Mode < 1 || cfg.TimeControl.Mode > 4)
                cfg.TimeControl.Mode = 1;

            if (cfg.TimeControl.DayLengthMinutes < 1)
                cfg.TimeControl.DayLengthMinutes = 1;

            if (cfg.TimeControl.NightLengthMinutes < 1)
                cfg.TimeControl.NightLengthMinutes = 1;

            if (cfg.TimeControl.LockedHour < 0f)
                cfg.TimeControl.LockedHour = 0f;

            if (cfg.TimeControl.LockedHour > 24f)
                cfg.TimeControl.LockedHour = 24f;

            cfg.WeatherControl.Mode = NormalizeWeatherMode(cfg.WeatherControl.Mode);

            if (string.IsNullOrWhiteSpace(cfg.Permissions.UseAdminOverridePermission))
                cfg.Permissions.UseAdminOverridePermission = DefaultAdminOverridePermission;
        }

        private string NormalizeWeatherMode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Clear";

            string mode = input.Trim();

            if (mode.Equals("clear", StringComparison.OrdinalIgnoreCase)) return "Clear";
            if (mode.Equals("overcast", StringComparison.OrdinalIgnoreCase)) return "Overcast";
            if (mode.Equals("rain", StringComparison.OrdinalIgnoreCase)) return "Rain";
            if (mode.Equals("storm", StringComparison.OrdinalIgnoreCase)) return "Storm";
            if (mode.Equals("fog", StringComparison.OrdinalIgnoreCase)) return "Fog";
            if (mode.Equals("vanilla", StringComparison.OrdinalIgnoreCase)) return "Vanilla";

            PrintWarning($"Unknown weather mode '{input}', defaulting to Clear.");
            return "Clear";
        }

        private Dictionary<string, object> GetMap(Dictionary<string, object> root, string key)
        {
            object value;
            if (!root.TryGetValue(key, out value) || value == null)
                return null;

            var dict = value as Dictionary<string, object>;
            if (dict != null)
                return dict;

            var jObject = value as Newtonsoft.Json.Linq.JObject;
            return jObject != null ? jObject.ToObject<Dictionary<string, object>>() : null;
        }

        private bool GetBool(Dictionary<string, object> map, string key, bool fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
                return fallback;

            try { return Convert.ToBoolean(value); }
            catch { return fallback; }
        }

        private int GetInt(Dictionary<string, object> map, string key, int fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
                return fallback;

            try { return Convert.ToInt32(value); }
            catch { return fallback; }
        }

        private float GetFloat(Dictionary<string, object> map, string key, float fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
                return fallback;

            try { return Convert.ToSingle(value); }
            catch { return fallback; }
        }

        private string GetString(Dictionary<string, object> map, string key, string fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
                return fallback;

            return Convert.ToString(value);
        }

        private string GetTimeModeName(int mode)
        {
            switch (mode)
            {
                case 1: return "DAY";
                case 2: return "NIGHT";
                case 3: return "CUSTOM";
                case 4: return "VANILLA";
                default: return $"UNKNOWN({mode})";
            }
        }

        private bool IsForcedWeather()
        {
            return !string.Equals(config.WeatherControl.Mode, "Vanilla", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSafeVisualWeather()
        {
            return string.Equals(config.WeatherControl.Mode, "Clear", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(config.WeatherControl.Mode, "Overcast", StringComparison.OrdinalIgnoreCase);
        }

        private float GetTargetLockedHour()
        {
            return config.TimeControl.Mode == 2 ? 0f : config.TimeControl.LockedHour;
        }

        private bool HasValidSky()
        {
            return TOD_Sky.Instance != null && TOD_Sky.Instance.Components != null && TOD_Sky.Instance.Components.Time != null;
        }

        private void Init()
        {
            config = config ?? CreateDefaultConfig();
            NormalizeConfig(config);
            RegisterConfiguredPermission();
        }

        private void OnServerInitialized()
        {
            ResolveRuntimeStrategy();

            if (runtimeStrategy == RuntimeStrategy.LocalVisualOverride)
                ApplyLocalOverrideToAllActivePlayers();

            if (runtimeStrategy == RuntimeStrategy.TrueWorldControl)
            {
                ApplyWorldControlState();
                StartWorldControlTimer();
            }

            if (IsForcedWeather())
            {
                ApplyConfiguredWeather(true);
                StartWeatherVerifyTimer();
            }

            LogRuntimeSummary();
        }

        private void Unload()
        {
            worldControlTimer?.Destroy();
            weatherVerifyTimer?.Destroy();

            if (runtimeStrategy == RuntimeStrategy.LocalVisualOverride)
                ResetAllLocalOverrides();

            RestoreVanillaTimeProgression();

            if (IsForcedWeather())
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.reset");
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (runtimeStrategy != RuntimeStrategy.LocalVisualOverride || player == null)
                return;

            timer.Once(1f, () =>
            {
                if (player != null && player.IsConnected)
                    ApplyConfiguredLocalTime(player);
            });
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (runtimeStrategy != RuntimeStrategy.LocalVisualOverride || player == null)
                return;

            timer.Once(1f, () =>
            {
                if (player != null && player.IsConnected)
                    ApplyConfiguredLocalTime(player);
            });
        }

        private void ResolveRuntimeStrategy()
        {
            worldControlTimer?.Destroy();

            if (!config.EnvironmentControl.Enabled)
            {
                runtimeStrategy = RuntimeStrategy.NoControl;
                return;
            }

            if ((config.TimeControl.Mode == 1 || config.TimeControl.Mode == 2) && IsSafeVisualWeather())
            {
                runtimeStrategy = RuntimeStrategy.LocalVisualOverride;
                return;
            }

            if (config.TimeControl.Mode == 4 && string.Equals(config.WeatherControl.Mode, "Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                runtimeStrategy = RuntimeStrategy.NoControl;
                return;
            }

            runtimeStrategy = RuntimeStrategy.TrueWorldControl;
        }

        private void LogRuntimeSummary()
        {
            Puts("[DayNightWeatherController]");
            Puts($"Strategy: {runtimeStrategy}");
            Puts($"Time Mode: {GetTimeModeName(config.TimeControl.Mode)}");
            Puts($"Weather Mode: {config.WeatherControl.Mode}");
        }

        private void ApplyLocalOverrideToAllActivePlayers()
        {
            foreach (var player in BasePlayer.activePlayerList)
                ApplyConfiguredLocalTime(player);
        }

        private void ResetAllLocalOverrides()
        {
            foreach (var player in BasePlayer.activePlayerList)
                ResetLocalTime(player);
        }

        private void SendAdminTimeCommand(BasePlayer player, float hour)
        {
            if (player == null || !player.IsConnected)
                return;

            bool wasAdmin = player.IsAdmin;

            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }

            player.SendConsoleCommand("admintime", hour);

            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }
        }

        private void ApplyConfiguredLocalTime(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
                return;

            SendAdminTimeCommand(player, GetTargetLockedHour());
        }

        private void ResetLocalTime(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
                return;

            SendAdminTimeCommand(player, -1f);
        }

        private void StartWorldControlTimer()
        {
            worldControlTimer?.Destroy();
            worldControlTimer = timer.Every(WorldControlIntervalSeconds, ApplyWorldControlState);
        }

        private void ApplyWorldControlState()
        {
            if (runtimeStrategy != RuntimeStrategy.TrueWorldControl || !HasValidSky())
                return;

            var timeComponent = TOD_Sky.Instance.Components.Time;
            timeComponent.UseTimeCurve = false;

            switch (config.TimeControl.Mode)
            {
                case 1:
                    timeComponent.ProgressTime = false;
                    TOD_Sky.Instance.Cycle.Hour = config.TimeControl.LockedHour;
                    ConVar.Env.time = config.TimeControl.LockedHour;
                    break;

                case 2:
                    timeComponent.ProgressTime = false;
                    TOD_Sky.Instance.Cycle.Hour = 0f;
                    ConVar.Env.time = 0f;
                    break;

                case 3:
                    ApplyCustomCycleState(timeComponent);
                    break;

                case 4:
                    timeComponent.ProgressTime = true;
                    break;
            }
        }

        private void ApplyCustomCycleState(TOD_Time timeComponent)
        {
            timeComponent.ProgressTime = true;

            float currentHour = TOD_Sky.Instance.Cycle.Hour;
            float sunrise = TOD_Sky.Instance.SunriseTime;
            float sunset = TOD_Sky.Instance.SunsetTime;
            bool isDay = currentHour > sunrise && currentHour < sunset;

            if (isDay)
            {
                float daylightSpan = Math.Max(0.1f, sunset - sunrise);
                timeComponent.DayLengthInMinutes = config.TimeControl.DayLengthMinutes * (24f / daylightSpan);
            }
            else
            {
                float nightSpan = Math.Max(0.1f, 24f - (sunset - sunrise));
                timeComponent.DayLengthInMinutes = config.TimeControl.NightLengthMinutes * (24f / nightSpan);
            }
        }

        private void RestoreVanillaTimeProgression()
        {
            if (!HasValidSky())
                return;

            var timeComponent = TOD_Sky.Instance.Components.Time;
            timeComponent.ProgressTime = true;
            timeComponent.UseTimeCurve = false;
        }

        private void StartWeatherVerifyTimer()
        {
            weatherVerifyTimer?.Destroy();
            weatherVerifyTimer = timer.Every(WeatherVerifyIntervalSeconds, () =>
            {
                if (IsForcedWeather())
                    ApplyConfiguredWeather(false);
            });
        }

        private void ApplyConfiguredWeather(bool forceVanillaReset)
        {
            string mode = config.WeatherControl.Mode ?? "Clear";

            if (string.Equals(mode, "Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                if (forceVanillaReset)
                    ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.reset");
                return;
            }

            ConsoleSystem.Run(ConsoleSystem.Option.Server, $"weather.load {mode}");
        }

        private void RegisterConfiguredPermission()
        {
            string perm = config != null && !string.IsNullOrWhiteSpace(config.Permissions.UseAdminOverridePermission)
                ? config.Permissions.UseAdminOverridePermission
                : DefaultAdminOverridePermission;

            permission.RegisterPermission(perm, this);
        }

        private bool HasAdminOverrideAccess(BasePlayer player)
        {
            if (player == null)
                return false;

            string perm = config.Permissions.UseAdminOverridePermission;
            if (string.IsNullOrWhiteSpace(perm))
                perm = DefaultAdminOverridePermission;

            return permission.UserHasPermission(player.UserIDString, perm);
        }

        private bool RequireAdminOverrideAccess(BasePlayer player)
        {
            if (HasAdminOverrideAccess(player))
                return true;

            SendReply(player, "You do not have permission to use this command.");
            return false;
        }

        [ChatCommand("day")]
        private void CmdDay(BasePlayer player, string command, string[] args)
        {
            if (!RequireAdminOverrideAccess(player))
                return;

            SendAdminTimeCommand(player, 12f);
        }

        [ChatCommand("night")]
        private void CmdNight(BasePlayer player, string command, string[] args)
        {
            if (!RequireAdminOverrideAccess(player))
                return;

            SendAdminTimeCommand(player, 0f);
        }

        [ChatCommand("time")]
        private void CmdTime(BasePlayer player, string command, string[] args)
        {
            if (!RequireAdminOverrideAccess(player))
                return;

            if (args == null || args.Length < 1)
            {
                SendReply(player, "Usage: /time <0-24>");
                return;
            }

            float hour;
            if (!float.TryParse(args[0], out hour) || hour < 0f || hour > 24f)
            {
                SendReply(player, "Hour must be a number between 0 and 24.");
                return;
            }

            SendAdminTimeCommand(player, hour);
        }

        [ChatCommand("realtime")]
        private void CmdRealtime(BasePlayer player, string command, string[] args)
        {
            if (!RequireAdminOverrideAccess(player))
                return;

            ResetLocalTime(player);
        }

        [ChatCommand("resettime")]
        private void CmdResetTime(BasePlayer player, string command, string[] args)
        {
            if (!RequireAdminOverrideAccess(player))
                return;

            ResetLocalTime(player);
        }

        [ChatCommand("envstatus")]
        private void CmdEnvStatus(BasePlayer player, string command, string[] args)
        {
            if (!RequireAdminOverrideAccess(player))
                return;

            float serverHour = HasValidSky() ? TOD_Sky.Instance.Cycle.Hour : -1f;
            SendReply(player,
                $"RuntimeStrategy: {runtimeStrategy}\n" +
                $"TimeMode: {GetTimeModeName(config.TimeControl.Mode)}\n" +
                $"WeatherMode: {config.WeatherControl.Mode}\n" +
                $"ServerHour: {serverHour:F2}");
        }
    }
}
