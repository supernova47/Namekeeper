# Native workshop assembly 0.3.0

The user approved an open counter shop with rear/side walls, a gable roof, one body and a separate roof. Existing prefab names, root coordinates, merchant transform, floor/foot/roof anchors, costs, health and roof migration are preserved. Native parts are slightly scaled to the existing footprint; they are visual/collider children, not independent build pieces.

## Sources and safe geometry extraction

All nine source names are verified against the installed SoftRef extended manifest: wood_floor, wood_pole, wood_beam, woodwall, wood_wall_half, wood_roof, sign, piece_workbench, piece_dvergr_lantern. In particular, the full wall is **woodwall**, not an invented wood_wall. Local UnityPy inspection confirmed transform/collider layout, healthy meshes and sign material Custom/Piece. The source sign is 1 × .5 × .1m visually; its native root collider is 1 × .55 × .089m. Roof local -Z is its high edge (snap y=1 at z=-1, y=0 at z=1). Two adjacent sections per slope are rotated 90/270 degrees. Native beam is 2 × .4 × .4m and pole is .4 × 1 × .4m. These values are from installed data, not shader-name assumptions.

NativeWorkshopAssets reconstructs transform trees with MeshFilter/sharedMesh, MeshRenderer/sharedMaterials and supported solid Collider types. It never instantiates source GameObjects. No source MonoBehaviour, ZNetView, Piece, WearNTear, ItemDrop, Container, fuel/loot/FX scripts, AudioSource or Rigidbody is copied or activated. Only the existing Namekeeper roots own network/piece logic. Healthy m_new and first-LOD renderers are selected, snow materials and worn/broken/wet branches omitted. Disabled descendants and empty copied branches are pruned. Mesh renderer lighting/shadow settings and native collider dimensions/materials are preserved; local transforms, including baked-mesh offsets, are retained. Native root scene position/rotation is deliberately ignored.

ShopModel constructs under a temporary inactive holder. A missing native source discards that holder and logs a fallback to LegacyShopModel before registration, so visual dependency problems do not suppress the persistent names. The legacy implementation is retained as a diagnostic safety fallback, not the normal design. Registration order, icon-before-AddPiece validation and rejection checks remain unchanged from 0.2.1.

Body collider nodes remain leaky. Roof nodes are Untagged so native Cover.IsUnderRoof can detect actual cover. The original included-roof migration still creates one persistent SN_NamekeeperRoof, and RoofPreview remains ghost/icon only. No world positions or roof migration flags are rewritten by this version.

## Sign failure and correction

The 0.2.2 live log confirmed Standard/Unlit shader lookup failed. It created a renderer before creating its material, yielding magenta; a cloned component lost private renderer references and tried to add MeshFilter again every two seconds. The old compiled constants checks could not establish runtime shader availability.

WorkshopSign now copies the native **sign material instance**, including its actual shader/keywords/pass settings. Graphics.Blit/ReadPixels reads a private wood texture copy, dark handwritten PNG alpha is composited into it, and all final pixels are opaque. The resulting Custom/Piece material uses ordinary world depth/culling, no UI or guessed shader, and no emission. Main texture scale/offset are reset for the plaque; native material/texture remain untouched. Back culling and ZWrite are explicit. The PNG resources, source Marck Script and OFL remain included.

Material preparation precedes renderer creation. Refresh uses GetComponent-or-add so copied prefab components are reused. On failure any sign renderer is disabled and retries stop on that instance; the native wooden board remains visible. RU/EN selection uses the game language, independent of HUD language. Completed materials/textures are shared across instances.

## Lighting and verification limits

Native hanging-lantern geometry replaces custom cubes. A private, cached grayscale copy of its emission mask is tinted warm with the existing Lantern_DN/fallback color. Its original shader, masks, albedo and other native materials are retained. Source materials are never edited. Lamp renderers do not cast self-occluding shadows. Centered Point/ForcePixel lights retain bounded 6–8m range and 2.5–4 intensity; native fuel/flare scripts are not copied.

BuildMod verifies the nine names against the installed manifest when present, compiles against installed game DLLs, runs four registration and six presentation/placement IL/resource checks, then packages. Data tests cover the unchanged transaction, localization, HUD, snapping and roof lifecycle contracts. Neither those checks nor UnityPy metadata inspection render the assembled hierarchy in Unity. Game appearance, collision, support and networking require the Russian checklist.

UnityPy is an optional audit-only tool in the workspace. It is not needed to build/run the mod and is not shipped. Game meshes/textures/audio/DLLs are referenced from installed prefabs at runtime and are not exported into release archives.
