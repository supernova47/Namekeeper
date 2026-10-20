# Architecture notes — Namekeeper 0.4.2

The synchronized `Shop.OperatingMode` setting selects `WorldLocations` (default) or `HammerBuildable`. Both legacy prefabs remain registered under their original names so placed shops and their ZDO state remain loadable. The hammer mode controls visibility of the shop and replacement roof in Hammer.

Jötunn clones the native `Vendor_BlackForest` location three times. Each entry targets the Meadows with one placement, a 2,800 m group spacing, and a 1,000–6,500 m center-distance range. `m_maximumVegetation = 1` permits normal terrain; the previous value of zero rejected it. Native Haldor-style discovery fields hide the marker until discovery and provide `$sn_namekeeper_shop` as the localized notification label.

The world shop preserves the native force field, no-build protection, and spawn blocking. Its camp contents are replaced by Namekeeper's existing shop. Each world shop reuses existing shop state and network objects; the female merchant remains a local visual child. A native Boar starts tamed and uses a persistent ZNetView.

`namekeeper_map on|off` is a local client command. On `on`, the client sends an empty request through the `RevealShopLocations` Jötunn RPC. The server resolves the peer from the RPC sender, checks its platform player ID against the server admin list, then searches `ZoneSystem` for all three generated locations. It sends only the authorized caller the available location indexes and world positions. The client creates temporary, non-saved pins on its own minimap. `off` clears those pins and the cached coordinates. The dedicated server never creates map pins or client UI.

The RPC avoids querying `ZoneSystem.m_locationInstances` on a remote client, where the authoritative generated-location list may not exist. A missing result is reported to the user. The server log records the caller, authorization decision, and number of positions found without logging the coordinates.

The legacy shop and roof IDs, ZDO keys, recipe, service fees, item metadata, shop footprint, roof repair, and door behavior remain unchanged. Location generation and all RPC/map behavior still require live game validation.
