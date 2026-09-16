# Development

## Build

Install .NET SDK 8 and Bun 1.4.1 or newer. Run `bun install --frozen-lockfile`
from the repository root to install the release tooling. Use your Valheim
installation and BepInEx 5 profile:

```sh
dotnet build src/Buildheim/Buildheim.csproj -c Release \
  -p:GameDir="/path/to/Valheim" \
  -p:BepInExDir="/path/to/profile/BepInEx"
```

Output: `src/Buildheim/bin/Release/net48/Buildheim.dll`.

`GameDir` defaults to a standard Steam installation on Linux or Windows.
`BepInExDir` defaults to `GameDir/BepInEx`; set it explicitly for r2modman.
Override `ManagedDir` for a dedicated server or another game data directory.
`Environment.props` is ignored by Git and may store local MSBuild properties:

```xml
<Project>
  <PropertyGroup>
    <GameDir>/path/to/Valheim</GameDir>
    <BepInExDir>/path/to/profile/BepInEx</BepInExDir>
  </PropertyGroup>
</Project>
```

## Check

After building, run the mod's checks with the same references:

```sh
MANAGED_DIR="/path/to/Valheim/valheim_Data/Managed" \
BEPINEX_DIR="/path/to/profile/BepInEx" bash scripts/check.sh
bun run typecheck
bun test tests/
```

The TypeScript package checks and JavaScript version checks use Bun's test runner.

## Package

Add `-t:Package` to the Release build command. The TypeScript script creates and validates
`artifacts/<PackageName>-<Version>.zip`. Import this ZIP with r2modman's
**Import local mod**. Build and package commands do not install or publish anything.

`package/manifest.json` defines the Thunderstore identity and dependencies.
Only the plugin DLL, package assets, README, changelog, and available license
notices enter the ZIP. Game, BepInEx, and NuGet dependency DLLs are excluded.
README image links use raw GitHub URLs pinned to the package version tag.
The repository and that tag must be public for Thunderstore to display images;
bundling images in the ZIP alone does not host them on the mod page.

## GitHub Actions

The identical **Build and publish** workflow in each mod builds on pushes to
`main`, pull requests, manual runs, and `vMAJOR.MINOR.PATCH` tags. It downloads
current public Valheim dedicated-server assemblies using anonymous SteamCMD
(app 896660), and the BepInEx version from `package/manifest.json`.
No Steam credentials or local game files are needed.

The workflow runs `scripts/check.sh`, package tests, and version tests, then
uploads the validated ZIP as `thunderstore-package`. Tag pushes additionally
create a GitHub release and publish that same ZIP with [Thunderstore CLI](https://github.com/thunderstore-io/thunderstore-cli).
The publishing token is passed only to the publish step. Branch pushes, pull requests,
and manual runs without a release tag only build and check.

Repository Actions settings:

- Variable `THUNDERSTORE_NAMESPACE`: `AugusDogus`.
- Secret `TCLI_AUTH_TOKEN`: a service-account access token for that Thunderstore team.

## Release

Keep the existing version until a release is ready. Update `CHANGELOG.md` and
commit your changes first. From a clean checkout, use Node.js 22.18+ or 24.11+:

```sh
npx bumpp@12.3.0 --release patch
# Inspect the generated commit and substitute its tag below.
git push --atomic origin HEAD:main vMAJOR.MINOR.PATCH
```

Use `--release 1.0.0` to tag an unreleased `1.0.0` without incrementing it, or
supply another explicit version. The shared bump config updates the manifest,
project version, plugin constant, and assembly versions together. Existing tags
are never overwritten. Check `git remote -v` before pushing from an old checkout
that also has an upstream remote.

Tags must match the source versions exactly. Prerelease tags are not published.
Creating a release manually on GitHub does not trigger publishing. To retry an
existing version with the current workflow, run **Build and publish** from `main`
and set `release_tag` to its existing tag (for example, `v1.0.0`). This checks out
and validates that tag before publishing; the tag is never moved.
If Thunderstore publishing fails, the ZIP remains on the GitHub release.
Correct credentials or settings and rerun the failed job. If the version is
already published on Thunderstore, release a new version.

## In-game checks

Verify planner layout, modifier shortcuts, obstructed placement, inventory and chest transfers, reconnecting, and a vanilla server with an unmodded observer. Automated tests cover blueprint parsing, materials, layers, scheduling, saves, and the game methods used by input and hammer hooks. They do not run Unity.

The client plugin does not require an asset-bundle build. Editable banner and
icon sources live in `assets/artwork/`.

### Positioning modifier regression (issue #2)

With a blueprint loaded and the planner closed:

1. Hold Ctrl + Alt and scroll one notch. Expect a 22.5° turn.
2. Keep Ctrl + Alt held, add Shift, and scroll. Expect a 90° turn.
3. Release only Shift and scroll. Expect a 22.5° turn.
4. Release all keys, hold Ctrl + Alt again, and scroll. Expect a 22.5° turn.
5. Repeat with each Shift key. With both held, releasing only one should retain
   90° turns; releasing both should restore 22.5° turns.
6. Check movement and capture-box adjustment too: Shift gives 1 m steps, and
   releasing it restores 0.1 m steps, including after releasing all modifiers.
7. Check Ctrl + X sideways movement and that Ctrl/X do not also crouch or sit
   while positioning. Release the modifiers and confirm normal controls return.

These checks require Unity's keyboard input. The .NET tests do not simulate
physical key presses or verify that the game's input backends agree.

If the problem persists, enable `Log rotation input = true` in the `[Diagnostics]`
section of `BepInEx/config/augusdogus.Buildheim.cfg` and restart. Rotate one wheel
notch at each stage above, pausing between stages. Share the lines containing
`Buildheim rotation trace:` from `BepInEx/LogOutput.log`, then disable the setting.
The trace records up to 100 rotation steps per game session: timestamps, frame
numbers, wheel values, angle changes, window focus, and each Shift key's state
from both input APIs. It does not record typed text. This distinguishes a single
90° step from repeated 22.5° steps without guessing which input state is wrong.
