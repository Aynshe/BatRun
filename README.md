# BatRun
Launcher for RetroBat with Hotkey gamepad select/back+start xinput or dinput  /  -publish self contained-
Mapping automatic also in Dinput - "source : gamecontrollerdb in RetroBat" "But the BatRun plugin startup is slower..." (start with a wait splash)

Having no other Dinput Joystick than my old "SideWinder Game Pad Pro USB version 1.0", I am not able to test.
I do not know if it is my old Dinput controller, I sometimes need to insist a little on the hotkey.
*the code is certainly not very clean... .!

_____________________________________________________________________________________________________________________________________________________

https://github.com/user-attachments/assets/6a5e2c1b-204f-457c-af81-3b7bd17714eb

_____________________________________________________________________________________________________________________________________________________

# BatRun v1.3

A launcher for RetroBat that allows you to use a controller button combination (Hotkey + Start) to launch RetroBat from any Windows application.

## New Features in Version 1.3

- 🚀 **Added controller vibration when pressing Hotkey + Start** (works with XInput, DirectInput not tested, potential incompatibility with some Bluetooth controllers).
- 🖥 **Automatic startup via scheduled task** (works if `explorer.exe` is not the default Shell on Windows startup).

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

- [Discord RetroBat](https://discord.com/invite/k8mg99cY6F) “Aynshe”
- [Source Code](https://github.com/Aynshe/BatRun)

## Credits

Developed by AI for Aynshe

_____________________________________________________________________________________________________________________________________________________

# BatRun v1.3

Un lanceur pour RetroBat qui permet d'utiliser une combinaison de touches manette (Hotkey + Start) pour lancer RetroBat depuis n'importe quelle application Windows.

## Nouveautés de la version 1.3

- 🚀 **Ajout de la vibration manette lors de la combinaison Hotkey + Start** (fonctionne avec XInput, DirectInput non testé, incompatibilité possible avec certaines manettes Bluetooth).
- 🖥 **Démarrage automatique via tâche planifiée** (compatible si `explorer.exe` n'est pas le Shell par défaut au démarrage de Windows).

### Remarque sur la charge CPU :
- Sur un processeur type i5-9600 (Win11) : charge CPU inférieure à 1%.
- Testé avec un i7-3770K (Win10) : charge variant entre 2% et 5%.
- Aucune solution immédiate pour optimiser cela ; des tests supplémentaires pourraient être nécessaires.

## Fonctionnalités

- 🎮 Support des manettes **XInput** et **DirectInput**
- 🔄 Configuration personnalisable des boutons
- 🪟 Minimisation automatique des fenêtres (optionnel)
- 🚀 Démarrage automatique avec Windows (via **Registre**, **raccourci**, ou **tâche planifiée**)
- 📝 Système de logs pour le dépannage

## Installation

1. Téléchargez la dernière version depuis la page [Releases](https://github.com/Aynshe/BatRun/releases).
2. Extrayez l'archive.
3. Lancez `BatRun.exe`.

## Configuration

### Configuration Générale

- **Focus Duration** : Durée pendant laquelle le processus focus est actif (débute après la durée de la vidéo configurée depuis BatGui, si activée).
- **Focus Interval** : Intervalle entre les tentatives de focus de EmulationStation.
- **Start with Windows** : Démarrage automatique (via **Registre**, **raccourci**, ou **tâche planifiée**).
- **Minimize Windows** : Active/désactive la minimisation des fenêtres.
- **Enable controller vibration** : Active/désactive la vibration (si supportée par votre manette).
- **Enable Logging** : Active/désactive les logs.

### Configuration des Manettes

1. Ouvrez **BatRun**.
2. Allez dans **Configuration > Controller Mappings**.
3. Sélectionnez votre manette.
4. Configurez les boutons Hotkey et Start.
5. Sauvegardez.

### Configuration du démarrage automatique

1. Lancez `BatRun.exe`.
2. Allez dans **Configuration > Startup Settings**.
3. Sélectionnez **votre choix**.
4. Sauvegardez.

## Utilisation

1. Lancez BatRun (une icône apparaît dans la barre des tâches).
2. Appuyez simultanément sur les boutons **Hotkey + Start** de votre manette.
3. RetroBat se lance automatiquement, accompagné d’une vibration de confirmation (si supportée par votre manette).

## Support

- [Discord RetroBat](https://discord.com/invite/k8mg99cY6F)  “Aynshe”
- [Code Source](https://github.com/Aynshe/BatRun)

## Crédits

Développé par AI pour Aynshe


![image](https://github.com/user-attachments/assets/a84403ef-7272-40c2-b6a2-f4f58261b07b)![image](https://github.com/user-attachments/assets/c7564bfe-e699-4183-8db4-32cbd987758d)![image](https://github.com/user-attachments/assets/a179cd65-9fa1-4040-bf07-7d30e2f4e33d)![image](https://github.com/user-attachments/assets/b4848a98-3d84-4709-bbe3-a1ee174c6346)
![image](https://github.com/user-attachments/assets/e796a9dd-1c24-4b30-b02d-45474f1f2e8b)![image](https://github.com/user-attachments/assets/cc1372c1-4191-479d-a656-321454450cac)

![image](https://github.com/user-attachments/assets/40e4294d-5ea1-4046-aa09-cd8de10d850d)



