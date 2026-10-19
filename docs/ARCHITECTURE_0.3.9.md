# Architecture notes — Namekeeper 0.3.9

The shop's five Hammer costs remain ordinary vanilla `Piece.Requirement` entries resolved during prefab registration. When Jötunn synchronizes server configuration, `ShopRegistry.RefreshSynchronizedRecipes` updates only each entry's `m_amount` through `RecipeRequirementAmounts.TryApply`. It does not replace the requirement array or call `PieceConfig.GetRequirements()` again, so `m_resItem` references and other native requirement flags remain intact. The update validates every entry before writing any amount; an incomplete recipe is left untouched and logged.

The replacement roof costs four Wood. Its recipe is fixed and is not refreshed when the server sends shop settings.

The plugin keeps `EveryoneMustHaveMod` and `Patch` compatibility metadata. Namekeeper 0.3.7 did not declare compatibility, so a server running that release cannot enforce the mismatch check. All peers must update to a release that declares the requirement; on 0.3.8 and newer servers, Jötunn performs the check at connection time.

The GUID, piece prefab names, ZDO keys, workshop footprint, existing shop state, service behavior, and configuration defaults are retained from 0.3.8.
