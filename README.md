<p align="center">
  <img src="banner.png" alt="Buildheim: blueprints for Valheim" width="900">
</p>

<p align="center">Plan it. Gather it. Build it.</p>

A client-only blueprint mod for Valheim, inspired by Litematica. Load or capture a building, position a private hologram, and build it with your normal hammer.

**Only you need the mod.** The server and other players do not need Buildheim. Your blueprint stays local; the pieces you build become ordinary world objects. Building uses materials from your inventory.

## Install

In your r2modman profile:

1. Install **BepInExPack Valheim 5.4.2333 or newer** and **Jötunn 2.30.0 or newer**.
2. Copy this project's `Buildheim.dll` into a folder under `BepInEx/plugins`. Remove the previous PlanBuild DLL when upgrading. Keep only one Buildheim DLL in the profile.
3. Launch with **Start modded**, join a world, and press **End**.

You can build the DLL using the [development instructions](#development) below. Use this project's build: the original PlanBuild package has a different workflow. Restart Valheim after replacing the DLL.

**Compatibility:** compilation and automated checks pass against Valheim **1.0.7**, BepInExPack **5.4.2350**, and Jötunn **2.30.0**. This is a development build. The new UI, targeting, and reconnect workflow still need in-game validation; multiplayer compatibility is not yet fully verified.

## Your first build

1. Put `.blueprint` or `.vbuild` files in `BepInEx/config/Buildheim/blueprints` inside your profile. Extract downloaded ZIPs first. Subfolders work too.
2. Press **End**, open **Blueprints**, and click **Refresh**. Choose a building to load its hologram and open the **Build** tab.
3. Close the planner and position the hologram with the shortcuts below. Loading or moving a hologram does not spend materials.
4. Open **Materials** to see what you need. Gather supplies and carry the materials for the pieces you want to place.
5. Select **Click to build**, close the planner, and equip your normal hammer. Aim at a missing hologram piece and click. Buildheim selects its recipe, position, and rotation, then Valheim checks and places it.

Completed pieces disappear from the hologram as they are detected in the loaded world.

To make your own blueprint, use **New blueprint** in the Blueprints tab or open **Capture**.

## Capture a building

1. Enter a blueprint name and choose **Select / edit box**.
2. Aim at a surface and **left-click** to set corner **A** (cyan). **Right-click** sets opposite corner **B** (orange). A visible box shows the capture area.
3. Adjust the last corner you clicked with **Ctrl + wheel** (forward/back), **Ctrl + X + wheel** (sideways), or **Alt + wheel** (height). Steps are 0.1 m; hold **Shift** for 1 m. **Middle-click** switches which corner you adjust. Click either mouse button again to reposition that corner at your aim point.
4. Enclose the whole building, including its foundation and roof. If both corners are at ground level, raise B with **Alt + wheel**. The box needs width, height and depth.
5. Press **End** to review the corners, box dimensions and piece count, then choose **Save blueprint**. Use **Select / edit box** again to refine it.

Capture includes loaded, player-built pieces whose placement points lie inside the box. Pieces may extend beyond those points, so leave room around the building. Corner A becomes the blueprint origin. Existing files are never overwritten.

While selecting, mouse clicks pick corners without attacking, placing or removing pieces. Your loaded placement is temporarily hidden and autobuild is turned off. Opening the planner ends selection mode; **Clear selection** removes the box. Selection corners last for the current world session.

## Positioning controls

Use these with a hologram loaded and the planner closed. Movement follows your camera's horizontal facing direction.

| Control | Action |
| --- | --- |
| **End** | Open or close the planner |
| **Ctrl + wheel** | Move forward or backward |
| **Ctrl + X + wheel** | Move sideways |
| **Alt + wheel** | Raise or lower the hologram |
| **Ctrl + Alt + wheel** | Rotate the hologram |
| **Shift** with a positioning shortcut | Use larger steps |
| **Home** | Toggle autobuild while the planner is closed |
| **F7** | Enable or disable the current placement while the planner is closed |
| **F8** | Show or hide Buildheim's HUD while the planner is closed |

Normal steps are **0.1 m** and **1°**. Hold Shift for **1 m** and **22.5°**. Building pauses while positioning modifiers are held, and the wheel does not also zoom the camera or rotate the hammer piece. These shortcuts take priority over crouching and sitting while positioning.

The planner slides open and closed with Valheim's inventory timing and uses its opening and closing sounds. Closing releases controls immediately; toggling again during the slide reverses its direction.

**Move to my feet** in the Build tab relocates the blueprint origin to your character. Shortcut hints are shown while the planner is closed, unless you disable the placement or hide the HUD.

## Choose how to build

| Mode | What happens |
| --- | --- |
| **Preview only** | See the hologram and select and place hammer pieces yourself. |
| **Click to build** | Aim at a missing hologram piece and click. Its recipe and alignment are selected for you. |
| **Autobuild** | Walk with your hammer equipped. Nearby eligible pieces are placed without aiming or clicking. |

Both assisted modes find real placement surfaces around the chosen piece, so your crosshair does not have to hit its support surface precisely. Normal hammer reach, line of sight, recipe knowledge, crafting stations, placement restrictions, stamina, and durability still apply. The HUD explains placement failures when Valheim supplies a reason.

Autobuild attempts at most one piece every half-second and respects the hammer cooldown. Menus and putting away the hammer pause it. Press **Home** to turn it off. Loading another blueprint, dying, or reconnecting turns autobuild off as well.

### Take a break

Press **F7**, or choose **Disable placement** in the Build tab, to hide the hologram and its HUD and stop all build assistance. Your position, layers and material checklist stay saved. Normal hammer building and camera controls work while the placement is disabled, and you can still use the Materials tab and record chest contents while gathering.

Press **F7** again or choose **Enable placement** to resume. Disabling turns autobuild off; enable it again explicitly when ready. The placement stays disabled across reconnects until you enable it.

To hide only the build status and shortcut hints, press **F8** or use **Hide HUD** at the top of the planner. Use the same key or **Show HUD** to bring it back. This preference persists across restarts and does not change the hologram or build mode, including autobuild.

## Build from the bottom up

In the Build tab, choose **Bottom** to show the lowest occupied layer. Finish it, then use **Next** to move upward. **Previous** moves down, and **All layers** shows the whole blueprint again. Empty layers are skipped.

Only pieces in the selected layer are shown and eligible for assisted building. Autobuild does not advance layers automatically.

**Layer height** ranges from **0.5 to 4 m**, with a **2 m** default. A piece belongs to the layer containing its origin, so a tall wall can extend beyond that layer. Changing the layer height returns a selected layer to the bottom.

## Materials and gathering

The Materials tab lists costs for unfinished pieces. Switch between the whole blueprint and the selected layer.

| Column | Meaning |
| --- | --- |
| **Need** | Materials required for the remaining pieces |
| **Bag** | Materials currently in your inventory |
| **Chests** | Last known contents of chests you have opened |
| **Gather** | What is still missing after bag and chest counts |

Open chests normally while a blueprint is loaded to record their contents. Opening the same chest again updates its count instead of counting it twice. Changes in observed, loaded chests update their counts; unloaded chests retain their last known contents. Those counts can become stale if someone moves items. **Forget chests** clears the observations.

Check off individual materials, use **Check stocked** to mark rows with nothing left to gather, or **Check all** to mark the current list. **Reset checks** clears your marks.

**Checkmarks are gathering notes.** They do not change quantities or authorize building. Chest contents are never withdrawn automatically: the materials must be in your own inventory when you build. Costs exclude unavailable pieces, which the planner reports. Completion counts reflect construction detected in the loaded world.

## Pick up where you left off

Your active hologram is saved automatically for each character and world, including:

- A copy of the blueprint and its position and rotation.
- Layer height and selected layer.
- Gathering checkmarks and whether you chose Preview only.
- Whether the placement is enabled or disabled.

Reconnect with the same character to the same world to restore it. Moving the original blueprint file does not lose the saved plan. **Autobuild is always off after reconnecting.** Chest observations last only for the current world session.

There is one active hologram per character and world. Loading another blueprint replaces it. **Clear hologram** also removes its saved placement. If a save cannot be read, the planner reports the problem and preserves the file.

## Files and settings

These paths are relative to your r2modman profile:

| Path | Contents |
| --- | --- |
| `BepInEx/config/Buildheim/blueprints` | Imported and captured blueprints |
| `BepInEx/config/Buildheim/placements` | Saved holograms and gathering notes |
| `BepInEx/config/augusdogus.Buildheim.cfg` | Planner, autobuild, placement and HUD keys; HUD visibility; blueprint directory |
| `BepInEx/LogOutput.log` | Mod loading messages and errors |

The configuration file is created on first launch. The positioning modifier combinations are currently fixed.

Upgrading from our earlier PlanBuild builds preserves your setup: Buildheim copies `marcopogo.PlanBuild.cfg` into its new config file if one does not already exist, including custom keys and blueprint directories. If the profile already has `BepInEx/config/PlanBuild` and no Buildheim data folder, it continues using that existing folder for blueprints and saved placements. Fresh installs use the Buildheim paths above. Original files are preserved.

## Blueprint support

Buildheim reads `.blueprint` and `.vbuild` files, up to **10,000 pieces** and **8 MB** per file.

- Terrain edits, container contents, and custom snap markers are not applied.
- Pieces with nonstandard scales can be displayed but cannot be placed by hammer assistance.
- Modded pieces require their original piece mods, including on the server when those mods require it.
- Assisted building requires resource costs. Disable the world's free-build setting to use it.

When switching from the original PlanBuild, finish or remove its shared plans with the original mod first and back up affected worlds and characters. This version does not migrate its plan objects, runes, Plan Hammer, or totems. Existing local blueprint files can still be imported.

## Development

Use a **.NET 8 SDK or newer**, a local Valheim installation, and BepInEx from your mod profile. From the repository root:

```sh
dotnet build PlanBuild/PlanBuild.csproj -c Release \
  -p:VALHEIM_INSTALL="/path/to/Valheim" \
  -p:BEPINEX_PATH="/path/to/profile/BepInEx"

dotnet test PlanBuildTest/PlanBuildTest.csproj \
  -p:VALHEIM_INSTALL="/path/to/Valheim" \
  -p:BEPINEX_PATH="/path/to/profile/BepInEx"
```

The mod DLL is `PlanBuild/bin/Release/net48/Buildheim.dll`. Install only the mod DLL, not game assemblies or the entire build directory. Building does not install or publish anything and does not modify the installed game assemblies. `Environment.props` can supply local paths; `BEPINEX_PATH` defaults to the game's `BepInEx` directory.

The client implementation lives in [`PlanBuild/Client`](PlanBuild/Client). Legacy source remains in the repository but is excluded from the runtime build.

Tests cover blueprint parsing, material accounting, layer selection, autobuild scheduling, placement saves, and the game methods used by the hammer and input hooks. They do not run Unity. Runtime validation should include UI layout, modifier shortcuts, obstructed placement, inventory and chest transfers, reconnecting, and a vanilla server with an unmodded observer.

For bug reports, include the mod and Valheim versions, steps to reproduce, and relevant log entries. Include the blueprint file when the issue depends on a particular building.

## Acknowledgments and license

This project builds on [the original PlanBuild](https://github.com/sirskunkalot/PlanBuild), created by MarcoPogo and developed with contributions from Jules, Algorithman, Dreous, and Jere. It uses [Jötunn](https://github.com/Valheim-Modding/Jotunn).

Licensed under the [WTFPL](LICENSE).
