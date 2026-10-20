# Namekeeper 0.4.8 — native Hildir feeding display

Compilation passed with 0 errors and the same 2 MSB3277 dependency-reference warnings. All 64 isolated tests and 30 structural/native-contract checks passed. Both native manifests provide the 38 required prefabs and the bowl/turnip models. Live Unity rendering remains pending; see docs/TEST_PLAN_0.4.8.ru.md.

- User screenshot confirmed the 0.4.7 procedural carrots still floated. That placement implementation and CampDisplayPacking were removed, rather than adjusted again.
- CampMeal captures the exact bowl and turnips siblings from Hildir_camp. Copies preserve all native local positions, rotations, scales, and first-LOD mesh references. Only the complete assembly is rigidly moved/rotated and grounded by the bowl bottom.
- Offline inspection loaded all 19 native meshes (one bowl, 18 food/greens). The native bowl is approximately 2.585 x 2.126 x 0.488 metres. Relative geometry was checked for multiple camp rotations; maximum numeric distance error was below 1.1e-13 metres. A local geometry preview was inspected. This is not a claim that Valheim was launched.
- Boars retain full size and animations; positions are slightly closer together at (5.25, -3) and (6.75, -3) around the native bowl centred at (6, -4.4).
- Current native-contract checks require authored sibling transforms and reject the removed procedural-packing class. Previous seating, discovery, protection, clutter, version and recipe checks also pass.
- Existing terrain, camp coordinates, shop/roof/ZDO identities, fees, recipes, operating modes, rest area and discovery history are preserved. No live profiles/server/worlds were modified.
- A single shared Thunderstore/r2modman release ZIP retains author SuperNova. Developer source is a separate snapshot; no second install/platform variant is produced. English README/changelog and MIT retained; packaged README has no compilation instructions.
