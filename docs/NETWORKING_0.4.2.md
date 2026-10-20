# Multiplayer and dedicated-server notes — 0.4.2

## Required versions

Install the same Namekeeper version, BepInExPack Valheim, and Jötunn on the dedicated server and every player. Namekeeper 0.3.8 and newer declares `EveryoneMustHaveMod` compatibility with strict patch-version matching. A 0.3.7 server predates this declaration and cannot reject mismatched Namekeeper clients.

## Server-controlled mode

`Shop.OperatingMode` remains synchronized by Jötunn and controlled by the server. `WorldLocations` is the default and registers three Meadow shops during world generation. `HammerBuildable` disables those generated locations and exposes the shop and replacement roof in Hammer. Restart the server and clients after changing the mode. Valheim does not backfill new locations into an already-generated world.

## Discovering and revealing the shops

Approaching a generated shop uses Valheim's native Haldor-style map discovery and localized notification. An administrator can use `namekeeper_map on` to ask the authoritative server for generated shop coordinates. The server validates the requesting peer against its admin list and sends the result only to that peer. The temporary map pins are created locally and are visible only to the administrator who requested them. `namekeeper_map off` removes those temporary pins; ordinary discovered pins remain.

The command works on dedicated servers without requiring the client to have a local copy of the world's generated location list. If no shop positions have been generated in the world's data, the client reports that instead of silently showing an empty map. The legacy `Map.RevealShopLocations` configuration entry is not used.

## Dedicated server

Headless startup registers prefabs, synchronized settings, world locations, and the map request RPC without constructing the client HUD. The server owns shop and pet ZDOs. Each player creates the animated female merchant locally from shop state; no separate NPC or duplicate ZDO is introduced for her.

## Validation status

The 20 October 2026 server log shows Namekeeper 0.4.1 loading and all three Meadow locations registering. It does not contain a successful placement or failure line for those custom locations, so placement still needs confirmation from a new-world test. Version 0.4.2 changes the map request to use the server's generated-location list; the RPC, authorization, pins, and live server handshake need in-game testing. See [the 0.4.2 Russian test plan](TEST_PLAN_0.4.2.ru.md).
