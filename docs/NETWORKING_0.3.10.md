# Multiplayer and dedicated-server notes — 0.3.10

## Installation and version checks

Install the same Namekeeper version on the dedicated server and every player. Namekeeper 0.3.8 and newer declares Jötunn compatibility as `EveryoneMustHaveMod` with `Patch` strictness, so a 0.3.8+ server rejects missing or mismatched versions.

Namekeeper 0.3.7 predates that declaration. It therefore cannot enforce the version requirement against clients: a 0.3.8 client may connect to a 0.3.7 server. Update the server and clients together before relying on mismatch rejection. The Jötunn compatibility check is decided during connection by the server, so a newer client cannot retroactively add enforcement to an older server.

Install matching BepInExPack Valheim and Jötunn versions from `manifest.json` on the server and clients.

## Settings controlled by the server

Jötunn marks these BepInEx entries as server-synchronized and admin-only:

- service prices (`Services`)
- shop recipe inputs (`Building`)
- merchant arrival delay and roof requirement (`Shop`)
- name and description length limits (`Text`)

After configuration synchronization, the client updates the five shop material amounts in its existing `Piece.Requirement` entries. This preserves the locally resolved vanilla item references and their Hammer resource icons. The separate roof uses a fixed four-Wood recipe and does not need to be rebuilt during synchronization.

## Dedicated-server behavior

Prefab registration and persistent building state run on a headless server. The Hammer icon renderer is skipped when the Unity graphics device is null, and the plugin does not construct the native shop UI on a headless server. The merchant model, animations, dialogue bubble, sound, and particles are local visuals attached to the shop object; they do not create a second network character or duplicate ZDO.

The shop's arrival flag, roof link, and door state are stored in the shop's ZDO. Door interactions are sent to the ZDO owner; the owner updates shared state and each peer animates its local door. Item personalization is stored in item custom data, and the coin debit is applied as one inventory transaction.

## Validation boundary

Source and compile checks cannot prove a live dedicated-server handshake or the in-game appearance of Hammer resource icons. Run `TEST_PLAN_0.3.10.md` on a disposable world before publishing.
