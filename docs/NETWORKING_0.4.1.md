# Multiplayer and dedicated-server notes — 0.4.1

## Required versions

Install the same Namekeeper version, BepInExPack Valheim, and Jötunn on the dedicated server and every player. Namekeeper 0.3.8 and newer declares `EveryoneMustHaveMod` compatibility with strict patch-version matching. A 0.3.7 server predates this declaration and cannot reject mismatched Namekeeper clients.

## Server-controlled mode

`Shop.OperatingMode` is synchronized by Jötunn and controlled by the server:

- `WorldLocations` is the default. Three Namekeeper locations are added to the Meadows world-generation list, with one location per entry and a 2,800 m minimum distance between group members.
- `HammerBuildable` disables those generated locations and exposes the shop and replacement roof in Hammer.

Restart the server and clients after changing the mode. Use the same mode on every peer. Valheim saves location placement in the world; newly enabled locations do not appear in an already-generated world. Existing Namekeeper shops remain loadable in either mode.

## Map discovery and admin reveal

Each generated location uses Valheim's native discovery fields cloned from Haldor's camp. Approaching it reveals its regular map marker and localized discovery notification to that player.

An administrator can type `namekeeper_map on` in the in-game console to temporarily show all generated shop markers on their own map. `namekeeper_map off` removes those temporary markers. The command checks the calling client's synchronized admin status; it does not send coordinates or markers to other players. The old `Map.RevealShopLocations` config entry is no longer used.

## Dedicated server

Headless startup registers prefabs, synchronized settings, and world locations without constructing the client HUD or registering the client-only map command. The server owns the shop and pet ZDOs. Every player creates the animated female merchant locally from the shop state; no separate NPC or duplicate ZDO is introduced for her.

## Validation

The 0.4.0 server log showed that all three locations registered but none passed world-generation placement. `m_maximumVegetation = 0` rejected ordinary terrain; 0.4.1 removes that restriction. Location placement is generated once per world, so test the fix on a new disposable world. Verify discovery, admin-only map reveal, and networking using [the 0.4.1 test checklist](TEST_PLAN_0.4.1.ru.md).
