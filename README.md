<p align="center">
  <img src="/assets/images/combo-tools.png">
</p>

# 4RTools
This is an all-in-one tool for **Ragnarök Online** servers. You will be able to Autopot, Spam Skill, Use Macro Songs and much more. Hope you enjoy it and collaborate with us. If you have found a bug or want to request a feature, feel free to open an issue!

**THIS IS A FREE TOOL, YOU CAN MAKE EVERYTHING YOU WANT UNDER MIT LICENSE. WE ARE OPEN FOR IDEAS AND COLLABORATIONS**

*Made from community TO community*.

Your Feedback is Welcome!

**Discord:** https://discord.gg/HRWvG5ut
**Website:** https://www.4rtools.com.br/

<img src='assets/images/ragnarok-icon.png' width='40'>

## Running & Building the Project
This project targets **.NET Framework 4.7.2** (WinForms) and can be built on Windows with Visual Studio 2022 or MSBuild:

```powershell
nuget restore 4RTools.csproj -PackagesDirectory packages
dotnet restore tests/4RTools.Tests/4RTools.Tests.csproj
msbuild 4RTools.sln /p:Configuration=Release /p:Platform="Any CPU"
```

### Running Unit Tests
```powershell
dotnet test tests/4RTools.Tests/4RTools.Tests.csproj -c Release
```

## Features
- [x] Full **Portuguese (PT-BR default)** & **English (EN)** interface with live language switcher (`settings.json`)
- [x] Dark Mode, Always on Top & floating **Mini HUD** overlay (persisted in `settings.json`)
- [x] Real-time HP/SP vital bars in the main header and Mini HUD
- [x] ON/OFF Button (shortcut key, persistent Sound toggle, Focus-only pause & Chat-Safe Pause)
- [x] Automatic disconnect detection when the Ragnarok client closes
- [x] Autopot (Normal & Yggdrasil; F-key pots stay active during Chat-Safe Pause)
- [x] Autobuff status & Elvira Candy cooldown support + Auto Stand
- [x] Manage Profiles (Create, Rename, Copy, Delete, Import/Export, Full `.zip` Backup/Restore, Auto-Switch by Character & Keybind Conflict Inspector)
- [x] AHK Spammer (Compatibility & Speed Boost modes + Clear All Keys)
- [x] Auto Refresh Spammer (6 Skill Timer slots)
- [x] Smart Autobuff Stuffs & Skills (instant search filter, Clear All, Min SP% threshold, per-buff recast cooldown & incapacitating debuff pause)
- [x] Song Macro (with working per-lane Reset)
- [x] Macro Switch / Macro Chain (with click support & per-lane Loop toggle)
- [x] ATK x DEF Mode switch
- [x] Custom Server memory address manager with live **Test** verification

#### References
https://github.com/k1ngJ/dtAP

## Collaborators
<a href="https://github.com/4RTools/4RTools/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=4RTools/4RTools" />
</a>
