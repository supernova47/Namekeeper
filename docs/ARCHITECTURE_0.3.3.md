# Namekeeper 0.3.3 — recipes, discovery and prices

Namekeeper 0.3.2 remains the user-approved stable base. Version 0.3.3 changes only construction recipes, hammer category and default service prices. Persistent prefab names, ZDO fields, shop geometry, separate-roof behavior, NPC, HUD, item text and Nexus integration remain based on 0.3.2.

The shop uses the vanilla item prefab requirements `Wood` 20, `FineWood` 10, `RoundLog` 10, `Stone` 10 and `Coins` 100. `FineWood` and `RoundLog` are present in the installed Valheim 1.0.20 extended asset manifest. The recipe's new materials gate construction at Black Forest progression. Requirement counts remain BepInEx settings under `[Building]`; changes require a restart and affect new builds. Existing shops are not modified.

Both the shop and separate roof use the vanilla `Misc` hammer category. The shop's workbench association is retained. The roof remains an independent piece with its own 4 Wood recipe, and the first roof remains included with the shop as before.

No custom discovery or toast patch is added. The game's `Player.UpdateKnownRecipesList` invokes `HaveRequirements(Piece, RequirementMode)` with known-material mode. The game checks each item in `Piece.m_resources` against the player's known materials. Once all are known, `Player.AddKnownPiece` adds the piece to recent build items and calls `MessageHud.QueueUnlockMsg` with `$msg_newpiece`, the piece icon and localized name. This is why new recipe materials must stay in the normal `Piece.m_resources` list.

Name and description edits and their independent resets cost 25 coins per field. The one-time 0.3.3 migration changes existing exact 50-coin values to 25 when the migration marker is absent; other custom price values remain unchanged. New configurations start at 25. The current roof-repair price and shop construction coin cost remain unchanged.

The local game's type metadata confirms `Misc` is a valid `Piece.PieceCategory`. User acceptance of the new recipe unlock, toast, category, price migration, construction and prior behavior remains pending in `TEST_PLAN_0.3.3.md`.
