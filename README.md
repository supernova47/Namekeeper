# Valheim Namekeeper

**Author:** SuperNova  
**Version:** 0.4.8

Licensed under the [MIT License](LICENSE).

Valheim Namekeeper adds a merchant who can rename equipment and edit or reset item descriptions for coins. Choose between three hidden Meadows shops in newly generated worlds or the classic hammer-built workshop.

Source code and releases: [GitHub](https://github.com/supernova47/Namekeeper). Report bugs or suggest features through [GitHub Issues](https://github.com/supernova47/Namekeeper/issues).

## Features

- Rename an eligible item or edit its description for 25 Coins per changed field. Resetting a field has the same price.
- In the default **WorldLocations** mode, find three Namekeeper shops in the Meadows of a newly generated world, 800–3500 metres from the centre and at least 1500 metres apart. New camps prefer gentle terrain and level their own clearing. Existing camp positions and terrain are preserved.
- Each camp has a Haldor-style force field, rune stones, a supply canopy, barrels, crates, a woodpile, and a fire made from vanilla game objects. Sit on the bench using the interaction key; a nearby table holds decorative bread, cooked meat, and a tankard. Food and supplies cannot be looted.
- Meet Inkblot and Snout: two stationary animated boars sharing Hildir's vanilla bowl of turnips and greens, with its original food arrangement and proportions. Interact to pet them and hear a response. They are camp residents with no player ownership, following, taming, or renaming.
- The camp clears grass, blocks monster spawning, prevents attacks and guardian powers inside its dome, and protects the shop, roof, and boar from damage.
- Discover each shop by approaching it: Valheim adds its Haldor-style map marker. Within 150 metres, a discovery banner and map notification appear once per character, world, and camp, after the native marker is available. Administrators can type `namekeeper_map on` in the in-game console to request all three locations from the server and reveal them on their own map, and `namekeeper_map off` to remove those temporary markers.
- In **HammerBuildable** mode, build the current shop after discovering its recipe materials: 20 Wood, 10 Fine Wood, 10 Round Log, 10 Stone, and 100 Coins. The first roof is included; a replacement roof costs 4 Wood.
- The buildable shop keeps its working door, vanilla furnishings, shelves, wall-mounted weapons, repair, and dismantling behavior.
- The female merchant, animations, Russian/English dialogue, and Hildir voice clips are retained in both modes.
- Hear merchant dialogue in Russian when the game language is Russian; other game languages use English dialogue. Hildir's vanilla voice clips provide the sound.
- Shop interface text and the handwritten sign follow each client's game language: Russian for Russian and English for other languages. English and Russian local overrides remain available.
- Keep Namekeeper text in item data without replacing or deleting Nexus metadata. A paid reset restores the original Valheim name or description.
- Configure service prices, construction materials, merchant arrival delay, roof requirements, and text limits on the server.

## Multiplayer and dedicated servers

Install Namekeeper 0.4.8, BepInExPack Valheim, and Jötunn on the dedicated server and every player. Namekeeper 0.3.8 and newer enforce matching mod versions. The server controls the operating mode and gameplay settings; the admin map command only changes the caller's map. Update every server and client together.

The shop, roof, door, and personalized item text use Valheim's networked objects and item data. The merchant model, dialogue, voice, and visual effects are local presentation features. Camp boars use the shared world clock for eating and a cosmetic RPC for pet reactions; they create no animal ownership or breeding state.

## Installation

Install Valheim Namekeeper from Thunderstore using a compatible mod manager. The manifest lists BepInExPack Valheim and Jötunn as dependencies. For a dedicated server, install the same mod version and dependencies in the server's BepInEx profile.

For local testing, download the [Thunderstore package archive](https://github.com/supernova47/Namekeeper/tree/main/release-assets) and import it into your Valheim profile.

See the complete [changelog](https://github.com/supernova47/Namekeeper/blob/main/changelog.md) for release history.

## Configuration

Server-controlled settings include the operating mode, service fees, the shop recipe, merchant arrival delay, the roof requirement, and maximum text lengths. Interface language follows the local game language by default; English and Russian overrides, dialogue, voice volume, and success effects are local settings. The `namekeeper_map on|off` console command is available to administrators for a temporary map reveal on their own client. Restart the server and clients after changing the operating mode.

The default operating mode is `WorldLocations`. Valheim saves location placement when a world is generated, so the three shops appear only in newly generated worlds. Existing worlds keep their placed Namekeeper shops; switch to `HammerBuildable` if you want to build another shop in an existing world. The admin command adds temporary markers to the command user's local map.

Other defaults: 25 Coins per changed field; buildable shop recipe of 20 Wood, 10 Fine Wood, 10 Round Log, 10 Stone, and 100 Coins; replacement roof for 4 Wood.

## Credits and licenses

Created by **SuperNova**. The mod is licensed under the MIT License; see LICENSE. The handwritten sign font is licensed under SIL OFL 1.1; see FONT_LICENSE.txt.


