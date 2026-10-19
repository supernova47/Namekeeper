# Multiplayer and dedicated-server notes — 0.3.8

## Required installation

Namekeeper uses custom networked building prefabs and a door RPC. Jötunn therefore enforces `EveryoneMustHaveMod` with patch-level version strictness: the server and every connecting player must have the same Namekeeper version. Install the matching BepInExPack Valheim and Jötunn versions listed in `manifest.json` on the server and clients.

## Settings controlled by the server

Jötunn marks the following BepInEx entries as server-synchronized and admin-only:

- service prices (`Services`)
- shop and replacement-roof recipe inputs (`Building`)
- merchant arrival delay and roof requirement (`Shop`)
- name and description length limits (`Text`)

When Jötunn reports that configuration has synchronized, the client rebuilds its local Hammer recipe requirements from the server values. This keeps recipe UI and the server's registered piece definitions aligned. Change server settings before starting the world; restart the world after changing building recipes.

Language, voice, dialogue text, voice volume, and success particles remain local presentation settings.

## Dedicated-server behavior

Prefab registration and persistent building state still run on a headless server. The Hammer icon renderer is skipped when the Unity graphics device is null, and the plugin does not construct the native shop UI on a headless server. The merchant model, animations, speech bubble, sound, and particles are local client visuals attached to the shop object; they do not create a second network character or duplicate ZDO.

The shop's arrival flag, roof link, and door state are stored in the shop's ZDO. Door interactions are sent to the ZDO owner; the owner updates the shared state and each peer animates its local door copy. Item personalization is stored in the item's custom data and the coin debit is applied as one inventory transaction before Valheim sends the changed inventory.

## Validation boundary

Source and compile checks cannot prove a live dedicated server handshake. Before publishing, run the version-match, version-mismatch, recipe synchronization, shop placement, door replication, and item persistence checks in `TEST_PLAN_0.3.8.md` on a disposable world.
