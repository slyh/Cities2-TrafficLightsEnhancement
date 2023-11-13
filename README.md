This repository contains mods that offer minor quality-of-life improvements over the base game of Cities: Skylines II.

**Disclaimer: These modifications are highly experimental. Your game may crash more frequently, and your save files could be corrupted.**

## Notice

* This project has moved to BepInEx version 6, which is not compatible with the previous BepInEx version 5

* Suggestions wanted! What additional signal phases do you think should be included in the Traffic Lights Enhancement mod? Leave your opinion in the [Discussions section](https://github.com/slyh/Cities2-Various-Mods/discussions)! 

## Mods

* Traffic Lights Enhancement
   * Set traffic lights to various predefined signaling modes
   * Guide available [here](https://github.com/slyh/Cities2-Various-Mods/tree/main/TrafficLightsEnhancement/README.md)
   * Compatible with version 1.0.13f1

## Installation

1. Remove BepInEx 5 if you have that installed. Otherwise, proceed to step 2

   * To remove BepInEx 5, you need to delete the `BepInEx` folder, the `doorstop_config.ini` file, and the `winhttp.dll` file from your game's installation directory

2. Install [BepInEx 6](https://builds.bepinex.dev/projects/bepinex_be)

   * Download `BepInEx-Unity.Mono-win-x64-6.0.0-be.674+82077ec.zip` (or a newer version), and unzip all of its contents into the game's installation directory, typically `C:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II`

   * The installation directory should now have the `BepInEx` folder, the `doorstop_config.ini` file, and the `winhttp.dll` file

3. Run the game once, then close it. You can close it when the main menu appears

4. Download the mod you like from the [release page](https://github.com/slyh/Cities2-Various-Mods/releases). Unzip it into the `Cities Skylines II/BepInEx/plugins` folder

5. Launch the game, mods should be loaded automatically

## Thanks

[Cities2Modding](https://github.com/optimus-code/Cities2Modding): Info dump / guides for Cities Skylines 2 modding

[BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework

[Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime