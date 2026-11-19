# Changelogs

## [v2.12.0] - 2026-11-19
### Added
- **Portuguese (PT-BR) interface by default + live `PT-BR / EN` switcher** (`LocalizationManager`): translates all tabs, forms, buttons, tooltips, tray menu, dialogs, server tester messages, and conflict reports live without restarting.
- **Floating Mini HUD** (`MiniHudForm`): compact, draggable, translucent always-on-top overlay displaying live `ON`/`OFF`/`CHAT` status, active profile/character, and real-time HP/SP bars; toggleable from the main header with persisted visibility and screen coordinates in `settings.json`.
- **Keybind Conflict Inspector** (`KeybindConflictDetector`): inspects the active profile across all modules (Toggle, Autopot, AHK, Autobuff, Recovery, Skill Timers, ATK/DEF, Song Macro, Macro Switch) and reports duplicate key assignments while allowing intentional shared cure keys.
- **Full `.zip` Backup & Restore** (`ProfileBackupManager`): export and restore all profiles, custom servers (`supported_servers.json`), and app settings (`settings.json`) in a single archive with Zip Slip and JSON validation.
- **Live Server Address Tester**: new **Test** button in `AddServerForm` (`Client.TestMemoryAddresses`) verifies HP/SP, character name, and active buff count directly against the running Ragnarok process before saving.
- **Smart AutoBuff controls**: configurable **Min SP%** threshold, per-buff recast **Cooldown (`CD(ms)`)**, and automatic pause during incapacitating debuffs (Silence, Freezing, Stun, Deep Sleep) on both Autobuff Skills and Stuffs tabs.
- **Automated Release workflow** (`.github/workflows/release.yml`): builds, tests, packages `4RTools-<tag>.zip` + `4RTools.exe` + `SHA256SUMS.txt`, and publishes GitHub Releases automatically on `v*` tags.

## [v2.11.0] - 2026-11-19
### Added
- **Dark Mode** and **Always on Top** toggles in the main header, persisted globally in `settings.json` (`AppSettings` + `ThemeManager`).
- **Real-time HP/SP vital bars** (`VitalBar`) below the active character name using the 16-byte bulk vitals read.
- **Auto-Switch Profile by Character**: bind profiles to character names in the Profiles tab (`ProfileForm`) to automatically switch profiles when logging in or changing characters (while automations are `OFF`).
- **Chat-Safe Pause**: optional `Chat` checkbox on the ON/OFF panel pauses automations while typing in Ragnarok chat (`Enter` toggles, `Esc` resumes, `CHAT` orange indicator) while keeping F-key Autopot active (`ChatPauseState`).
- **Clear All Keys** button in Skill Spammer (`AHKForm`).
- Search/filter bar and Clear All button on Autobuff Skills and Autobuff Stuffs tabs.
- Optional "Focus" mode to pause automations when the Ragnarok window is not in focus (saved per profile).
- Persistent Sound toggle setting per profile.
- Automatic client-close detection that cleanly stops all automations when the game process exits.
- Per-lane `Loop` toggle in Macro Switch.
- Profile Import and Export buttons in the Profiles tab.
- Rotating diagnostic file logger (`logs/4rtools.log`) and unhandled exception capture.
- Automated Windows CI workflow (`.github/workflows/ci.yml`) and xUnit test suite (`tests/4RTools.Tests`).

### Performance & Architecture
- Replaced 100 individual `ReadProcessMemory` syscalls per loop with a single 400-byte bulk read in `AutoBuff`, `StatusRecovery`, and `DebuffsRecovery` (and single 16-byte bulk vitals read in `Autopot`).
- Eliminated 150+ redundant JSON disk writes on profile switches via `ProfileSingleton.SuppressSave()` and atomic file writes.
- Consolidated `StatusRecovery` and `DebuffsRecovery` onto shared `StatusEffectRecoveryBase`.
- Requested minimal read-only process permissions (`PROCESS_VM_READ | PROCESS_QUERY_LIMITED_INFORMATION`), properly disposed native handles and `Process`/`SoundPlayer` objects, and removed unused ads/telemetry code.

### Fixed
- Replaced obsolete/deadlocking `Thread.Suspend()` in `_4RThread` with cooperative `CancellationToken` background threads; stopped thread leaks in `ATKDEFMode` and `AutoRefreshSpammer`.
- Fixed `Autopot` `hpPotCount` counter bug, dead-character potting, and duplicate event handler accumulation on profile switches.
- Fixed `AutoBuff` Quagmire loop using `break` instead of `continue`.
- Fixed `MacroSongForm` Reset button not resetting the lane and fixed `cDagger`/`cInstrument` array checks.
- Fixed `MacroSwitchForm` click lookup bug, sparse slot `KeyNotFoundException`, and `hasClick` preservation.
- Wired disconnected `Auto Stand` checkbox and `Elvira Candy Cooldown` input in `DebuffRecoveryForm`.
- Fixed `ProfileSingleton.Load` leaking settings from previous profiles, fixed profile names with dots, and sanitized profile names.
- Replaced fragile `Enum.Parse` key conversions with `KeyInterop`-based `KeyHelper` and added `try/finally` safety for `AHK` `noShift`.
- Hardened `AutoPatcher` (handle lifecycle, `NaN` progress guard, Zip Slip validation, `.zip` support) and `ClientUpdaterForm` (`ProgressBar.Maximum` overflow).

## v[1.0.0] - 25/02/2022 (First Stable Version)
- Autopot (HP/SP with delay customization)
- Autobuff Status (Only for Poison, Silence, Blind, Confusion, Curse, Hallucinationwalk)
- Autoclick (Skill Spammer with delay customization) 
- Profiles Support (How many profiles you want)
- Ingame ON/OFF Button (End Key), you don't need to minimize your game screen.



## [v1.1.0] - 2022-02-26
### Added
- Auto-Refresh Spammer
- Auto status. Removes Poison, silence, blind, confusion, curse and hallucination automatically using Panacea, Green Potion or Royal Jelly
- New UI to manager profiles

### Fixed
- Fixed a bug where AHK was consuming more than 5% of CPU. Now the consumer of the CPU is under of 1%.
- Always request execution level as administrator


## [v1.2.0] - 2022-02-28
### Added
- Autobuff for Stuffs
- Added new keys (Space, Page UP, Page Down, Insert, Delete)

### Fixed
- On change profile not start threads anymore.
- App don't crash anymore with a invalid json profile.
