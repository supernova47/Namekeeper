# Static checks — 2026-10-17

**Execution environment:** no .NET SDK, Valheim game DLLs or active BepInEx/Jötunn profile were available in this container. No successful compilation, game launch or dedicated-server test is claimed.

Completed local source checks:

- All 9 `.cs` files readable as UTF-8.
- Counted balanced `{`/`}` characters in every C# file (a lightweight inspection, not a compiler/semantic parser).
- Parsed `manifest.json` with Python's JSON parser and found version `0.1.3`.
- Inspected the project for implementation of timer/ZDO, shop registration, inventory filter, independent fees, data keys, tooltip hooks, currency deduction and rollback attempt.
- Read and checked Jötunn documentation on `CustomPiece`, `PieceConfig`, `RequirementConfig`, localization and vanilla-prefab availability.

Still required:

- Compile project using `BuildMod.bat` against actual game / BepInEx / Jötunn DLLs.
- Correct any API differences observed during compilation.
- Run `docs/TEST_PLAN.md` on a disposable world, then multiplayer and a headless server.

This is a **source snapshot for the first implementation milestone**, not a verified installable release.

## 2026-10-17 Build Fix 2

Observed Windows compilation failure: 21 errors; 2 warnings. Diagnosed by exact user BuildLog.txt. Added UnityEngine facade reference, `Hoverable.GetHoverOffset()`, removed the obsolete `UITooltip` use. Windows compilation and Valheim runtime tests are **pending**.

## Build Fix 3 checks

- [x] Source ZIP now has a single root folder.
- [x] XML parses and newly required managed assembly paths are listed.
- [x] No compile-time reference to removed `Inventory.Changed()` or global `Localization` remains.
- [x] Obsolete empty localization constructor removed.
- [ ] Compiler validation against actual Windows Valheim DLLs (user-side).
- [ ] Functional and dedicated-server game tests.

NOTE: 0.1.3 was not compiled against installed Valheim assemblies in this environment; complete the tests on the target PC.

## 0.1.3 source review

- [x] Fixed tooltip-body duplicate custom-name prefix; custom item name is now patched into `InventoryGrid.CreateItemTooltip` title using `m_topic` reflection.
- [x] Added Jötunn GUIManager input capture and release around the merchant HUD.
- [x] Added `ZInput.GetKey*` and `PlayerController.TakeInput` defensive guards and a short post-close input suppression interval.
- [x] Reworked IMGUI layout and textures to echo Valheim workbench dark-slate/bronze style.
- [x] Verified C# delimiter balance ignoring strings and comments, parsed CSProj XML and manifest JSON, checked version consistency.
- [x] Verified the ZIP has exactly one top-level folder and passes zip CRC test.
- [ ] **Compile** against user game assemblies, and confirm native tooltip creation method exists.
- [ ] **Game test** cursor/camera blocking, custom tooltip title, saved descriptions, and UI sizing.


## 0.1.3 static review

- [x] Replaced obsolete `CreateItemTooltip` target with `UpdateGui`/`UpdateInventory` post-vanilla tooltip-topic update.
- [x] Reads per-instance `m_customData` and resolves inventory items by `m_gridPos` to preserve independent titles.
- [x] Shared prefab data is not changed.
- [x] Build/manifest/project version set consistently to 0.1.3.
- [ ] Actual Windows compilation and in-game runtime of 0.1.3 — **pending**.
