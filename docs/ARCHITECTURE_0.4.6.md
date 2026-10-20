# Namekeeper 0.4.6: camp presentation fixes

The user's 0.4.5 client log showed `No decoration mesh bounds: Native_Carrot` in `FillBowl` for all three visited camps. `WorldCamp.Update` called decoration before animal initialization and treated that exception as terminal, so neither boar could be created. The server log independently confirmed `authorized=True, locations=3` for the map command.

## Clutter exclusion

The existing `ClearVegetation` terrain modifier paints alpha. Valheim's `ClutterSystem.GenerateVegPatch` uses multiple independent filters: some entries ignore vegetation alpha and rely on cleared-ground RGB classification instead. Painting alpha and refreshing patches alone therefore did not reliably suppress visible grass/ferns.

`NamekeeperCampClutterGuard` prefixes the native `ClutterSystem.GetGroundInfo`. A candidate within an active camp's 12 m X/Z disk returns false. The installed game's generator was inspected: it immediately branches past that candidate after a false return, before creating the instance. Outside candidates follow the native implementation. The test deliberately ignores height because candidate Y is zero before the ground raycast; camps can be tens of metres above sea level.

`WorldCamp.Active` is populated before patch refresh. Existing grass buffers are invalidated with `ClutterSystem.ResetGrass`. No terrain height, saved point, or player terrain file is changed by this exclusion. Existing leveling rules and `sn_namekeeper_level_045` remain unchanged.

## Carrot visuals and initialization

`NativeWorkshopAssets.AddItemVisual` copies only the vanilla item's `attach` subtree, retaining its authored child transforms, mesh/material references and scale. It does not copy ItemDrop, item physics, sparkles, equipment-offset geometry or prefab LOD bookkeeping. The installed Carrot asset has two real renderers in `attach` and an unrelated inactive model under `equipoffset` roughly 51 m below origin. The new route selects the actual attach mesh filters explicitly, validates the result and cleans up a failed copy.

Food/decor setup is isolated from the camp's area/RPC state and animal creation. `EnsureBoars` creates each missing animal independently and retries after five seconds; successful entries are retained, so retries cannot duplicate them. Failed animal roots are destroyed. Animator and visual activation are explicit. Repeated identical errors are logged once until the error changes or creation succeeds.

## Shared bowl

The user replaced the approved layout's pair of small bowls with one large shared bowl. It is centred at local X=6, Z=-4.4 between the existing boar positions, with dimensions 3.4 × 1.3 m and height 0.35 m. Seven full-size carrot visuals lie inside it. Bounds and placement are computed in camp axes from mesh vertices, avoiding renderer bounds before activation and distortion from arbitrary world yaw. Carrots are siblings of the bowl, so resizing the bowl cannot resize the food. The remainder of the approved layout is retained.

Compilation and structural/pure tests cannot prove the rendered animals, food placement, or grass result. Use `TEST_PLAN_0.4.6.ru.md` on the user's existing world before stable release. Both peers must install 0.4.6; live profiles are not modified by packaging.
