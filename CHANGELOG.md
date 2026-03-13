# Changelog

## 0.5.1
- Final validation build
- Replaced dynamic custom cycle helper with strongly typed `TOD_Time`
- Finalized cleanup and validation pass

## 0.5.0
- Reworked custom cycle to use TOD time progression rate instead of manually bumping hour
- Added unload cleanup for local visual overrides
- Added low-frequency weather verification for forced weather modes
- Restored vanilla time progression and weather on unload

## 0.4.0
- Added `Overcast` as a safe LocalVisualOverride weather mode
- Improved startup logging
- Tightened config safety guards
- Improved custom cycle precision
- Expanded config migration support for weather labels

## 0.3.1
- Switched to descriptive config format
- Removed redundant `AdminOverrides` section
- Added automatic config migration/merge support
- Permission is now the single gate for admin commands

## 0.3.0
- Added first true world control implementation
- Added 10-second world control timer
- Added custom cycle prototype
- Added storm/day and vanilla control paths

## 0.2.1
- Fixed client console command usage for local time overrides

## 0.2.0
- Added `/realtime` command
- Added `/resettime` alias
- Added `/envstatus`
- Expanded runtime strategy detection and weather handling

## 0.1.0
- Initial working build
- Local visual override for default PvP path
- Admin local time override commands
