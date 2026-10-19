# Network architecture — Namekeeper 0.3.8

The plugin declares Jötunn network compatibility as `EveryoneMustHaveMod` with `Patch` version strictness. Custom shop and roof prefabs, RPC names, ZDO keys, inventory text keys, and migration rules therefore have one known implementation on the server and connected clients.

Gameplay-affecting configuration entries are bound with Jötunn's synced-config flag. Jötunn applies server values to clients and locks those settings for non-admin players. A synchronization callback rebuilds both Hammer requirement arrays from the now-authoritative values. Local presentation choices are not synchronized.

`ShopState` remains the owner of merchant arrival state. `ShopRoofAssembly` keeps the roof migration and link in the shop ZDO. `WorkshopDoor` sends state changes to the object's ZDO owner, which writes the shared open/closed value. Only client peers create the merchant presentation and native HUD; the custom shop and roof prefabs still register on headless servers.

Personalized names and descriptions remain item custom-data values. The existing inventory transaction debits coins and applies both requested fields together before notifying Valheim's inventory system. Live client/server replication must still be checked using the multiplayer test plan before this release is marked verified.
