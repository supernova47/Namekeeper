# Namekeeper 0.4.4 networking notes

- The plugin keeps `EveryoneMustHaveMod` compatibility with patch-version strictness. Install the exact same Namekeeper release on the dedicated server and every client.
- `OperatingMode`, service prices, construction costs, merchant arrival delay, roof requirements, and text limits remain server-synchronized.
- World location generation and persistence remain owned by Valheim's ZoneSystem. The three shops are generated for new worlds; existing generated shops load the updated location prefab after the mod update.
- Automatic discovery and Haldor-style map icons are calculated locally for each client. The `namekeeper_map on|off` command remains administrator-only; the authoritative server validates the platform-formatted ID and sends positions only to the requesting administrator. Those temporary pins remain on that client's map.
- The camp's combat and damage guards run on every peer. Damage is checked again at authoritative RPC/application paths, so a client-side hit cannot damage camp pieces or non-player characters when the owner is the dedicated server.
- Interface/sign language and the optional English/Russian override are local client settings and are not synchronized from the server.
- This release does not deploy itself. Update server and clients together, then check the dedicated-server log for matching plugin versions and the three registered custom locations.
