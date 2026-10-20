# Namekeeper 0.4.8 native camp meal

CampMeal captures two direct siblings from the loaded Hildir_camp asset: fi_vil_container_bucket01 and turnips. The first is the bowl, the second is the complete authored food/greens group with 18 mesh renderers. They are cached under separate internal aliases so unrelated camp props cannot overwrite their sources.

Both parts are copied into the same NativeMealFrame. Each copy receives its original local position, quaternion rotation and scale, including the bowl's authored nonuniform scale. A shared translation cancels the original bowl pivot; a shared yaw aligns the native bowl's longer axis with the boars. The complete group is then grounded and centred using only the bowl mesh bounds. No individual food placement, scaling, collision-query packing, Rigidbody or simulation is used.

The native bowl footprint after alignment is approximately 2.585 by 2.126 metres, height 0.488 metres. Its camp centre remains (6, -4.4). Boars now stand at (5.25, -3) and (6.75, -3), facing away from the shop as before. All actors retain their original size.

Geometry copying keeps original mesh/material references and authored child transforms. Native LOD references select the food group's first LOD without duplicating renderers or retaining camp scripts. The bowl retains its solid collider; food is decoration without ItemDrop, inventory or pickup behavior. No game assets are included in the mod package.

Offline inspection loaded the native bowl and turnip models and checked 19 meshes. Their relative geometry is preserved under whole-group translation and multiple camp rotations. Compilation/contract checks establish this copying path and the removal of CampDisplayPacking; a live Unity render remains a separate acceptance step.
