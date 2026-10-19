using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    internal static class ShopRegistry
    {
        internal const string PrefabName = "SN_NamekeeperShop";
        internal const string RoofPrefabName = "SN_NamekeeperRoof";
        private static bool _registered;
        private static GameObject _shopPrefab;
        private static bool _recipeRequirementsWarningLogged;

        internal static void Register()
        {
            if (_registered) return;
            try
            {
                var source = PrefabManager.Instance.GetPrefab("piece_workbench");
                var sourcePiece = source != null ? source.GetComponent<Piece>() : null;
                var sourceWear = source != null ? source.GetComponent<WearNTear>() : null;
                if (sourcePiece == null || sourceWear == null)
                    throw new InvalidOperationException("Native workbench building components are unavailable");

                var config = Config("$sn_namekeeper_shop", "$sn_namekeeper_shop_desc");
                SetShopRequirements(config);
                config.Icon = sourcePiece.m_icon;
                var custom = new CustomPiece(PrefabName, true, config);
                var prefab = custom.PiecePrefab;
                _shopPrefab = prefab;
                PrepareRoot(prefab, sourcePiece, sourceWear, false);
                ShopModel.BuildBody(prefab.transform);
                // Preview only: live shops use the independent, persistent roof piece.
                var preview = new GameObject(ShopRoofAssembly.PreviewName);
                preview.transform.SetParent(prefab.transform, false);
                preview.transform.localPosition = Vector3.up * WorkshopLayout.RoofHeight;
                ShopModel.BuildRoof(preview.transform);
                AddAnchors(prefab.transform, WorkshopLayout.BodyAnchors());
                prefab.AddComponent<ShopRoofAssembly>();
                prefab.AddComponent<ShopState>();
                RegisterPiece(custom, config, false);
                NamekeeperPlugin.Log.LogInfo("Registered persistent shop " + PrefabName);

                var roofConfig = Config("$sn_namekeeper_roof", "$sn_namekeeper_roof_desc");
                SetRoofRequirements(roofConfig);
                roofConfig.Icon = sourcePiece.m_icon;
                var roof = new CustomPiece(RoofPrefabName, true, roofConfig);
                PrepareRoot(roof.PiecePrefab, sourcePiece, sourceWear, true);
                ShopModel.BuildRoof(roof.PiecePrefab.transform);
                AddAnchors(roof.PiecePrefab.transform, WorkshopLayout.RoofAnchors());
                roof.PiecePrefab.AddComponent<ShopRoofPart>();
                RegisterPiece(roof, roofConfig, true);


                _registered = true;
                NamekeeperPlugin.Log.LogInfo("Registered workshop, separate roof and native snap anchors");
            }
            catch (Exception ex) { NamekeeperPlugin.Log.LogError("Shop registration failed: " + ex); }
            finally { PrefabManager.OnVanillaPrefabsAvailable -= Register; }
        }

        internal static bool RefreshSynchronizedRecipes()
        {
            if (_shopPrefab == null) return false;

            var piece = _shopPrefab.GetComponent<Piece>();
            var amounts = new[]
            {
                NamekeeperPlugin.WoodCost.Value,
                NamekeeperPlugin.FineWoodCost.Value,
                NamekeeperPlugin.CoreWoodCost.Value,
                NamekeeperPlugin.StoneCost.Value,
                NamekeeperPlugin.ShopCoinCost.Value
            };

            // Keep the Requirement objects that Jötunn resolved when the prefab was
            // registered. Recreating them after config sync can lose m_resItem links,
            // which makes Valheim draw its fallback shield icon for every ingredient.
            if (piece == null || !RecipeRequirementAmounts.TryApply(piece.m_resources, amounts))
            {
                if (!_recipeRequirementsWarningLogged)
                {
                    NamekeeperPlugin.Log.LogWarning("Could not update the synchronized shop recipe because its vanilla resource references are incomplete; retaining the existing recipe.");
                    _recipeRequirementsWarningLogged = true;
                }
                return false;
            }

            _recipeRequirementsWarningLogged = false;
            return true;
        }

        private static void SetShopRequirements(PieceConfig config)
        {
            config.Requirements = Array.Empty<RequirementConfig>();
            config.AddRequirement(new RequirementConfig("Wood", NamekeeperPlugin.WoodCost.Value));
            config.AddRequirement(new RequirementConfig("FineWood", NamekeeperPlugin.FineWoodCost.Value));
            config.AddRequirement(new RequirementConfig("RoundLog", NamekeeperPlugin.CoreWoodCost.Value));
            config.AddRequirement(new RequirementConfig("Stone", NamekeeperPlugin.StoneCost.Value));
            config.AddRequirement(new RequirementConfig("Coins", NamekeeperPlugin.ShopCoinCost.Value));
        }

        private static void SetRoofRequirements(PieceConfig config)
        {
            config.Requirements = Array.Empty<RequirementConfig>();
            config.AddRequirement(new RequirementConfig("Wood", WorkshopLayout.RoofWood, 0, true));
        }

        private static void RegisterPiece(CustomPiece custom, PieceConfig config, bool roof)
        {
            // AddPiece validates m_icon synchronously and returns false on rejection.
            ShopIcons.Assign(custom.PiecePrefab, roof);
            // Jotunn may reapply config while fixing references; retain the rendered icon.
            config.Icon = custom.PiecePrefab.GetComponent<Piece>().m_icon;
            if (!PieceManager.Instance.AddPiece(custom))
                throw new InvalidOperationException("Jotunn rejected building " + custom.PiecePrefab.name);
        }

        private static PieceConfig Config(string name, string description) => new PieceConfig
        {
            Name = name, Description = description, PieceTable = "Hammer",
            Category = "Misc", CraftingStation = "piece_workbench"
        };

        private static void PrepareRoot(GameObject prefab, Piece source, WearNTear sourceWear, bool roof)
        {
            if (prefab == null) throw new InvalidOperationException("Jotunn did not create building prefab");
            // CreateEmptyPrefab includes a primitive cube: remove its hidden solid obstruction.
            foreach (var collider in prefab.GetComponents<Collider>()) Object.DestroyImmediate(collider);
            var renderer = prefab.GetComponent<MeshRenderer>();
            if (renderer != null) Object.DestroyImmediate(renderer);
            var filter = prefab.GetComponent<MeshFilter>();
            if (filter != null) Object.DestroyImmediate(filter);
            var piece = prefab.GetComponent<Piece>();
            piece.m_placeEffect = source.m_placeEffect;
            piece.m_canBeRemoved = true;
            piece.m_canRotate = true;
            piece.m_clipGround = false;
            piece.m_groundPiece = false;
            piece.m_noClipping = !roof;
            piece.m_notOnTiltingSurface = !roof;
            var wear = prefab.AddComponent<WearNTear>();
            wear.m_health = roof ? 400 : 1200;
            wear.m_materialType = sourceWear.m_materialType;
            wear.m_damages = sourceWear.m_damages;
            wear.m_destroyedEffect = sourceWear.m_destroyedEffect;
            wear.m_hitEffect = sourceWear.m_hitEffect;
            wear.m_switchEffect = sourceWear.m_switchEffect;
            wear.m_noRoofWear = false;
            wear.m_noSupportWear = !roof;
            wear.m_supports = true;
            wear.m_autoCreateFragments = false;
            wear.m_burnable = sourceWear.m_burnable;
        }

        private static void AddAnchors(Transform root, System.Collections.Generic.IEnumerable<SnapAnchor> anchors)
        {
            foreach (var anchor in anchors)
            {
                // Piece.GetSnapPoints inspects tagged DIRECT children, not nested transforms.
                var point = new GameObject("$sn_snap_" + anchor.Key);
                point.tag = "snappoint";
                point.transform.SetParent(root, false);
                point.transform.localPosition = new Vector3(anchor.X, anchor.Y, anchor.Z);
            }
        }
    }
}
