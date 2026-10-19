# Namekeeper 0.1.3 — acceptance and regression test plan

None of the following checks has been executed yet; this is a reproducible checklist.

## A. Build checks

- [ ] `dotnet build` with current game/BepInEx/Jötunn DLLs, no compile errors
- [ ] Load Valheim, observe plugin startup log, no Harmony patch errors
- [ ] Verify the hammer Crafting category shows Namekeeper's Shop and its requirements
- [ ] Build near a workbench; verify placement prevented without a workbench
- [ ] Confirm full shop collision and player access to counter

## B. Arrivals and shelter

- [ ] After building, trader is absent for exactly configured delay
- [ ] Timer begins without requiring additional building cover
- [ ] Save, quit, reload before arrival; remaining wait is preserved
- [ ] Save, quit, reload after arrival; exactly one trader visual
- [ ] Place several shops; independent timers, no duplicated NPCs
- [ ] Service accepts E when roof check passes; rejected when no roof
- [ ] Separate roof removal/rebuild while shop survives (NOT YET SUPPORTED)
- [ ] Remove entire shop with hammer; merchant disappears, no orphan visuals

## C. Personalization

- [ ] Select several stack-size-1 items from inventory list without moving them
- [ ] Stackable resources do not appear
- [ ] Name only changes for 50 coins; description only for 50; both for 100
- [ ] No-op edit does not charge; insufficient coin balance rejects
- [ ] Reset name and/or description costs the same as changing it
- [ ] Empty name, rich-text tags, control characters and excess lengths are rejected
- [ ] Custom text is displayed in inventory tooltip and dropped item name
- [ ] Save/reload, equip/unequip, repair, upgrade, chest and item transfer preserve custom text
- [ ] Other customData fields, durability, quality, magic modifiers and variant remain intact
- [ ] Immediate double click does not cause duplicate charges
- [ ] Press Esc and leave merchant area; close UI and restore controls

## D. Multiplayer / dedicated server

- [ ] Install matching plugin on host and 2 clients; all see one trader per shop
- [ ] Joined players can edit their own items at other players' shops
- [ ] Disconnect/reconnect while waiting, and after arrival
- [ ] Dedicated-server headless load registers piece without shader/NPC issues
- [ ] Confirm no duplicate traders after server restart
- [ ] Client price manipulation is rejected by server (NOT YET IMPLEMENTED)
- [ ] Race/simultaneous transactions on same client produce one payment each

## E. Compatibility

- [ ] Clean BepInEx + Jötunn profile, then with Valheim Nexus
- [ ] Icon availability and Localization translation on English and Russian game clients
- [ ] Epic Loot / item custom data coexistence
- [ ] UI scaling at 1920x1080, 2560x1440, low resolution, controller input
- [ ] World backup restore behaves correctly after uninstalling plugin

## 0.1.3 — regression cases from screenshot

- [ ] Actual tooltip header shows custom name and does not also show vanilla name.
- [ ] Tooltip body starts with custom description, not with duplicated custom name.
- [ ] Removing custom name restores original localized title; description can be reset separately.
- [ ] Mouse look/camera zoom/hotkeys do not react to input while HUD is open.
- [ ] Mouse wheel scrolls the item list and does not zoom camera.
- [ ] Keyboard input into both fields is not consumed by game controls.
- [ ] Escape, close button and walking out of range restore camera and player input.
- [ ] HUD looks cohesive at low screen resolution (min ~1024x768).


## 0.1.3 — user-reported inventory heading regression

1. Load a previously customized axe (or any eligible single-stack item), with distinct custom name and description; hover in inventory: custom name is the only header, custom description is unchanged.
2. Drag into another inventory slot and across container UI: header follows the correct instance.
3. Keep two identical original items with different custom names: each has its own tooltip title.
4. Drop and pick up: title is correct both in world and inventory.
5. Reset only name: original title returns, existing custom description stays.
6. Verify no Namekeeper warnings during inventory update and that other mods' items retain their normal tooltip headers.
