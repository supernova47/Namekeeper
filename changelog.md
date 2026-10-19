# Changelog

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



