# Internal architecture (0.1.3 source preview)

`NamekeeperPlugin` owns config, Jötunn events, Harmony and IMGUI lifecycle. `ShopRegistry` makes a single whole building prefab with Jötunn and gives it a `ZNetView` and `ShopState`. `ShopState` stores `sn_namekeeper_spawn_s` and `sn_namekeeper_arrived` in its ZDO. A locally rendered model is created from Hildir when `arrived == 1`; it is NOT a second independent network entity. `MerchantInteraction` implements vanilla `Hoverable` + `Interactable` to open `NamekeeperGui`. `ItemText` owns namespaced instance keys, input constraints and tooltip patches. `NamekeeperEffects` creates one-shot golden particles.

## Data invariants

- Selection stores a reference to an item in the local player's live inventory and checks it remains there before billing.
- No shared ItemData prefab fields are mutated.
- Money comes from `$item_coins`, removed using Inventory.RemoveItem. The count is checked before and after.
- Existing `m_customData` entries are never cleared. Undo restores the prior dictionary snapshot on exception.
- Arrival is durable (ZDO-backed), and each client only creates local visuals once.
- Transaction validation and money removal are currently CLIENT-SIDE only; no server-side anti-cheat guarantee.

## Planned next tasks before version can be considered stable

1. Compile and fix game-version-specific APIs with the actual local DLLs.
2. Replace passive roof children with independent, networked removable/repairable roof sub-pieces and robust cover detection.
3. Implement a confirmed server-issued transaction RPC linked to validated shop/player proximity and client-side commit, documenting the limits of client-owned inventory. Consider economy compatibility with Valheim's standard non-authoritative inventory.
4. Replace IMGUI with a real native-style uGUI panel, optionally forwarding clicks from `InventoryGui.OnSelectedItem` so selection happens in vanilla grid.
5. Confirm Hildir model lifetime/Animator/visual stripping under current game version; add proper icon and native material texture.
6. Add automated pure-data tests for rename cost planning, validation, ZDO arrival edge cases and rollback.

## UI and geometry in 0.1.3

`InputGuards.cs` intercepts game-side mouse button and camera capture while the IMGUI HUD is open. It does not intercept Unity IMGUI input. `NamekeeperGui.LateTick` reacquires the cursor in LateUpdate after Valheim camera updates.

`ShopRegistry.BuildModel` creates a multi-board wooden shop and extends supports below the placement origin. All geometry is registered as part of the same networked prefab; `ShopState` controls the local trader representation. Roof shingles have `ShopRoofMarker` components for the existing shelter check.

### 0.1.3 tooltip/UI/input notes

- Inventory tooltip title and body are separate: `InventoryTooltipTitlePatch` updates the heading `m_topic` via reflection on an `InventoryGrid.CreateItemTooltip` argument. `ItemTooltipDescriptionPatch` changes the tooltip body. Nothing changes the shared prefab `m_name`/`m_description`.
- Jötunn `GUIManager.BlockInput` owns input capture lifecycle for the shop window. Harmony guards remain only while this window is open.
- Workbench-like IMGUI panel is not a clone of the runtime `InventoryGui` prefab.


### 0.1.3 inventory title strategy

`InventoryTooltipTitlePatch` targets the runtime `InventoryGrid.UpdateGui` and `UpdateInventory` method(s), late in Harmony ordering. UI element index is computed from each item's `m_gridPos`, not from its prefab name. On the matched `InventoryElement`, the `m_tooltip.m_topic` field is replaced with the exact item's custom name, only while `m_customData` contains Namekeeper's name key. Both calls are guarded and logged to help diagnose new game versions. The obsolete `CreateItemTooltip` method patch was removed. The description still uses `ItemDrop.ItemData.GetTooltip`.
