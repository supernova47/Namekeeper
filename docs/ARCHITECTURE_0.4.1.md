# Architecture notes — Namekeeper 0.4.1

The synchronized `Shop.OperatingMode` setting selects `WorldLocations` (default) or `HammerBuildable`. Both legacy prefabs remain registered under their original names so placed shops and their ZDO state stay loadable. `Piece.m_enabled` controls whether the shop and replacement roof appear in Hammer.

Jötunn clones the native `Vendor_BlackForest` location three times. Each entry targets the Meadows and has one placement. They share a 2,800 m minimum distance and use a 1,000–6,500 m center-distance range. The maximum vegetation filter is set to 1 (unrestricted); a value of 0 rejects normal terrain and caused all three locations to fail placement in the 0.4.0 server log. Native Haldor-style discovery flags are retained: the icon is hidden until the player discovers the location, and `$sn_namekeeper_shop` supplies the localized discovery message.

The native force field, no-build protection, and spawn-blocking settings are retained. The camp contents are replaced by Namekeeper's existing shop. Each world shop reuses `ShopState` and the existing network objects. The female merchant remains a local visual child of the shop ZDO. A native Boar prefab is included with `Tameable.m_startsTamed` enabled and a persistent ZNetView. The location merchant arrives as soon as its zone loads; hammer-built shops retain the configured arrival delay.

`namekeeper_map on|off` is a local client console command. It is registered only on clients, checks Jötunn's synchronized admin status, and temporarily adds or removes map markers on the caller's map. It does not broadcast coordinates to other players. Normal Haldor-style discovery remains available to every player who approaches a generated shop.

Valheim stores location placement in the world save and generates locations once. The 0.4.1 placement fix applies to newly generated worlds; it does not backfill a world where 0.4.0 already failed to place the three locations. Existing hammer-built shops continue to load, and players can use `HammerBuildable` to add a shop to an old world.

The legacy piece costs, service fees, text limits, item custom-data keys, prefab IDs, ZDO keys, shop footprint, roof logic, and door RPC remain unchanged.
