# Architecture notes — Namekeeper 0.4.3

The synchronized `Shop.OperatingMode` setting selects `WorldLocations` (default) or `HammerBuildable`. Both legacy prefabs remain registered under their original names so placed shops and their ZDO state remain loadable. The hammer mode controls visibility of the shop and replacement roof in Hammer.

Jötunn clones the native `Vendor_BlackForest` location three times. Each entry targets the Meadows with one placement, a 2,800 m group spacing, and a 1,000–6,500 m center-distance range. Native Haldor-style discovery fields hide the marker until discovery and provide `$sn_namekeeper_shop` as the localized notification label.

`namekeeper_map on|off` is a local client command. On `on`, the client asks the authoritative server for generated shop positions through the `RevealShopLocations` RPC. The server resolves the peer from the RPC sender and authorizes it by parsing each platform-prefixed admin-list ID, then comparing its numeric user ID with the peer's numeric ID. This supports entries such as Steam's `V_<SteamID>` format as well as legacy bare numeric entries. Only the authorized caller receives positions; temporary, non-saved pins are created on that caller's minimap. `off` removes those temporary pins and cached coordinates.

The RPC avoids querying `ZoneSystem` on a remote client, where the authoritative generated-location list may not exist. The server log records the caller, authorization decision, and number of positions found without logging coordinates. A location must exist in generated world data before the command can reveal it.

The legacy shop and roof IDs, ZDO keys, recipe, service fees, item metadata, shop footprint, roof repair, and door behavior remain unchanged. Location generation, map RPC authorization, and map pins still require live game validation.
