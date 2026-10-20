# Architecture notes — Namekeeper 0.4.0

The synced `Shop.OperatingMode` setting selects `WorldLocations` (default) or `HammerBuildable`. Both legacy prefabs are always registered under their original names so placed shops and their ZDO state remain loadable. `Piece.m_enabled` controls whether the shop and replacement roof appear in Hammer.

Jötunn clones the native `Vendor_BlackForest` location three times. The location entries are prioritized, restricted to Meadows, assigned one placement each, and grouped with a 2,800 m minimum distance. Their center-distance range is 1,000–6,500 m. The native location's no-build and spawn-blocking fields and ForceField object are retained; its camp contents are replaced by the existing Namekeeper shop prefab.

Each world shop reuses `ShopState` and the existing shop/roof network objects. The female merchant remains a local visual child of the shop ZDO. A native Boar prefab is included with `Tameable.m_startsTamed` enabled and a persistent ZNetView. The location's merchant arrives immediately when its zone loads; hammer-built shops retain the configured arrival delay.

Valheim stores location placement in the world save and generates locations once. Registering the custom entries does not retrofit the three shops into an already-generated world. Existing built shops continue to load, and players can use `HammerBuildable` to add a shop in an old world.

`Map.RevealShopLocations` is a local config entry. It adds unsaved Hildir-style map pins only to the local Minimap and removes those pins when disabled. It does not change the server's location list or disclose coordinates to other players.

The legacy piece costs, service fees, text limits, item custom-data keys, prefab IDs, ZDO keys, shop footprint, roof logic, and door RPC remain unchanged.
