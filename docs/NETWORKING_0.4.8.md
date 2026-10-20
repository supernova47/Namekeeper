# Namekeeper 0.4.8 networking

All clients and the server require version 0.4.8. Version enforcement, synchronized gameplay settings, authenticated administrator map requests, discovery history and saved prefab/ZDO identities are unchanged.

The feeding display is rebuilt from the persistent shop anchor using the same authored Hildir bowl and food transforms on each peer. It has no food network objects, loot, ownership or physics simulation. Existing boar animation and pet RPC behavior is retained; only the two local display positions are slightly closer together.

Camp terrain and saved shop coordinates are not changed. Existing worlds can load the new display after restart. Client/server profiles and worlds are not modified by the packaging scripts.

One shared archive includes `author: SuperNova` and serves both Thunderstore and r2modman. The developer source snapshot contains no game assets or dependency binaries. Use the Russian 0.4.8 checklist for final visual and multiplayer acceptance.
