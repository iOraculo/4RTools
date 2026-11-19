# Changelogs

## [v2.11.0] - 2026-11-19
### Added
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
