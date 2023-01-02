# AlbaVR
VR mod for Alba: A Wildlife Adventure.

## Features
- Play the game from start to finish as a stationary VR experience. **Motion controls are not supported.** Keyboard and mouse controls work, but a gamepad is strongly recommended.
- Optional support for playing the entire game in first person mode with snap turn (gamepad controls only) or smooth turn.
- HUD toggle for a more immersive experience.
- Works with the Steam and Epic versions of the game. Other versions probably work too but have not been tested.
- Only tested on Valve Index, but should support all SteamVR and Oculus headsets.

## Known issues
The game is 100% completable in VR, but please keep these issues in mind:

### Gameplay
- There are currently no comfort options provided, aside from a toggleable first person view with optional snap turn. If you're prone to motion sickness, you may find some camera movements in the game difficult to handle.
- Playing in one save slot and then switching to another save slot currently breaks VR rendering. If you want to use a different save slot, restart the game.

### Graphics
- The outer edges of the map do not render properly in the right eye. You may want to close this eye when using the map if it causes you discomfort.
- Some visual effects lack polish, like when the screen fades to black.

## Installation/configuration
1. Download `BepInEx_x64_5.4.21.0.zip` from https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21
2. Download the latest version of the mod from https://github.com/wouterpleizier/AlbaVR/releases/latest
3. Extract the contents of both zip files to the folder that contains the game's executable (`Alba.exe`)

The contents of your game folder should now look something like this (make sure the folder and files in bold exist):

:file_folder: `Alba_Data`
:file_folder: **`BepInEx`**
:file_folder: `MonoBleedingEdge`
:page_facing_up: `Alba.exe`
:page_facing_up: **`doorstop_config.ini`**
:page_facing_up: **`Play with Oculus.bat`**
:page_facing_up: **`Play with SteamVR.bat`**
:page_facing_up: **`Play without VR.bat`**
:page_facing_up: `UnityCrashHandler64.exe`
:page_facing_up: `UnityPlayer.dll`
:page_facing_up: **`VRMODE.txt`**
:page_facing_up: **`winhttp.dll`**

To enable the mod and start playing, run `Play with SteamVR.bat` or `Play with Oculus.bat` depending on which headset you use. This setting is remembered, so after the first time you can just launch the game normally. To switch back to non-VR mode, run `Play without VR.bat`.

You'll probably need to use your VR software's recentering function once the game has started. After that, you can press the in-game recenter key (`Home` by default) to realign the camera position to your head when necessary.

The first time you launch the game after installing the mod, a file named `AlbaVR.ini` will appear in the game folder. You can configure various settings by editing this file with a plain text editor such as Notepad. Each setting includes information about its purpose and its default and expected values.

## Hotkeys
The default hotkeys are listed below. Bindings can be changed in `AlbaVR.ini`.

| Key      | Command                       |
| -------- | ----------------------------- |
| `Home`   | Recenter VR position          |
| `Insert` | Toggle full first person view |
| `H`      | Toggle HUD                    |

## Full first person view
By default, the VR view follows the third and first person cameras that are also used in non-VR mode. If this is uncomfortable or if you'd like a more immersive experience, you can force the game to always use a first person view (except during cutscenes, dialogs and certain interactions). This can be toggled in-game using the `Insert` key by default. When enabled, certain camera smoothing settings and behaviours are also changed to make the experience more similar to other VR games. Most notably, snap turning is enabled by default when using a gamepad. This, along with other settings such as turn speed, can be changed by editing `AlbaVR.ini`.

## Performance
By default, the game renders at your HMD's native resolution. If you want to reduce or increase the render resolution, change the render scale/multiplier setting in your VR software and restart the game. The resolution in the game's graphics settings menu doesn't do anything in VR.

The game isn't super demanding, so most VR-ready systems should be able to run it at a decent frame rate. However, expect some frame drops when using the phone camera in areas with a lot of things to render.

If you need a performance boost, I recommend turning off the Shadows option in the in-game graphics settings menu. This makes the game use prebaked shadows, which is much faster and doesn't impact the visual quality much. Tweaking the LOD setting (either in-game or in `AlbaVR.ini`) can also help.

## Credits
- Original game developed by Ustwo Games
- VR mod developed by Wouter Pleizier a.k.a. Blueberry_pie
- VR patcher developed by [MrPurple6411](https://github.com/MrPurple6411/Modding-Tools)

## Donate
If you enjoy the mod, consider donating to Alba's Forest via [Ecologi](https://ecologi.com/albasforest) or buying a gift copy of the game for your friends. If you'd like to show your support to me, you can donate via [PayPal](https://www.paypal.com/paypalme/wouterpleizier) or leave a message via [e-mail](mailto:wouterpleizier@gmail.com) or [Steam](https://steamcommunity.com/id/Blueberry_pie/).
