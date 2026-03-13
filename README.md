# DayNightWeatherController

A high-performance Rust uMod/Oxide plugin that intelligently controls day/night behavior and weather while minimizing risk to vanilla event timing.

## Overview

`DayNightWeatherController` was built to solve a common Rust server problem:

- Many PvP servers want **always day** and **clear visibility**
- Many PvE servers want **custom day/night lengths** and **atmospheric weather**
- Many vanilla-style servers want to keep the environment untouched but still give staff **local time override tools**

Instead of using one blunt method for every scenario, this plugin automatically chooses the safest runtime strategy based on your config.

## Key Features

- **Always Day / Always Night**
- **Custom day/night durations**
- **Vanilla passthrough mode**
- **Clear, Overcast, Rain, Storm, Fog, or Vanilla weather**
- **Automatic runtime strategy selection**
- **Low-risk visual override mode for PvP-style setups**
- **Admin local override tools**
- **Automatic config migration / merge support**
- **Unload cleanup for local overrides, weather, and time progression**
- **Lightweight design suitable for high-population servers**

## Runtime Strategy System

This plugin automatically selects one of three internal strategies:

### 1) LocalVisualOverride
Used for the safest PvP-friendly cases:

- `DAY + Clear`
- `DAY + Overcast`
- `NIGHT + Clear`
- `NIGHT + Overcast`

What it does:
- Keeps **real server time progressing**
- Applies a **local visual override** to players
- Helps preserve vanilla event timing
- Avoids unnecessary world-time manipulation

### 2) TrueWorldControl
Used when the server actually needs shared world-state control:

- `CUSTOM` day/night cycles
- `DAY/NIGHT` paired with `Rain`, `Storm`, or `Fog`
- any configuration that requires a real shared atmosphere

What it does:
- Controls the actual environment
- Applies shared weather to all players
- Uses controlled time progression logic

### 3) NoControl
Used when environment control is effectively disabled:

- `VANILLA + Vanilla`

What it does:
- Leaves time and weather alone
- Still allows staff tools if they have permission

## Why this plugin is safer than older time plugins

Older time plugins often relied on:
- hard time jumps
- frozen world time
- aggressive clock rewriting

That can cause problems with vanilla behavior and event timing.

This plugin instead:
- prefers **local visual override** whenever appropriate
- uses **true world control only when needed**
- uses **lightweight timers**
- avoids unnecessary brute-force world manipulation

## Commands

These commands are **permission protected**.

| Command | Description |
|---|---|
| `/day` | Set local time to day |
| `/night` | Set local time to night |
| `/time <0-24>` | Set a custom local hour |
| `/realtime` | Return to the actual server time |
| `/resettime` | Alias for `/realtime` |
| `/envstatus` | Show current runtime strategy, time mode, weather mode, and server hour |

## Permission

```text
daynightweathercontroller.adminoverride
```

Example permission grants:

```text
oxide.grant group admin daynightweathercontroller.adminoverride
oxide.grant group moderator daynightweathercontroller.adminoverride
```

## Configuration

```json
{
  "EnvironmentControl": {
    "Enabled": true
  },
  "TimeControl": {
    "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)": 1,
    "DayLengthMinutes (Custom Only)": 55,
    "NightLengthMinutes (Custom Only)": 5,
    "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)": 12.0
  },
  "WeatherControl": {
    "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)": "Clear"
  },
  "Permissions": {
    "UseAdminOverridePermission": "daynightweathercontroller.adminoverride"
  }
}
```

## Config Options

### EnvironmentControl

| Setting | Description |
|---|---|
| `Enabled` | Enables or disables environment control logic |

### TimeControl

| Setting | Description |
|---|---|
| `Mode` | `1=DAY`, `2=NIGHT`, `3=CUSTOM`, `4=VANILLA` |
| `DayLengthMinutes` | Used only in `CUSTOM` mode |
| `NightLengthMinutes` | Used only in `CUSTOM` mode |
| `LockedHour` | Used only in `DAY` and `NIGHT` style locking |

### WeatherControl

| Mode | Description |
|---|---|
| `Clear` | Constant clear weather |
| `Overcast` | Constant overcast weather |
| `Rain` | Constant rain |
| `Storm` | Constant storm weather |
| `Fog` | Constant fog |
| `Vanilla` | Rust controls weather naturally |

### Permissions

| Setting | Description |
|---|---|
| `UseAdminOverridePermission` | Permission required to use admin override commands |

## Time Mode Reference

Rust uses a 24-hour floating time system.

| Hour | Lighting | Description |
|---|---|---|
| 0.0 | Night | Midnight, darkest part of night |
| 1.0 | Night | Moonlit night |
| 2.0 | Night | Dark night |
| 3.0 | Night | Dark night |
| 4.0 | Night | Pre-dawn |
| 5.0 | Dawn | First dim light |
| 6.0 | Sunrise | Sun begins rising |
| 7.0 | Morning | Daylight |
| 8.0 | Morning | Bright |
| 9.0 | Day | Full daylight |
| 10.0 | Day | Bright daylight |
| 11.0 | Day | Late morning |
| 12.0 | Day | Peak daylight |
| 13.0 | Day | Afternoon |
| 14.0 | Day | Afternoon |
| 15.0 | Day | Afternoon |
| 16.0 | Day | Late afternoon |
| 17.0 | Evening | Pre-sunset |
| 18.0 | Sunset | Sun setting |
| 19.0 | Dusk | Light fading |
| 20.0 | Nightfall | Darkening |
| 21.0 | Night | Moon visible |
| 22.0 | Night | Dark night |
| 23.0 | Night | Late night |

### Recommended LockedHour values

| Scenario | Value |
|---|---|
| Always Day | `12.0` |
| Always Night | `0.0` |
| Sunrise | `6.0` |
| Sunset | `18.0` |
| Dawn atmosphere | `5.0` |
| Dusk atmosphere | `19.0` |

## Example Configurations

### PvP Safe Day Mode
```json
{
  "EnvironmentControl": {
    "Enabled": true
  },
  "TimeControl": {
    "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)": 1,
    "DayLengthMinutes (Custom Only)": 55,
    "NightLengthMinutes (Custom Only)": 5,
    "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)": 12.0
  },
  "WeatherControl": {
    "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)": "Clear"
  },
  "Permissions": {
    "UseAdminOverridePermission": "daynightweathercontroller.adminoverride"
  }
}
```

### PvP Overcast Day Mode
```json
{
  "EnvironmentControl": {
    "Enabled": true
  },
  "TimeControl": {
    "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)": 1,
    "DayLengthMinutes (Custom Only)": 55,
    "NightLengthMinutes (Custom Only)": 5,
    "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)": 12.0
  },
  "WeatherControl": {
    "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)": "Overcast"
  },
  "Permissions": {
    "UseAdminOverridePermission": "daynightweathercontroller.adminoverride"
  }
}
```

### Custom Cycle with Vanilla Weather
```json
{
  "EnvironmentControl": {
    "Enabled": true
  },
  "TimeControl": {
    "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)": 3,
    "DayLengthMinutes (Custom Only)": 55,
    "NightLengthMinutes (Custom Only)": 5,
    "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)": 12.0
  },
  "WeatherControl": {
    "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)": "Vanilla"
  },
  "Permissions": {
    "UseAdminOverridePermission": "daynightweathercontroller.adminoverride"
  }
}
```

### Storm Day Server
```json
{
  "EnvironmentControl": {
    "Enabled": true
  },
  "TimeControl": {
    "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)": 1,
    "DayLengthMinutes (Custom Only)": 55,
    "NightLengthMinutes (Custom Only)": 5,
    "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)": 12.0
  },
  "WeatherControl": {
    "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)": "Storm"
  },
  "Permissions": {
    "UseAdminOverridePermission": "daynightweathercontroller.adminoverride"
  }
}
```

### Vanilla Server with Staff Tools
```json
{
  "EnvironmentControl": {
    "Enabled": true
  },
  "TimeControl": {
    "Mode (1=DAY, 2=NIGHT, 3=CUSTOM, 4=VANILLA)": 4,
    "DayLengthMinutes (Custom Only)": 55,
    "NightLengthMinutes (Custom Only)": 5,
    "LockedHour (DAY/NIGHT Only, Recommended Day=12.0, Night=0.0)": 12.0
  },
  "WeatherControl": {
    "Mode (Clear, Overcast, Rain, Storm, Fog, Vanilla)": "Vanilla"
  },
  "Permissions": {
    "UseAdminOverridePermission": "daynightweathercontroller.adminoverride"
  }
}
```

## Notes

- `DAY/NIGHT + Clear/Overcast` is the **lowest-risk path** and is recommended for PvP servers.
- `CUSTOM` mode and non-clear global weather use **true world control**.
- `VANILLA + Vanilla` leaves the environment untouched.
- The plugin automatically migrates older config formats to the current format.
- On unload, the plugin clears local visual overrides and restores vanilla progression/weather.

## Installation

1. Place `DayNightWeatherController.cs` in your server's `oxide/plugins` folder.
2. Load or reload the plugin.
3. Edit the generated config in `oxide/config/DayNightWeatherController.json`.
4. Grant the admin override permission to the groups or users you want to use local staff commands.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

This project is released under the MIT License. See [LICENSE](LICENSE).
