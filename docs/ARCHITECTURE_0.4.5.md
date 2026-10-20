# Namekeeper 0.4.5 architecture

## Persistent camp lifecycle

`NamekeeperWorldShop` retains its network identity and `ShopState.IsWorldLocation`. Its new `WorldCamp` component creates the approved set dressing exactly once per loaded shop instance. The shop remains the only camp anchor; decorative furniture has no inventories, item drops, fuel requirements, or independent ZDOs. Native geometry and materials are referenced from installed Valheim resources, never distributed in the package.

The live shop installs a local `Location`, the no-monster effect area, grass-clearing modifier, and sanctuary bounds. This also works when ZNetScene recreates the shop from a saved ZDO without its original LocationProxy hierarchy. Terrain modifiers are activated with `m_triggerOnPlaced` temporarily suppressed to avoid removing existing terrain modifiers. Heightmaps rebuild before decor and animals sample their support heights; grass patches are then refreshed.

New `ZoneSystem.SpawnLocation` calls in Full/Ghost mode establish a scoped generation context. `ZNetView.Awake` records `sn_namekeeper_level_045` only for newly generated world shops in that context. Client proxy loading explicitly suppresses the context. Existing saved shops have no key and receive no leveling or smoothing. New camps level a 9.5 m inner disk and smooth to 11.5 m. New-world selection uses Meadows, 800–3500 m from centre, at least 1500 m separation, and a 1.5 m maximum terrain delta. No existing location coordinates or world files are rewritten.

## Approved layout

See `CAMP_LAYOUT_PROPOSAL.svg`, approved by the user on 2026-10-20. The existing shop stays at local origin, facing -Z. A native Hildir canopy and supplies occupy the left side, the fire and bench sit front-left, wood and crates sit rear-right, and two boars with carrot bowls sit front-right. Eight native trader runes mark the perimeter. Counter access and the rear door approach stay clear. Decorative pieces use full-size vanilla geometry and actual mesh bounds for grounding.

## Animals

`CampBoar` copies only the native Boar Visual hierarchy and keeps its Animator, bones, skins, and LODs. Native MonoBehaviours are removed before activation; animation events and root motion are disabled. The root never moves. The verified native `consume` trigger returns through the native controller to Movement/idle. A shared world-clock cadence offsets the two animals' feeding.

The boars are not Character/Tameable objects. They have no player ownership, taming, following, renaming, breeding, or drops. Petting broadcasts a cosmetic shop RPC, applies a short local idle variation, and plays native pet audio through a local spatial source. Native networked effect prefabs are not instantiated on every peer.

The old `NamekeeperTameBoar` prefab name stays registered. Its compatibility component disables its old creature presentation/AI and lets its ZDO owner remove only that legacy Namekeeper object through ZNetScene, without damage or loot. Ordinary player animals have no migration component.

## Admin map request

The original routed CustomRPC was replaced with connection-bound ZRpc handlers. The server resolves the callback's actual connection, requires a ready connected peer, and passes `peer.m_socket.GetHostName()` unchanged to `ZNet.IsAdmin`. The character's `m_playerID` is never an account credential. Replies go only to the requesting connection; clients require the current server connection and request serial. Reconnect/off clears temporary positions, stale replies are ignored, and unanswered requests time out. World access is guarded until `ZNet.GetWorld()` returns a world.

Both operating modes, existing shop/roof identifiers, pricing, recipes, item text semantics, merchant gestures and voice, and per-client localization remain in place. Visual alignment, complete camp generation on a seed, and live server behavior require the accompanying in-game checklist.
