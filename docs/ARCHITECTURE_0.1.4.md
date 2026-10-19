# Namekeeper 0.1.4 changes

`NamekeeperPlugin`, `ShopRegistry`, `ShopState`, `MerchantVisual` and input guards retain the 0.1.3 lifecycle. See historical `ARCHITECTURE.md` only for background; its old claims that CreateItemTooltip is unavailable are superseded by the local 1.0.20 DLL inspection.

`PersonalizedText` holds dependency-free rules for existing per-instance keys, independent reset markers and read-only Nexus fallback. Display priority is own custom value → own explicit vanilla reset → Nexus value → vanilla. A plain item needs no new keys until edited. Empty custom descriptions remain distinct from resets. Reset cost planning compares metadata/effective text, avoiding repeat charges.

`ItemText` adapts these rules to ItemData and patches ground hover, static tooltip description and `InventoryGrid.CreateItemTooltip(ItemData, UITooltip)`. Harmony ordering follows `com.supernova.valheimspawner.InputGuard`, the verified Nexus owner. Namekeeper-only overrides apply only to items it owns. `TooltipTitleUpdater` caches reflection and calls the actual Set with the existing body/anchor/fixed position; it never mutates SharedData or traverses the entire grid. A known leading vanilla/Nexus description can be replaced; unknown layouts stay intact and warn once.

`InventoryTransaction.Execute` uses the live inventory list (the installed GetAllItems returns that list) and stages the vanilla-equivalent eligible coin debit together with the instance metadata edit. It invokes no inventory notifications during staging. Failure restores original coin references/counts/list indices and the original customData dictionary/reference. It does not reconstruct currency via prefab/AddItem, so refund does not depend on empty slots or ObjectDB. No inventory-changing external callback may be added to its applyText action.

After staging commits, `InventoryChangeDispatcher` invokes `Changed(false,false)` or a legacy fallback exactly once. Observer exceptions are logged as post-commit refresh issues and do not undo a published transaction. Cosmetic effect failure is also isolated. Client-side authority is unchanged; custom economy hooks on RemoveItem are not invoked and require explicit compatibility work if needed.

Tests link the production rules/dispatcher/transaction/tooltip adapter. Minimal game data doubles and an instrumented tooltip exercise failure and refresh paths without loading Unity. These are isolated tests, not game or Harmony-integration tests.
