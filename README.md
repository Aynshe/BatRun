# BatRun try

Launcher for RetroBat supporting XInput/DInput gamepads, with the hotkey "Select + Start".
Can also be started directly with Windows startup in "custom shell" mode, replacing the Windows Explorer (Explorer.exe).
_____________________________________________________________________________________________________________________________________________________
https://github.com/user-attachments/assets/1a56cbad-afbd-4ec9-89dc-7dab3e4f5f61
___________________________________________________________________________________________
https://github.com/user-attachments/assets/2e692905-ea64-4026-b146-be997264fc46
_____________________________________________________________________________________________________________________________________________________
<img width="786" height="543" alt="image" src="https://github.com/user-attachments/assets/903374b4-4f0e-4219-8213-4f077c937084" />
_____________________________________________________________________________________________________________________________________________________

## ✨ New — Automatic Launch game (BatRun 2.2.0)

- **System selection**: Choose a system (console, arcade, etc.) and BatRun will directly launch a game from that system.
- **Random mode** *(optional)*: Enable the option to have a game randomly selected from the chosen system.

<img width="1736" height="1096" alt="image" src="https://github.com/user-attachments/assets/73a27753-5c1a-405a-a342-9536e5db3e35" />

- fix / Refactoring code

> **Requirement**: [.NET Desktop Runtime 8.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) must be installed on your system.
_____________________________________________________________________________________________________________________________________________________
## 2.1.0
### New "Hide ES during loading" option
- 🎬 Hide EmulationStation during loading with customizable video (RetroBat intro alternative / waits for ES loading completion / early video stop possible with Start)

### Enabling this option automatically disables:
- BatRun splash screen on startup (available without "Hide ES during loading")
- BatRun splash screen when launching RetroBat (available without "Hide ES during loading")
- Active windows minimization
- RetroBat intro video
_____________________________________________________________________________________________________________________________________________________
# 2.0
A launcher for RetroBat that allows you to use a controller button combination (Hotkey + Start) to launch RetroBat.

### Custom Shell
- Configuration as custom Windows shell
- Command and application execution
- Auto-Hide Applications

### Dynamic Wallpaper
- Video wallpaper support (MP4)
- Animated GIF support
- Movable floating menu
- Automatic wallpaper pause when launching EmulationStation
- Audio volume control for video wallpapers

### Shortcuts System
- Custom shortcuts management interface
- Quick access menu for shortcuts from floating menu
- Ability to add/edit/delete shortcuts
_____________________________________________________________________________________________________________________________________________________
## 1.3

- 🚀 **Added controller vibration when pressing Hotkey + Start** (works with XInput, DirectInput not tested, potential incompatibility with some Bluetooth controllers).
- 🖥 **Automatic startup via scheduled task** (works if `explorer.exe` is not the default Shell on Windows startup).
_____________________________________________________________________________________________________________________________________________________
### CPU Load Notes:
- On a processor like the i5-9600 (Win11) : CPU load is below 1%.
- Tested on an i7-3770K (Win10) : load varies between 2% and 5%.
- No immediate solution to optimize this; additional tests may be required.

## Features

- 🎮 Support for **XInput** and **DirectInput** controllers.
- 🔄 Customizable button mapping.
- 🪟 Optional automatic window minimization.
- 🚀 Automatic startup with Windows (via **Registry**, **shortcut**, or **scheduled task**).
- 📝 Logging system for troubleshooting.

## Installation

1. Download the latest version from the [Releases](https://github.com/Aynshe/BatRun/releases) page.
2. Extract the archive.
3. Run `BatRun.exe`.

## Configuration

### General Settings

- **Focus Duration**: Duration for which the focus process remains active (starts after the configured video duration from BatGui, if enabled).
- **Focus Interval**: Interval between focus attempts for EmulationStation.
- **Start with Windows**: Automatic startup (via **Registry**, **shortcut**, or **scheduled task**).
- **Enable controller vibration** : Enables/disable rumblee (if supported by your controller).
- **Minimize Windows**: Enables/disables window minimization.
- **Enable Logging**: Enables/disables logging.

### Controller Configuration

1. Open **BatRun**.
2. Go to **Configuration > Controller Mappings**.
3. Select your controller.
4. Configure the Hotkey and Start buttons.
5. Save.

### Auto Startup Configuration

1. Launch `BatRun.exe`.
2. Go to **Configuration > Startup Settings**.
3. Select **your choice**.
4. Save.

## Usage

1. Launch BatRun (an icon will appear in the taskbar).
2. Simultaneously press the **Hotkey + Start** buttons on your controller.
3. RetroBat will automatically launch with a confirmation vibration (if supported by your controller).

## Support

- [Discord RetroBat](https://discord.gg/GVcPNxwzuT) “Aynshe”
- [Source Code](https://github.com/Aynshe/BatRun)

## Credits

Developed by AI for Aynshe
