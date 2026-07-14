# BatRun 

Launcher for RetroBat supporting XInput/DInput gamepads, with the hotkey "Select + Start".
Can also be started directly with Windows startup in "custom shell" mode, replacing the Windows Explorer (Explorer.exe).
_____________________________________________________________________________________________________________________________________________________
https://github.com/user-attachments/assets/1a56cbad-afbd-4ec9-89dc-7dab3e4f5f61
___________________________________________________________________________________________
https://github.com/user-attachments/assets/2e692905-ea64-4026-b146-be997264fc46
_____________________________________________________________________________________________________________________________________________________
<img width="786" height="543" alt="image" src="https://github.com/user-attachments/assets/903374b4-4f0e-4219-8213-4f077c937084" />
_____________________________________________________________________________________________________________________________________________________

## ✨ (BatRun 3.1.0)

- **Plugin Management & Integration**: Introduces a dedicated Plugin Management interface inside BatRun. Users can directly browse, download, install, configure (`⚙️ CONFIG`), reveal files (`📁 REVEAL`), or uninstall plugins fetched dynamically from the GitHub releases API.
- **Wallpaper default configuration & video choice**: For demonstration purposes of this pre-existing feature, it is now activated by default on fresh installations and during configuration migration from v3.0.0 and below (configures a default loading video, enables with Windows Explorer, enables audio, and loops video).
- **Wallpaper Loop Video Mute**: Audio of loop videos is now muted starting from the second loop to avoid audio repetition annoyance. An informative comment has been added in the UI.
- **Updater robustness**: Proactively kills `BatRunGuardian.exe` and `BatRun.exe` before updating, and implements backup/cleanup for `BatRunGuardian.exe` and `BatRunGuardian.dll` to prevent Windows file lock issues.
  *(⚠️ **Note for upgrading from v3.0.0 or lower**: If the updater gets stuck or loops infinitely, please manually stop the `BatRunGuardian.exe` process via the Windows Task Manager to allow the script to complete. From v3.1.0 onwards, this termination is handled automatically.)*
- **Collapsible Sections**: Header sections ("MAIN PROJECTS" and "EXPERIMENTAL") in the plugins list are now collapsible with header indicators (▶ / ▼) and are collapsed by default at startup.
- **Releases Versioning**: Groups GitHub releases to display only the single latest version of each plugin (handling `📥 INSTALL`, `🔄 UPDATE` or `✅ INSTALLED` dynamically).
- **Dynamic Plugin Configuration Editor (`⚙️ CONFIG`)**: Launches plugin executable to generate `.ini` if missing before opening the editing form, and preserves original file formatting/comments.
- **Uninstall Paths**: Cleans leading slashes in relative paths to fix `Path.Combine` ignoring target directories during uninstallation.

## (BatRun 3.0.0) 

- Arcade Mode (A silly idea, but I'm sharing it ^^ as it's part of BatRun) 
*This mode originally started as a personal project to simply lock my arcade cabinet without my authorization. It eventually evolved into an alternative credit management system to control front-end usage... If the cabinet is set to freeplay on all games, added credits are managed as gameplay time. A web page is also available to administer it from a browser, where you can see the live game in progress, as well as game history and play time.*


# Warning - This special mode is very intrusive...

**Information: The arcade mode settings also include a "moonlight" setting. This is a very unstable function (its development is experimental, and I'm not sure I'll continue fixing the problems). It allows you to run games remotely from a web browser via "moonlight-web-stream" (Sunshine must be installed on the machine) through a portal that lists games. I've made some modifications to the settings and web display. The "streamer.exe" and "web-server.exe" applications have not been modified and are not included. I believe the latest updates should remain compatible. "streamer.exe" and "web-server.exe" must be placed in the ".moonlight-web-stream" folder. This is unstable; you can test it, but don't expect reliable use. Furthermore, to access it remotely, you need to open the Sunshine and Moonlight web server access points... there is a proxy function to redirect port 8080 to port 4321 of the portal... which adds even more instability.**

- **Credit Management**: Time-based countdown per credit, automatic locking at zero.
- **Dedicated Hardware**: Assign a unique device authorized to add credits (e.g., RP2040-Zero emulating a keyboard key connected to a coin acceptor).
- **Process Control**: Pause (NtSuspendProcess) and Termination (WM_CLOSE/Kill) of executables.
- **Operator Mode**: Accessible universally via the `9` key (or `NumPad9`). Features a password interface (default: `admin` if not set) to unlock without credits, Free Play mode, manual credit addition, and a floating mini-overlay to quickly relock the cabinet after maintenance.
- **Crash Recovery & Guardian**: A background watchdog (`BatrunGuardian`) monitors BatRun for crashes or UI freezes. If an issue occurs, it restarts the system and utilizes a `session_state` to seamlessly restore current credits, game time, and Free Play status. You can manually call the Guardian overlay at any time by pressing the `0` key (or `NumPad0`) to manually force a restart.
- **Dashboard & Network**: Web Dashboard, remote control API and monitoring via UDP Discovery.
- **Security & Focus Management**: Blocks dangerous Windows shortcuts (Win, Ctrl+Esc, Alt+F4, Alt+Esc) via a Low-Level Keyboard Hook to prevent players from reaching the desktop.
- **Safe Task Switcher (Alt+Tab)**: Controlled Alt+Tab mode displaying a custom dedicated UI, which strictly authorizes switching only to a predefined whitelist of applications.
- **Core Engine Upgrade**: Migration to **.NET 10.0**.
- **Code Refactoring**: Structural project reorganization (UI, Core, Input, Models, Utils) following modern .NET standards.

- **fix** Splashtop stuck start RetroBat (enable by default)
- **fix** stuck boot ESLoadingPlayer vidéo alternative
- **Misc**

## fix (BatRun 2.2.5)

- Enhanced ESLoadingPlayer to add detailed logging for video selection and file existence, improved timeout handling to avoid blocking RetroBAT launch

## fix (BatRun 2.2.4)

- Added a combined function with RetroBat.exe (starting with version 7.5.0.1) so that Batrun does not compete for focus
- Lock DPI scaling

## Automatic Launch game (BatRun 2.2.0)

- **System selection**: Choose a system (console, arcade, etc.) and BatRun will directly launch a game from that system.
- **Random mode** *(optional)*: Enable the option to have a game randomly selected from the chosen system.

<img width="1736" height="1096" alt="image" src="https://github.com/user-attachments/assets/73a27753-5c1a-405a-a342-9536e5db3e35" />

- fix / Refactoring code

> **Requirement**: [.NET Desktop Runtime 10.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) must be installed on your system.
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
