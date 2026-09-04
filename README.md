# DayNightWeatherController

A lightweight Rust uMod/Oxide plugin for reliable server-wide day/night scheduling and weather control.

## Features

- Always-day and always-night modes
- Custom day and night durations
- Vanilla time passthrough
- Clear, Overcast, RainHeavy, RainMild, Storm, Fog, or Vanilla weather modes
- Low-frequency correction timers
- Automatic config migration and merge support
- Safe restoration of vanilla time progression and weather on unload
- Permission-protected `/envstatus` diagnostics

## September 2026 compatibility change

Rust's September 2026 update prevents server plugins from invoking the client-only `admintime` command. As a result, version 0.6.0 removes the old `/day`, `/night`, `/time`, `/realtime`, and `/resettime` personal-visibility commands and the `LocalVisualOverride` strategy.

Configured DAY and NIGHT modes now use actual world control and therefore apply to the entire server. CUSTOM mode continues to control the shared cycle, while VANILLA mode leaves Rust's time progression untouched.

### Private daylight for administrators

Administrators can still change only their own view by entering these commands directly in the F1 console:

```text
admintime 12
admintime -1
```

The first command enables personal daylight; the second returns to the server's real time. Other players and the server clock are unaffected.

For convenient spectating, administrators can create local keybinds:

```text
bind f8 "admintime 12"
bind f9 "admintime -1"
writecfg
```

These binds are stored on the administrator's own Rust client and do not grant anything to regular players.

## Time modes

| Mode | Name | Behavior |
|---|---|---|
| `1` | DAY | Locks the shared server time to `LockedHour` |
| `2` | NIGHT | Locks the shared server time to midnight |
| `3` | CUSTOM | Runs a shared cycle using the configured day and night durations |
| `4` | VANILLA | Allows Rust to control time normally |

## Weather modes

| Mode | Behavior |
|---|---|
| `Clear` | Forces clear weather |
| `Overcast` | Forces overcast weather |
| `RainHeavy` | Forces heavy rain |
| `RainMild` | Forces mild rain |
| `Storm` | Forces storm weather |
| `Fog` | Forces fog |
| `Vanilla` | Allows Rust to control weather naturally |

## Commands

| Command | Description |
|---|---|
| `/envstatus` | Shows the active strategy, time mode, weather mode, and current server hour |

## Permission

`/envstatus` requires:

```text
daynightweathercontroller.adminoverride
```

Example grants:

```text
oxide.grant group admin daynightweathercontroller.adminoverride
oxide.grant group moderator daynightweathercontroller.adminoverride
```

The legacy permission name is retained so existing configurations and grants continue to work.

## Configuration

```json
{
  "EnvironmentControl": {
    "Enabled": true
  },
  "TimeControl": {
    "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)": 3,
    "DayLengthMinutes (Custom Only)": 115,
    "NightLengthMinutes (Custom Only)": 5,
    "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)": 12.0
  },
  "WeatherControl": {
    "Mode (Clear, Overcast, RainHeavy, RainMild, Storm, Fog, Vanilla)": "Vanilla"
  },
  "Permissions": {
    "UseAdminOverridePermission": "daynightweathercontroller.adminoverride"
  }
}
```

### Configuration reference

| Setting | Description |
|---|---|
| `EnvironmentControl.Enabled` | Enables or disables time-control logic |
| `TimeControl.Mode` | Selects DAY, NIGHT, CUSTOM, or VANILLA |
| `TimeControl.DayLengthMinutes` | Desired daylight duration in CUSTOM mode |
| `TimeControl.NightLengthMinutes` | Desired nighttime duration in CUSTOM mode |
| `TimeControl.LockedHour` | Shared hour used by DAY mode; NIGHT mode always uses midnight |
| `WeatherControl.Mode` | Selects forced or vanilla weather |
| `Permissions.UseAdminOverridePermission` | Legacy permission key used by `/envstatus` |

## Installation

1. Place `DayNightWeatherController.cs` in the server's `oxide/plugins` directory.
2. Load or reload the plugin.
3. Edit `oxide/config/DayNightWeatherController.json` as required.
4. Reload the plugin after configuration changes.

Existing configuration values are migrated and preserved when the plugin adds defaults.
The legacy `Rain` value is automatically migrated to Rust's current `RainHeavy` preset.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

This project is released under the MIT License. See [LICENSE](LICENSE).
