# Namekeeper 0.1.4 — required game acceptance

Status: **not executed**. Back up character/world, existing DLL and profile config first. Keep Nexus JSON and .bak. Use a disposable world; keep only one Namekeeper DLL installed.

## Load and inventory title

- [ ] BepInEx loads Namekeeper 0.1.4 without patch exceptions. Log contains `after CreateItemTooltip (UITooltip.Set)`.
- [ ] Hover an already personalized item from 0.1.3; the only heading is its custom name. Hold the pointer still for several seconds: heading stays custom.
- [ ] Two identical original items with distinct personal names stay independent; an unmodified third item retains its normal name.
- [ ] Move items between slots and into/out of a chest; drop/pick up, equip/repair, save character and fully restart. Verify name, description, stats and unrelated mod metadata.

## Nexus and paid reset

- [ ] Test items with only Namekeeper data, only Nexus data and both. Existing Nexus-only text is visible before a Namekeeper edit.
- [ ] On an item with both sets of data, Namekeeper wins in inventory and on the ground; description does not contain duplicate Nexus/vanilla paragraphs.
- [ ] Reset **name only**: 50 coins, vanilla name, description unchanged. Reset **description only**: 50 coins, vanilla description, name unchanged. Both: 100 coins.
- [ ] Repeat a completed reset: no change and no additional charge. Cancel a pending reset: original draft returns and no charge.
- [ ] New custom text after reset displays correctly; after saving/restarting, reset masks and foreign metadata remain.
- [ ] Restore a vanilla description longer than the configured text limit and containing game formatting. It must restore intact, without blocking reset. Try an overlong/rich-text custom edit: reject before charging.
- [ ] Test EN and RU game language and both HUD language settings. Reset should use the current vanilla localization, not freeze one translation.
- [ ] Verify with Nexus enabled and disabled, and if practical both plugin load orders. Unknown description layouts must log a warning and preserve stats.

## Payment and regression

- [ ] Name/description/both charges are 50/50/100; unchanged drafts cost zero; insufficient funds fail without changing text or money.
- [ ] Use exact funds in one stack, funds across several stacks, and a full inventory. Verify remaining currency and original item properties.
- [ ] Double-click Confirm: one actual edit/charge. Test configured free services. Reopen UI after purchase.
- [ ] If `Purchase committed; inventory notification failed` occurs, the purchase must remain successful; collect the log. Fault injection in the automated tests is not a substitute for runtime notification compatibility.
- [ ] Shop/NPC, configured arrival delay (including existing 15 s), build resources/workbench, gold effect and saved arrival state still behave as before.
- [ ] HUD blocks camera/attacks/scroll/gameplay keys while typing. Escape, Close and removing the shop restore controls. Test alongside Nexus at 1920×1080 and 1280×720.

Co-op and dedicated-server acceptance remain separate. The 0.1.3 general regression checklist is retained as `TEST_PLAN.md`; its uncompleted entries remain pending.
ц