# 0.1.5: uGUI presentation and local merchant dialogue

Working baseline: user-confirmed 0.1.4. Core ItemText, PersonalizedText, TooltipTitleUpdater, InventoryTransaction and InventoryChangeDispatcher are retained unchanged. World prefab identity, ZDO keys, arrival and payment rules remain compatible.

`NamekeeperGui` now owns only session/draft state, input capture and the existing validated transaction flow. It delegates presentation to `NativeHud`. No OnGUI/GUI.Window or generated IMGUI textures remain in the active UI path.

`NativeHud` uses verified Jötunn 2.30.2 public helpers for wood panels, fonts, text, buttons and InputField. Scroll views use Unity DefaultControls.CreateScrollView with Jötunn resources and ApplyScrollRectStyle. Inspection showed Jötunn's own CreateScrollView returns a canvas wrapper and automatic content layout, so it is intentionally not treated as a direct ScrollRect. No guessed child paths are used.

The panel lives under CustomGUIBack: vanilla NPC speech in the native GUI remains above it. It fits available canvas dimensions, keeps the purchase footer fixed and scrolls the editor separately. Rows rebuild only when item references/order change; labels/icons/highlights update independently. Input fields do not truncate stored original descriptions; validation remains before billing. Hide deselects only this HUD's focused control, and releases only its own input block request. GUIManager's private, inspected InputBlockRequests field is read defensively to retain a visible cursor for another Jötunn UI.

`MerchantDialogue` lives on the existing local merchant holder, not a separate network object. It sends text through the verified native Chat.SetNpcText signature, using Hildir's dialogue-height setting. Game language is read from Localization.instance.GetSelectedLanguage (assembly_guiutils): only Russian selects RU, every other language selects EN. HUD config language is independent.

The Hildir source prefab is inspected read-only. AudioClip references are collected from ZSFX components in Trader.m_randomTalkFX/m_randomGreetFX/m_randomStartTradeFX/m_randomBuyFX/m_randomGoodbyeFX/m_randomSellFX effect prefabs. Clips are played on one local spatial AudioSource; a native mixer group is retained when available. EffectList.Create and original Trader behavior are not invoked: no extra NPC/RPC or uncontrolled trade logic is created. No audio is extracted into the release. AudioSource components on the visual-only clone are stopped/disabled before activation; the speaker remains separate.

`DialogueLines` contains all RU/EN variants and priorities; `DialogueScheduler` has one pending line, duplicate throttling, expiry and outcome priority. UI actions enqueue lines, never one per keypress. Proximity and idle checks run twice per second, and the nearest merchant alone supplies ambient chatter. Errors in speech/audio are caught as presentation issues and cannot roll back or charge a transaction.

The new assembly references are installed game assemblies assembly_guiutils and UnityEngine.AudioModule. There are no new external libraries. Headless worlds retain the shop logic, with no client HUD or merchant speaker created.

Isolated tests cover the preserved 0.1.4 rules plus game-locale selection, every dialogue cue, scheduling and layout bounds. These do not instantiate Unity UI or prove that native asset clips are populated: live rendering, voices and input coexistence require the game checklist.
