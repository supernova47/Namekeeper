# Namekeeper 0.4.8 project context

Author: SuperNova. License: MIT. README and changelog are English; Russian acceptance checklists are provided separately. Packaged README files do not contain compilation instructions.

The default WorldLocations mode generates three independent hidden shops in Meadows in new worlds, 800–3500 metres from the centre and at least 1500 metres apart. Existing worlds and camp terrain are preserved. HammerBuildable keeps the established workshop and recipe. Mode and gameplay settings are synchronized by the server; every peer requires the same mod version.

Generated camps use a protected 12-metre area, native map icons, grass exclusion, vanilla decorations, two stationary animated boars and the approved rest area. Inkblot and Snout respond to petting without player ownership, following or breeding. The bench has three native Chair seats; the table's food is decorative. Local signs, interface and notices follow game language with English/Russian overrides.

Version 0.4.8 replaces procedural carrots with the original Hildir bowl/turnips siblings in one shared frame. Preserve all authored local transforms and scales; ground and rotate the complete composition only. The native bowl centre is (6, -4.4) relative to the shop. Boars stand at (5.25, -3) and (6.75, -3). No physics packing or food pickup scripts remain.

Preserve GUID com.supernova.valheimnamekeeper, shop/roof prefab identities, saved ZDO keys, item metadata, prices and construction costs. Merchant gestures return through the native animator; do not force idle through Animator.Rebind, Animator.Play or saved-state crossfades.

Publish one shared install ZIP for Thunderstore and r2modman with author SuperNova in manifest.json. Keep developer source snapshots separate. Do not distribute game models, Valheim libraries, audio or dependency DLLs. Building/packaging must not modify live profiles, server settings or worlds.

Validation: compilation, 64 isolated tests and 30 structural/native-contract checks passed. Native Hildir geometry was inspected offline; live-game rendering and dedicated-server acceptance remain pending. Current documentation: BUILD_STATUS.md, docs/ARCHITECTURE_0.4.8.md, docs/NETWORKING_0.4.8.md and docs/TEST_PLAN_0.4.8.ru.md.
