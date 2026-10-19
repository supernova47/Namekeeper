# Valheim Namekeeper

**Author:** SuperNova  
**Version:** 0.3.10

Valheim Namekeeper adds a buildable merchant workshop where players can rename equipment and edit or reset item descriptions for coins. The shop uses vanilla Valheim building pieces, has a separate replacement roof and working door, and features a local animated merchant with voiced dialogue.

Source code and releases: [GitHub](https://github.com/supernova47/Namekeeper). Report bugs or suggest features through [GitHub Issues](https://github.com/supernova47/Namekeeper/issues).

## Features

- Rename an eligible item or edit its description for 25 Coins per changed field. Resetting a field has the same price.
- Build the shop after discovering its recipe materials: 20 Wood, 10 Fine Wood, 10 Round Log, 10 Stone, and 100 Coins. The first roof is included.
- Build a replacement roof separately for 4 Wood. Repair and dismantle the shop and roof using Valheim's standard building actions.
- Use the working shop door, vanilla furnishings, shelves, wall-mounted weapons, and the full-size merchant model.
- Hear merchant dialogue in Russian when the game language is Russian; other game languages use English dialogue. Hildir's vanilla voice clips provide the sound.
- Keep Namekeeper text in item data without replacing or deleting Nexus metadata. A paid reset restores the original Valheim name or description.
- Configure service prices, construction materials, merchant arrival delay, roof requirements, and text limits on the server.

## Multiplayer and dedicated servers

Install Namekeeper 0.3.10, BepInExPack Valheim, and Jötunn on the dedicated server and every player. Namekeeper 0.3.8 and newer enforce exact mod version matching and synchronize gameplay settings from the server. The 0.3.7 server predates version enforcement and can accept a client with a different version, so update every server and client together.

The shop, roof, door, and personalized item text use Valheim's networked objects and item data. The merchant model, dialogue, voice, and visual effects are local presentation features.

## Installation

Install Valheim Namekeeper from Thunderstore using a compatible mod manager. The manifest lists BepInExPack Valheim and Jötunn as dependencies. For a dedicated server, install the same mod version and dependencies in the server's BepInEx profile.

For local testing, download the [Thunderstore package archive](https://github.com/supernova47/Namekeeper/tree/main/release-assets) and import it into your Valheim profile.

See the complete [changelog](https://github.com/supernova47/Namekeeper/blob/main/changelog.md) for release history.

## Configuration

Server-controlled settings include service fees, the shop recipe, merchant arrival delay, the roof requirement, and maximum text lengths. Interface language, dialogue, voice volume, and success effects are local settings.

Defaults: 25 Coins per changed field; shop recipe of 20 Wood, 10 Fine Wood, 10 Round Log, 10 Stone, and 100 Coins; replacement roof for 4 Wood.

## Build and test

Build on Windows with Valheim, BepInEx, Jötunn, and the .NET SDK installed by running BuildMod.ps1. The build uses the game assemblies installed on that computer and does not install the mod into a live profile.

Run the game-independent regression suite with:

    dotnet run --project tests/Namekeeper.Tests.csproj --configuration Release

The 0.3.10 release passed 56 regression tests, 14 compiled IL checks, 24 native prefab checks, and package integrity checks. A live in-game session and dedicated-server version-mismatch test are still required for final multiplayer acceptance. See docs/TEST_PLAN_0.3.10.md for the manual test checklist.

## Credits and licenses

Created by **SuperNova**. The handwritten sign font's OFL license is included in FONT_LICENSE.txt.


