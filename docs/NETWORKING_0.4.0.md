# Multiplayer and dedicated-server notes — 0.4.0

## Required versions

Install the same Namekeeper version, BepInExPack Valheim, and Jötunn on the dedicated server and every player. Namekeeper 0.3.8 and newer declares `EveryoneMustHaveMod` compatibility and checks matching versions during connection. A 0.3.7 server predates this declaration and cannot reject mismatched Namekeeper clients.

## Server-controlled mode

`Shop.OperatingMode` is synchronized by Jötunn and is controlled by the server:

- `WorldLocations` is the default. Three Namekeeper locations are added to the Meadows world-generation list, with one location per entry and a 2,800 m minimum distance between group members.
- `HammerBuildable` disables the three generated locations and exposes the shop and replacement roof in Hammer.

The server and clients must restart after changing the mode. Use the same mode on every peer. Valheim saves location placement in the world; newly enabled locations do not appear in an already-generated world. Existing Namekeeper shops remain loadable in either mode.

`Map.RevealShopLocations` is local to each client. A player who enables it sees temporary map markers for the three generated locations; other players' map settings are unaffected.

Service fees, building costs, merchant delay, roof requirement, and text limits remain server-synchronized. The merchant model, animations, text, audio, and local map pins are presentation features on each client. The world shop and boar are persistent network objects.

## Dedicated server

Headless startup registers prefabs, settings, and world locations without constructing the client HUD or adding map pins. The server owns the shop and pet ZDOs. Every player creates the animated female merchant locally from the shop state; no separate NPC or duplicate ZDO is introduced for her.

## Validation

Source checks cannot prove that all three locations fit every seed, that the dome looks correct in-game, or that every network peer receives the mode before world generation. Verify on a disposable new world and a dedicated server using [the 0.4.0 test checklist](TEST_PLAN_0.4.0.ru.md).

Jötunn documents that locations are generated once per world, unpacked as players approach, and persist only GameObjects whose `ZNetView.m_persistent` flag is true: [Zones and locations](https://valheim-modding.github.io/Jotunn/tutorials/zones.html).
