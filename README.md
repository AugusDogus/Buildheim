# PlanBuild

A client-only blueprint planner for Valheim. Capture buildings, load `.blueprint` or `.vbuild` files, and position a private hologram as a building guide. The server and other players do not need PlanBuild.

Install BepInEx, Jötunn 2.30.0 or newer, and this build on your client. No server installation is needed.

Compatibility checks target Valheim **1.0.7** (Steam build 25185596), using BepInExPack 5.4.2350 and Jötunn 2.30.0. The release build and automated format, material and placement-contract tests pass against those assemblies. In-game loading, rendering and multiplayer placement remain unverified. Jötunn 2.30.0 includes the upstream Valheim 1.0.7 fixes; older Jötunn versions are not supported by this build.

## Usage

1. Join a world and press **End** to open the planner.
2. Put blueprint files in `BepInEx/config/PlanBuild/blueprints`, or use **Capture and save** to capture player-built pieces within a radius of your character. Your feet define the capture origin.
3. Select a blueprint, move its origin, and adjust its position and rotation. Hold Shift for 1 m position steps instead of 0.1 m.
4. Select **Click to build**, close the planner, and equip the normal hammer. Aim at a missing hologram piece, then click. Assistance selects the learned recipe, aligns its position and rotation, and finds a nearby real placement surface without requiring your crosshair to hit that surface.
5. Valheim validates and places the piece, then consumes its materials, stamina and tool durability normally. Completed pieces disappear from the hologram.

You must carry the materials yourself. Nearby containers do not satisfy assistance's inventory check. Normal hammer reach, recipe knowledge, crafting stations, wards and placement restrictions still apply. Free-build world settings must be disabled for assistance. Scaled imports remain visual guides because normal hammer placement cannot reproduce arbitrary scales.

Select **Guide** for ordinary hammer behavior. Hold Shift while adjusting position for 1 m steps. The hologram remains fixed until cleared or you leave the world; placement positions are not saved across sessions.

The planner key and blueprint directory are configurable under `[Client]` in `marcopogo.PlanBuild.cfg`. Blueprint files stay on your computer. Files can be shared manually.

With the planner closed, hold **Ctrl + mouse wheel** to move the hologram toward/away from your view, **Ctrl + X + wheel** to move sideways, **Alt + wheel** to adjust height, or **Ctrl + Alt + wheel** to rotate. Hold **Shift** for 1 m / 22.5 degree steps instead of 0.1 m / 1 degree. Building pauses while these positioning modifiers are held. The wheel controls the hologram without also zooming the camera or rotating the hammer piece.

Only meshes are drawn for holograms. They have no collision, network objects, or persistent world data. Missing prefabs and unsupported visuals are counted in the planner. Modded pieces still require their original piece mods wherever those mods require installation, including the server. Terrain instructions, container inventories and custom snap markers are not applied.

## Autobuild

Select **Autobuild** in the planner, close it, and walk with the normal hammer equipped. It automatically tries nearby missing pieces without aiming or clicking. Press **Home** to switch between autobuild and click-to-build; the hotkey is configurable under `[Client]`.

Autobuild attempts at most one piece every half-second, and respects the normal hammer cooldown. It requires inventory materials, stamina, hammer durability, a learned recipe and any crafting station. Both assisted modes probe around the selected piece for real surfaces within reach and line of sight, then run the normal placement checks. Each probe stops at its first collider. Blocked autobuild candidates are skipped and retried after other pieces. The HUD explains specific placement failures when Valheim reports them; generic rejections ask you to check overlap, support and surface restrictions.

Opening a menu or putting the hammer away pauses autobuild. Loading a blueprint, dying or leaving the world resets it to click-to-build. The HUD shows when autobuild is on. Autobuild stays within the selected layer and does not advance to the next layer automatically.

## Building by layers

Use **Bottom** in the planner to start with the lowest occupied layer, then **Next** to work upward. **Previous** moves down and **All layers** restores the full blueprint. Empty height bands are skipped.

The **Layer height** slider sets band thickness from 0.5 to 4 m (default 2 m). Heights are relative to the hologram origin. Whole pieces belong to the band containing their origin, so a tall wall can extend past a band's boundary. Changing the band thickness restarts a selected layer at the bottom.

Only the selected layer's missing pieces are shown or eligible for assisted placement and autobuild. A built/total counter shows progress within that layer. Finish it, open the planner and select **Next**, then close the planner to resume. Existing real construction remains visible throughout.

## Upgrading from the original mod

This is a replacement for the original shared planning workflow. Plan Hammer, rune inventory items, totems, shared plans, server blueprint sharing, terrain tools and direct bulk building are no longer registered. Old server settings do not apply.

Back up characters and worlds that contain original PlanBuild items or planned pieces before switching. This version does not migrate those networked objects. Finish or remove old plans with the original mod first. Existing local blueprint files can still be imported.

## Development

Build with a .NET SDK and local Valheim/BepInEx assemblies:

```sh
dotnet build PlanBuild/PlanBuild.csproj -p:VALHEIM_INSTALL="/path/to/Valheim" -p:BEPINEX_PATH="/path/to/profile/BepInEx"
```

`BEPINEX_PATH` defaults to the game's `BepInEx` directory. `Environment.props` can supply these paths. Reference assemblies are publicized inside `obj`, leaving the installed game unchanged. Building does not install or publish the mod.

The explicit compile list includes only the client runtime and the reusable piece format. The legacy planning, networking and asset source remains in the repository for reference and is excluded from the assembly. HookGenPatcher is no longer required.

Run format tests and placement compatibility checks with the same paths:

```sh
dotnet test PlanBuildTest/PlanBuildTest.csproj -p:VALHEIM_INSTALL="/path/to/Valheim" -p:BEPINEX_PATH="/path/to/profile/BepInEx"
```

These tests do not run Unity. Before release, test with a vanilla server and an unmodded observer: capture/load a blueprint, align it, place pieces with exact materials, retry without materials, and check unknown recipes, missing stations, wards, blocked placement, reach, duplicate clicks, disconnect/reconnect and normal building with assistance off. Also verify autobuild with empty inventory, depleted stamina, a broken hammer, blocked surfaces and open menus; toggle Home and switch layers to ensure no queued or hidden-layer placement occurs. Confirm the observer sees only completed vanilla pieces and the server save contains no PlanBuild prefabs.

## Credits

The original PlanBuild mod was created by __[MarcoPogo](https://github.com/MathiasDecrock)__

Blueprint functionality originally created by __[Algorithman](https://github.com/Algorithman)__ & __[Jules](https://github.com/sirskunkalot)__

Blueprint Marketplace GUI created by __[Dreous](https://github.com/imcanida)__

All further coding by __[MarcoPogo](https://github.com/MathiasDecrock)__ & __[Jules](https://github.com/sirskunkalot)__

Special thanks to __[Jere](https://github.com/JereKuusela)__ for exchanging code and ideas

Made with Löve and __[Jötunn](https://github.com/Valheim-Modding/Jotunn)__

## Contact

Source available on GitHub: [https://github.com/sirskunkalot/PlanBuild](https://github.com/sirskunkalot/PlanBuild)﻿. All contributions welcome!

You can find us at the [Jötunn Discord](https://discord.gg/DdUt6g7gyA) (```Jules#7950``` and ```MarcoPogo#6095```).
