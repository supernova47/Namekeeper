# Namekeeper 0.2.0: workshop construction and roof lifecycle

Stable baseline: user-approved 0.1.6. Fifteen original C# files are unchanged, including NativeHud, HudLayout, NamekeeperGui, MerchantDialogue, ItemText and the transaction/tooltip rules.

## Registration and geometry

ShopRegistry registers SN_NamekeeperRoof first, then preserves SN_NamekeeperShop. Both are CustomPiece persistent network prefabs with Piece and WearNTear. The unused CreateEmptyPrefab primitive renderer/filter/collider is removed. Piece.m_placeEffect and WearNTear hit/destroy effects come from the installed piece_workbench components, never guessed sound-prefab names.

ShopModel builds the existing body footprint plus decorative timber, iron/brass fittings, shelves, writing tools and two small shadowless point lights. WorkshopMesh supplies a shared beveled box with flat face normals; shared materials enable instancing. Decorative props have no colliders or loot. Graphics material/light generation is skipped with GraphicsDeviceType.Null. WorkshopSign uses the existing Jotunn Averia font and game language; it creates no separate network entity.

WorkshopLayout defines direct-child tagged snappoint transforms. Piece.GetSnapPoints only enumerates direct children; nested points would not work. Nine base points, five roof mounts and five roof-side points participate in native manual/automatic snapping. Native player input is untouched. Localized transform names are shown by Valheim's snapping message.

ShopIcons uses the inspected RenderManager.Render(RenderRequest), 256×256, without stale cache. A custom procedural silhouette is assigned first for each type so failed rendering cannot leave a workbench/missing icon. RenderManager clones under an inactive parent and strips scripts before activation. The original root keeps RoofPreview for ghost/icon rendering; ShopRoofAssembly disables it on every live network instance, including non-owner clients.

## Persistent roof migration

The owner of SN_NamekeeperShop alone runs RoofMigration.Initialize. Existing arrival ZDO keys are unchanged. New namespaced keys:

- sn_namekeeper_roof_migration: 0 uninitialized, 1 reserved, 2 complete.
- sn_namekeeper_roof_id: initial roof ZDOID.
- On the roof: sn_namekeeper_roof_included and sn_namekeeper_roof_parent.

Migration writes state 1 BEFORE Instantiate, then creates the persistent roof, marks it included, records its ID and writes state 2. A recorded existing roof is adopted. Reserved/completed state never creates another free roof, even if the first roof has since been removed. Failure logs the condition, destroys a partially created live instance if available and leaves manual replacement possible. This deliberately prefers a missing repairable roof over uncertain repeated creation after a crash. Tests inject failures before creation and final state commit; actual world-save atomicity and ownership timing require live testing.

Included roofs clone resource requirements per instance and disable their refund. The default shop recipe still covers their cost. Manually built replacement roofs cost four Wood and retain native recovery. RoofRequirements copies both the array and each mutable Requirement record, preserving item references and every resource field; it never changes the shared recipe.

WearNTear.m_onDestroyed removes the linked initial roof when the body is actually destroyed; ordinary OnDestroy/area unload never performs persistent cleanup. A loaded linked roof is claimed before ZNetScene.Destroy, because that method only destroys the ZDO for its owner. Independently built roofs retain normal support behavior. The roof has 400 HP; the body 1200. The body's support decay is disabled to avoid collapsing legacy placements, while roof support remains native. Rain-only wear is disabled on both.

## Cover, merchant and data

ShopState.IsRoofed delegates to native Cover.IsUnderRoof at the merchant head with the prior two-second cache. The body is tagged leaky; separate roof collider objects are not. Thus real own/external cover qualifies without relying on names or a marker on a child cube. Arrival keys/timing and local Hildir lifetime are unchanged. No roof destroys, respawns or duplicates the merchant. The existing CanServe path closes an open HUD when required cover disappears.

HUD, pricing, text/reset/Nexus rules and local merchant voices are preserved. This stage does not add server-authoritative payments. Headless registration and ownership/client synchronization have compiled API support but are not certified by isolated tests.
