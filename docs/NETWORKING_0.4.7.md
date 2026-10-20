# Namekeeper 0.4.7 networking

All players and the server must use 0.4.7. The existing strict Jotunn version check, synchronized gameplay configuration, authenticated administrator map requests, and saved prefab/ZDO identities are unchanged.

The bench uses the three native Chair components and attachment points. Native player attachment/animation provides multiplayer presentation and native nearby-player checks determine seat occupancy. No extra furniture network object, inventory, or custom seat RPC is created.

The table and carrot displays are static local presentation rebuilt from the durable camp anchor. Food has no ItemDrop or Rigidbody. Collision-query shapes exist only during placement and are destroyed before the next physics simulation. Decorative food cannot be consumed or looted. The native table's solid colliders are shared in layout on all peers.

Discovery messages run only for a real local player after the native location pin exists; a dedicated server displays no HUD. History is saved in each character's custom data, scoped to world and camp. Administrator-revealed distant pins do not mark locations as discovered or announce them. The temporary map command and its authorization are unchanged.

Existing camps refresh presentation when loaded; their terrain and coordinates are preserved. No live profile, server configuration, or world file is changed by packaging. Manual tests must check native sitting, occupancy, join/rejoin, notices, and carrot geometry on the target game build.
