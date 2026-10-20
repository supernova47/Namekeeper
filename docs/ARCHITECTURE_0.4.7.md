# Namekeeper 0.4.7 camp presentation

The persistent shop remains the camp anchor. Camp layout approval covers a seatable bench at (-6.6, -5.2), a native table at (-5.0, -5.2), and the fire at (-3.0, -5.2), in metres relative to the shop. The rug is removed. Existing terrain and all saved positions remain unchanged.

CampSeating copies the three native bench Chair settings and recreates their attachment transforms in the copied collider hierarchy. Native Chair uses Character.AttachStart and Player.GetClosestPlayer for occupancy, without a furniture ZNetView. The existing player synchronizes sitting. Camp furniture retains no Piece, WearNTear, resource, or destruction scripts.

Table items use only authored attach meshes, at their original scale. Their local geometry bounds place their bottoms at the table mesh's upper surface. Bread, CookedMeat, and Tankard provide the vanilla food/drink visuals, with no ItemDrop, inventory, pickup, or consumable behavior.

The shared bowl is 3.4 by 1.6 metres and 0.45 metres high. Six carrots use 1.6 times native visual scale. Four base positions form a rectangle; two crosswise carrots rest above them. CampDisplayPacking constructs disabled temporary mesh query colliders, convex per carrot mesh and concave for the hollow bowl. It lowers each carrot against the bowl and previously placed carrots using explicit-pose Physics.ComputePenetration and refines the first contact by binary search. It never advances physics, creates rigidbodies, or retains the query colliders. Failure is logged and optional decor remains isolated from animal initialization.

ShopDiscovery observes Minimap.UpdateLocationPins after native reconciliation. A real Namekeeper location icon and a local player within 150 metres are required before the localized banner and map notice. The message does not add a second pin and does not change native map discovery. Character m_customData stores a namespaced world UID and rounded horizontal camp coordinates. The generic Location discovery label is empty to avoid a duplicate entry banner.

The compiled native Chair and Minimap contracts are reviewed separately from in-game acceptance. PhysX cooking/contact, character seating, terrain-dependent geometry and UI rendering still require the Russian checklist.
