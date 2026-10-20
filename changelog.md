# Changelog

## 0.4.8 — Hildir's original feeding bowl

- Replaced the floating carrot display with the complete vanilla bowl and turnip arrangement from Hildir's camp. Bowl and food retain their authored positions, rotations, and scales in one common frame.
- Removed procedural carrot packing and its temporary collision queries. Only the complete meal is moved and rotated; food pieces receive no individual height adjustment, scaling, or physics.
- Grounded the composition by the bowl's bottom and moved the two boars slightly closer together to match the native bowl's width. Names, pet interaction, facing, and animations are retained.
- Preserved the approved rest area, discovery messages, grass clearing, protection, terrain, saved camp locations, merchant behavior, services, prices, and both operating modes.
- Use one shared Thunderstore/r2modman archive with `author: SuperNova` in its manifest. No separate platform variants are created; the developer source snapshot is kept separately.
- Existing worlds can use the update after restarting. Update server and clients together. Native-asset geometry checks and compilation do not replace the final in-game visual check.

## 0.4.7 — camp seating, food displays, and discovery notices

- Removed the deer-hide rug from generated camps. Added the approved rest area: an interactive vanilla bench, a full-size wooden table with decorative bread, cooked meat, and a tankard, and a fire moved slightly toward the camp centre.
- Restored all three native bench seats and their standard interaction, attachment animation, and occupancy checks. Sitting uses the player's existing networked animation; the camp furniture has no loot or dismantling behavior.
- Enlarged the shared bowl and its carrots. Six carrots are arranged in two layers and lowered against the actual bowl and food contact geometry. Temporary collision-query shapes are removed after placement, leaving a static decorative display.
- Added a localized discovery banner and map notice within 150 metres of a generated shop once its native location marker exists. Each character remembers each camp separately for each world, including across save/reload. Distant administrator map reveals do not trigger discovery notices.
- Removed the generic camp-boundary discovery label to prevent a second banner when entering the dome. Native map icons and the administrator command are retained.
- Kept existing terrain, camp positions, boars, merchant animations, protection, recipes, prices, saved identities, and both operating modes. Existing worlds can use this update; update the server and clients together.
- Compilation and automated checks do not replace the in-game seating, carrot-contact, and discovery acceptance checklist.

## 0.4.6 — camp grass and missing boars

- Fixed the food-display exception that stopped camp initialization before the boars were created. Carrots now use their authored `attach` visual, preserving native size and excluding the unrelated equipment-offset model and prefab LOD references.
- Isolated optional food/decor failures from animal initialization. Missing boars retry independently without duplicating successful animals; failed visual clones are cleaned up.
- Added a direct exclusion in Valheim's procedural clutter generator for points inside each loaded camp. This removes grass, ferns and other ground clutter regardless of terrain-paint rules. Existing patches are refreshed when the camp loads.
- Replaced the two small bowls with one large shared oval bowl containing seven full-size carrots. Food placement uses camp-local mesh bounds, so world rotation does not change its fit.
- Preserved boar names, facing, animations, pet interaction, camp positions, existing terrain, admin map authorization, services, recipes and prices. Update both server and clients; an existing world can be used for verification.
- Server/client logs from the user's 0.4.5 test confirmed successful administrator authorization and three returned locations. The grass/animal fixes in 0.4.6 still require in-game acceptance.

## 0.4.5 — inhabited camps and authenticated map requests

- Fixed the dedicated-server rejection of `namekeeper_map on`: requests now travel over the authenticated connection and use Valheim's native socket-based administrator check. Earlier builds incorrectly compared the character ID with the administrator's platform account ID.
- Accept map replies only from the current server connection and current request. Clear temporary positions on reconnect, ignore stale replies, and report a request timeout.
- Fixed a loading-screen exception caused by reading the world UID before the world existed.
- Attached grass clearing, no-build bounds, monster exclusion, and sanctuary protection to the persistent shop instead of relying on a temporary location root. Ground clutter is refreshed after terrain processing.
- New-world locations now target Meadows 800–3500 metres from the centre, with at least 1500 metres between shops and a gentler terrain limit. Only newly generated camps receive leveling and edge smoothing; existing camps keep their terrain and coordinates.
- Added the approved camp layout: vanilla supply canopy, barrels, crates, a woodpile, a bench, a deer-hide rug, a fire, and eight perimeter trader rune stones. Objects retain their full size and are grounded after terrain rebuilding. Decorations provide no loot.
- Replaced the old tamed animal with two stationary animated camp boars, Inkblot and Snout (Клякса and Пятачок in Russian). They face the same direction as the merchant, eat from carrot bowls, and respond to petting with an animation and native audio. They cannot be owned, followed, renamed, bred, or looted.
- Retained the old boar prefab identity for migration and removed its saved instances without drops. Ordinary player animals are unaffected.
- Kept both operating modes, shop services, prices, recipes, existing shop/roof identifiers, merchant gestures, per-client localization, and item metadata behavior.
- This candidate still requires the Russian in-game checklist, including a dedicated-server admin request and existing/new world checks, before stable release.

## 0.4.4 — protected, localized Meadow shops

- Added a Haldor-style safe area around each generated shop: monsters do not spawn there, player attacks and guardian powers are blocked, and the shop, roof, boar, and other non-player targets cannot be damaged.
- Clears ground clutter and vegetation across the camp using Valheim's native terrain modifier.
- Fixed automatic shop map markers by reusing Haldor's native location icon; the existing admin-only `namekeeper_map on|off` command remains available for temporary personal map pins.
- Made the shop interface follow each client's game language by default. Russian uses the Russian interface and sign; every other game language uses English. Manual local overrides remain available.
- Kept the tamed boar beside the shop and constrained its movement while retaining native AI and idle animation.
- Preserved both operating modes, world and building identifiers, recipes, prices, item data, and existing shop saves. World-spawned locations are still generated only when a new world is created.
- This build needs in-game multiplayer checks for map discovery, safe-zone boundaries, terrain clearing, combat restrictions, boar behavior, and per-client localization.

## 0.4.3 — dedicated-server admin ID matching

- Fixed `namekeeper_map on` authorization when Valheim stores the administrator as a platform-formatted ID such as `V_<SteamID>` while the network peer exposes the numeric ID.
- The server now parses platform-prefixed admin-list entries and matches their numeric user ID to the requesting peer before returning generated shop coordinates.
- Kept coordinates restricted to the authorized caller and map pins local to that player's client.
- Preserved shop placement, discovery, recipes, prices, services, and saved data.

## 0.4.2 — dedicated-server map reveal fix

- Fixed `namekeeper_map on` on dedicated servers: the client now requests the generated shop coordinates from the authoritative server instead of searching its own location list.
- The server validates the requesting peer against its admin list and sends the positions only to that administrator. The temporary pins remain local to the caller.
- Added immediate status feedback, including when no locations exist in the world's generated data yet; `off` still removes temporary pins.
- Preserved Haldor-style discovery, world locations, hammer-built mode, item services, and saved data.

## 0.4.1 — world placement and discovery fixes

- Fixed Meadows shop placement: a zero maximum-vegetation setting rejected normal terrain, preventing all three locations from generating on the tested seed.
- Kept Haldor-style discovery behavior so approaching a generated shop reveals its map marker and displays the localized discovery message.
- Replaced the local `RevealShopLocations` config option with the admin-only `namekeeper_map on|off` console command. Temporary reveal markers are visible only to the administrator who runs the command.
- Preserved the default `WorldLocations` mode, the hammer-built alternative, shop services, recipes, and saved data identifiers.
- Valheim generates locations once per world. Create a new world to validate the placement fix; an existing world whose generation already failed will not be backfilled.

## 0.4.0 — world-spawned merchant shops

- Added a server-synchronized operating mode, with three Meadows merchant locations enabled by default and the hammer-built shop available as an alternative.
- Cloned Haldor's native trader camp location settings and force-field visuals, then replaced the camp contents with Namekeeper's existing shop and female merchant.
- Added a persistent, tame boar beside each world-spawned shop.
- Added an optional local map setting that reveals all three shop locations with temporary map markers.
- Added an MIT license file to the mod package and kept developer build instructions out of the packaged README.
- Existing worlds retain their placed shops. Valheim's one-time world generation means the new locations are created in newly generated worlds; use HammerBuildable mode to add a shop to an existing world.
- Preserved service behavior, prices, item data, the separate replacement roof, shop and roof prefab IDs, and existing ZDO keys.
- Requires in-game testing of location placement, the force field, boar taming/persistence, map markers, and multiplayer synchronization before a stable release.

## 0.3.11 — Thunderstore version update

- Bumped the manifest, plugin, and assembly version to 0.3.11 so the package can be uploaded as a new Thunderstore release.
- No gameplay behavior changed from 0.3.10.

## 0.3.10 — Thunderstore release update

- Bumped the manifest, plugin, and assembly version to 0.3.10.
- Updated the English README, Thunderstore project URL, and release archives.
- No gameplay behavior changed from 0.3.9.
## 0.3.9 — resource icons and network versions

- Fixed recipe refresh after settings synchronize: material amounts change while vanilla item references remain intact. This prevents every material from displaying the same shield icon.
- Confirmed mixed-version behavior: a 0.3.7 server does not check mod versions because it predates Jötunn's network compatibility attribute. Version enforcement works when the server is updated to 0.3.8 or newer.
- Preserved strict version matching, server settings, and the other 0.3.8 features.

## 0.3.8 — server compatibility

- Namekeeper now requires the same mod version on the server and every player.
- The server synchronizes gameplay settings: service prices, shop materials, merchant arrival delay, roof requirement, and text limits.
- Shop and separate roof recipes refresh on clients after server settings are applied.
- The dedicated server no longer creates the client interface; the merchant model and visual effects remain client-side.
- Prepared the Thunderstore README, manifest, icon, and a flat package archive.

## 0.3.7 — restoring the merchant through the native animation transition

- Removed the forced return to a cached Animator state: a state number does not guarantee an idle pose and could leave the merchant lying down.
- Gestures still start smoothly; afterward, the Animator's native one-shot transition returns the merchant to idle.
- Preserved the position, model, dialogue, Hildir voice, shop, prices, recipes, and saved identifiers from 0.3.6.
- This candidate requires in-game verification of every gesture.

## 0.3.6 — smooth merchant animation transitions

- Fixed the Animator reset after a gesture. It returned the female model to an incorrect pose, making the merchant lie down and then stand up.
- The idle pose is captured before the first gesture; transitions into and out of gestures use a smooth crossfade without resetting the controller.
- Preserved dialogue, Hildir voice, merchant position, shop, prices, recipes, and saved identifiers from 0.3.5.
- This candidate requires in-game verification of every gesture and the return to idle.

## 0.3.5 — merchant position and idle pose

- Lowered the female model's root point to the shop floor.
- Moved the merchant away from the counter so her body and dress no longer intersect the countertop.
- Set the idle animation to the player's calm native stance with arms down; removed the periodic shoulder shrug.
- Returned the Animator to its base idle pose after each temporary gesture, even when a game animation left it on its last frame.
- Preserved the model, voice, dialogue, furniture, recipes, prices, and saved identifiers from 0.3.4.
- This candidate requires in-game verification of the model placement and animations.

## 0.3.4 — wall mounts and female merchant

- Moved the shelves against the rear wall and connected them to it with vanilla wooden braces.
- Kept full-size items within the shelf boundaries and in front of the inner wall plane; mounted the shield and sword close to the wall.
- Replaced Hildir with a visual model of the Valheim female player character, with long dark hair and an undyed dress.
- Added player gestures for greeting, selection/editing, waiting, success, refusal, and farewell.
- Preserved the dialogue text and Hildir voice clips; shop functionality and saved identifiers remain unchanged.
- This candidate requires in-game verification of the model, mounts, and gestures.

## 0.3.3 — Black Forest unlock and service prices

- Shop construction unlocks after collecting each required resource once: 20 Wood, 10 Fine Wood, 10 Core Wood, 10 Stone, and 100 Coins. This places the materials at the Black Forest progression stage.
- Reduced the fee for changing or independently resetting a name or description from 50 to 25 Coins per field. Existing default fees of 50 migrate to the new price on the first launch of this version.
- Moved the shop and separate roof to the Hammer's `Misc` category. The roof remains a separate piece unlocked by its own recipe of 4 Wood.
- Uses native `Piece` requirements: the game unlocks each build piece after the player discovers all its resources and displays the standard localized unlock toast and icon. No duplicate mod notification was added.
- Preserved shop and roof identifiers, saves, roof cost, model, and other 0.3.2 features. This build requires in-game verification of recipes, unlocks, and notifications.

## 0.3.2 — precise decor placement, bed, and wall weapons

- Accepted by the user as a stable working version after in-game testing (October 18, 2026).
- Moved the shelves away from the rear wall; full-size vanilla decor now rests on the shelf surfaces according to the actual mesh bounds.
- Restored amber, pearl, ruby, coin, trophy, and chest models to their original vanilla prefab scale. Moved the chest to the counter.
- Added a standard-size vanilla `bed` in place of the reduced bed model.
- Added a wooden shield and bronze sword to the wall; moved the merchant slightly to make room beside the bed.
- Preserved building identifiers, door and roof saves, prices, previous features, and the shop footprint.

## 0.3.1 — door and vanilla shop decor

- Added a working native door leaf in the rear wall. Its state is saved in the shop ZDO and synchronized between players; it uses the vanilla wooden door sound effects.
- Added vanilla amber, rubies, coins, pearls, a chest, and a deer trophy to the three shelves. Replaced the interior workbench with a bed.
- Shortened the lower supports while keeping the floor height. Moved the lower snap points to the new bases; all other snap points and their names remain unchanged.
- Replaced the thatched roof with a richer darkwood roof finish; preserved its cost, repair, dismantling, and initial included-roof behavior.
- Preserved the existing HUD, signs, lights, build sounds, merchant and arrival timer, payments, Nexus integration, prefab/GUID names, and other saved features.
- Compiled against local game DLLs. The appearance, door interaction, and synchronization still require in-game verification; this version is a candidate.

## 0.3.0 — ready-made shop built from Valheim pieces

- Built the body from vanilla floors, walls, beams, a sign, and furniture. The gabled thatch roof and hanging lanterns use vanilla models; the counter, open sides, shelves, and work area remain.
- Copies native geometry without game scripts, network objects, or item drops; the body pieces are repaired and dismantled together. The separate roof and its 4 Wood cost are preserved.
- Fixed the confirmed 0.2.2 magenta sign reported in the log: the material is copied from a real sign and the text is rendered on a copy of its wood. Unavailable `Shader.Find` calls and duplicate `MeshFilter` additions after cloning were removed.
- If sign setup fails, its renderer is disabled without repeated log spam. If a native model cannot be found, registration continues with the previous appearance.
- Preserved existing shop position and snap points, merchant, GUID/prefab/ZDO, HUD scaling, prices, Nexus/reset behavior, dialogue, and voice. Models were fitted to the existing footprint.
- Ran a real compilation, 54 tests, and 10 DLL checks. Appearance and physics still require in-game verification. See the Russian `TEST_PLAN_0.3.0.md` checklist.

## 0.2.2 — omnidirectional light, handwritten sign, and placement

- Strengthened the omnidirectional Point Light and moved its source to the center of the lantern insert; enabled Force Pixel. When available, settings are based on the light from `Lantern_DN`; otherwise, a fallback is used.
- Made the glowing insert visible from all sides and prevented its own shadow from blocking the light.
- Replaced bright UI text with nearly black handwritten Marck Script lettering embedded as images. The world material checks depth, writes to the depth buffer, and culls the back face; emission is disabled.
- Lightened the sign wood for better contrast. Cyrillic and Latin text no longer depend on a system-installed font; the OFL license is included.
- Disabled `clipGround` and enabled `noClipping` and `notOnTiltingSurface` for the shop. Added five contact points at the bottoms of the legs without moving existing objects or the origin.
- Included the PNG/TTF assets and image generator in the source ZIP; extended build checks for resources, sign depth, light type, and placement flags.
- Preserved the HUD, payments, Nexus/reset, voices, registration, and separate roof. In-game assessment of lighting, occlusion, and placement is still required.

## 0.2.1 — building registration fix

- Fixed the confirmed 0.2.0 log error: Jötunn rejected both build pieces because their icons were assigned after `AddPiece`.
- Creates the icon before validation and saves the final value in `PieceConfig`. The native image remains a fallback when graphics are unavailable.
- Checks the return value from `AddPiece`; registers the main shop before the new roof.
- Removed invalid hyphens from snap-point localization keys.
- Added a required IL check of the built DLL for icon assignment order, config persistence, registration failure handling, and shop-before-roof registration. The check detects the 0.2.0 DLL defect.
- Preserved the model, roof, building behavior, HUD, dialogue, and item rules. This fix does not automatically restore removed or saved ZDOs.

## 0.2.0 — workshop, construction, and separate roof

- The user-approved stable 0.1.6 baseline, its DLL, and its archives were preserved.
- Added the native wooden build effect, model-specific icons, and direct-child snap points for standard Hammer snap-point switching.
- Updated the workshop with finished wood edges, braces, fittings, a localized sign, shelves, tools, writing supplies, and warm shadowless lanterns.
- Removed the default Jötunn primitive that interfered with collisions; preserved the existing `SN_NamekeeperShop`, footprint, and merchant position.
- Made `SN_NamekeeperRoof` a separate networked `WearNTear` build piece with dismantling, damage, repair, and replacement for 4 Wood. The first roof is included with the shop; dismantling it grants no extra resources.
- Allows the ZDO owner to create a roof exactly once for an old or new shop. Migration state is preserved and protected against duplicate creation after a failure; unloading the area does not remove the roof.
- Uses native `Cover.IsUnderRoof` for shelter checks; the frame is leaky. The NPC remains when the roof is absent, but services are blocked by `RequireRoof`; an external roof also qualifies.
- Preserved the HUD, scaling, payments, item data, Nexus/reset, and 0.1.6 dialogue/voice. Payment synchronization was outside this phase.
- Uses installed Jötunn 2.30.2, the verified minimum dependency. No new libraries or game assets are included in the archives.
- Added snap-point, fault-injected migration, and refund-isolation checks. In-game acceptance of 0.2.0 is still pending.

## 0.1.6 — proportional HUD scaling

- Fixed the HUD occupying an increasing share of the screen as resolution is lowered.
- Scaled the complete 940×760 panel and left inset from a 1920×1080 reference canvas; preserved the internal layout and prevented enlargement above the reference size.
- Added regression checks for screen share, proportional 720p size, repeated resolution changes, and unusual aspect ratios.
- Preserved all other 0.1.5 behavior, including merchant text and voice, item data, Nexus resets, pricing, and input handling.
- Kept the Russian checklist and added focused resolution-change checks. Live confirmation of the new scaling remains pending.

## 0.1.5 — native HUD and merchant dialogue

- Replaced the active IMGUI shop window with uGUI controls styled with Jötunn's native wood, fonts, buttons, and input resources.
- Made the equipment list and editor scroll independently; kept the price, feedback, and payment controls at the bottom and preserved the 0.1.4 transaction and text rules.
- Added contextual overhead dialogue through the vanilla `Chat.SetNpcText` path: approach, trade actions, resets, outcomes, failures, farewell, and occasional idle chatter.
- Selects Russian only when the **game** language is Russian; all other game languages use English, regardless of the shop UI language setting.
- Plays local spatial Hildir voice clips referenced by her native Trader effects, without copying audio files or spawning a second NPC.
- Added dialogue/voice toggles and source-volume settings; limits repeated lines and prioritizes important outcomes.
- Preserved the working 0.1.4 baseline, existing configs, saved shops, item data, Nexus compatibility, and prices.
- Ran 37 executable isolated regressions and compiled against installed game libraries. Live rendering, audio, input, and multiplayer acceptance remain pending.

## 0.1.4 — tooltip and local transaction fixes

- Refreshes the current per-item inventory title through `UITooltip.Set`; removes duplicate grid scans.
- Runs Namekeeper display patches after Nexus's existing display hooks.
- Adds explicit, independent paid vanilla resets; preserves Nexus and other `customData` keys.
- Keeps original formatted/long descriptions intact during reset; validates user edits before charging.
- Stages the coin debit and item text together, restores exact instances on pre-commit failure, and notifies inventory once afterward.
- Supports `Changed(bool,bool)`; isolates notification/effect failures from transaction failures.
- Adds 24 executable isolated regression/fault-injection tests and a focused manual acceptance plan.
- Preserves GUID, prefab, old metadata, shop/NPC behavior, tariffs/config, and EN/RU.

Compiled against local game dependencies. In-game, multiplayer, and dedicated-server validation pending.



