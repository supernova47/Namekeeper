# Multiplayer and dedicated-server notes — 0.4.3

## Required versions

Install the same Namekeeper version, BepInExPack Valheim, and Jötunn on the dedicated server and every player. Namekeeper 0.3.8 and newer declares `EveryoneMustHaveMod` compatibility with strict patch-version matching. A 0.3.7 server predates this declaration and cannot reject mismatched Namekeeper clients.

## Server-controlled mode

`Shop.OperatingMode` remains synchronized by Jötunn and controlled by the server. `WorldLocations` is the default and registers three Meadow shops during world generation. `HammerBuildable` disables those generated locations and exposes the shop and replacement roof in Hammer. Restart the server and clients after changing the mode. Valheim does not backfill new locations into an already-generated world.

## Discovering and revealing the shops

Approaching a generated shop uses Valheim's native Haldor-style map discovery and localized notification. An administrator can use `namekeeper_map on` to ask the authoritative server for generated shop coordinates. The server parses the platform-prefixed IDs from its admin list (for example, `V_<SteamID>`) and matches their numeric value to the numeric player ID supplied by the peer. This resolves the 0.4.2 mismatch that denied valid admins. The server sends results only to the authorized peer; temporary pins are local to that player's map. `namekeeper_map off` removes those temporary pins while ordinary discovered pins remain.

The command searches the server's generated location-instance list, which can contain coordinates before the corresponding terrain zone has been loaded or saved to disk. Wait for startup location generation to finish before using the command. If the server then reports zero Namekeeper locations, the current world has no matching generated entries; exploring farther does not create replacement entries in an already-generated world. The legacy `Map.RevealShopLocations` configuration entry is not used.

## Dedicated server

Headless startup registers prefabs, synchronized settings, world locations, and the map request RPC without constructing the client HUD. The server owns shop and pet ZDOs. Each player creates the animated female merchant locally from shop state; no separate NPC or duplicate ZDO is introduced.

## Validation status

The 20 October 2026 server log shows Namekeeper 0.4.2 loading and all three Meadow locations registering. It contains no successful placement or failure line for those custom locations. It also shows admin map requests denied because the peer's numeric ID was compared directly with the platform-prefixed admin-list entry. The current `New world3` save has only its initial world metadata file and no saved zone database, so the saved files do not expose shop coordinates. The denied RPC returned an artificial zero count, and the available log alone cannot establish whether the server's in-memory generated-location list contains the shops. See [the 0.4.3 Russian test plan](TEST_PLAN_0.4.3.ru.md) for the live check.
