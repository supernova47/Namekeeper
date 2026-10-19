using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    internal sealed class ShopRoofAssembly : MonoBehaviour
    {
        internal const string PreviewName = "RoofPreview";
        private const string StateKey = "sn_namekeeper_roof_migration";
        private const string RoofKey = "sn_namekeeper_roof_id";
        private ZNetView _view;
        private float _next;
        private bool _previewHidden;
        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            var wear = GetComponent<WearNTear>();
            if (wear != null) wear.m_onDestroyed += RemoveIncludedRoof;
            // Ghosts and icon clones have no valid ZDO and retain their roof preview.
            if (_view != null && _view.IsValid())
            {
                var preview = transform.Find(PreviewName);
                if (preview != null) preview.gameObject.SetActive(false);
                _previewHidden = true;
            }
        }
        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 1;
            if (_view == null || !_view.IsValid() || ZNetScene.instance == null) return;
            if (!_previewHidden)
            {
                var preview = transform.Find(PreviewName);
                if (preview != null) preview.gameObject.SetActive(false);
                _previewHidden = true;
            }
            if (!_view.IsOwner()) return;
            var zdo = _view.GetZDO();
            int state = zdo.GetInt(StateKey, 0);
            var id = zdo.GetZDOID(RoofKey);
            bool exists = id != ZDOID.None && ZDOMan.instance.GetZDO(id) != null;
            var prefab = ZNetScene.instance.GetPrefab(ShopRegistry.RoofPrefabName);
            GameObject instance = null;
            try
            {
                // Reserve durably before Instantiate: partial creation cannot duplicate free roofs.
                RoofMigration.Initialize(state, _view.IsOwner(), prefab != null, exists, value => zdo.Set(StateKey, value), () =>
                {
                    instance = Object.Instantiate(prefab, transform.TransformPoint(Vector3.up * WorkshopLayout.RoofHeight), transform.rotation);
                    var view = instance.GetComponent<ZNetView>();
                    if (view == null || !view.IsValid()) throw new InvalidOperationException("Roof has no persistent ZDO");
                    instance.GetComponent<ShopRoofPart>().MarkIncluded(zdo.m_uid);
                    zdo.Set(RoofKey, view.GetZDO().m_uid);
                });
            }
            catch (Exception ex)
            {
                // Never retry an uncertain creation. The normal hammer roof remains available.
                if (instance != null) ZNetScene.instance.Destroy(instance);
                NamekeeperPlugin.Log.LogError("Roof migration incomplete; build a replacement with the hammer: " + ex);
            }
        }

        private void RemoveIncludedRoof()
        {
            // WearNTear invokes this only for actual destruction, never on scene unload.
            if (_view == null || !_view.IsValid() || !_view.IsOwner()) return;
            var id = _view.GetZDO().GetZDOID(RoofKey);
            var roof = id != ZDOID.None ? ZNetScene.instance.FindInstance(id) : null;
            if (roof == null) return; // A previously destroyed roof must never be recreated.
            var view = roof.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return;
            view.ClaimOwnership();
            ZNetScene.instance.Destroy(roof);
        }
    }

    internal sealed class ShopRoofPart : MonoBehaviour
    {
        private const string IncludedKey = "sn_namekeeper_roof_included";
        private const string ParentKey = "sn_namekeeper_roof_parent";
        private ZNetView _view;
        private float _next;
        private bool _includedApplied;
        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            if (_view != null && _view.IsValid()) ApplyRefund(_view.GetZDO().GetBool(IncludedKey, false));
        }
        internal void MarkIncluded(ZDOID parent)
        {
            _view.GetZDO().Set(IncludedKey, true);
            _view.GetZDO().Set(ParentKey, parent);
            ApplyRefund(true);
        }
        private void ApplyRefund(bool included)
        {
            var piece = GetComponent<Piece>();
            // Never mutate the shared requirement array or its reference-type entries.
            piece.m_resources = RoofRequirements.ForInstance(piece.m_resources, included);
            _includedApplied = included;
        }
        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 2;
            if (_view == null || !_view.IsValid()) return;
            var zdo = _view.GetZDO();
            if (!zdo.GetBool(IncludedKey, false)) return;
            if (!_includedApplied) ApplyRefund(true);
        }
    }
}
