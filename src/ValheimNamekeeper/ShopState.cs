using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    // ZDO is the sole durable authority for arrival. Merchant graphics are local children,
    // not independent network NPCs, preventing doubled traders after world reload.
    internal sealed class ShopState : MonoBehaviour
    {
        private const string SpawnAtKey = "sn_namekeeper_spawn_s";
        private const string ArrivedKey = "sn_namekeeper_arrived";
        private ZNetView _view;
        private GameObject _merchant;
        private float _nextCheck;
        private float _nextRoofCheck;
        private bool _covered;

        internal static readonly Vector3 MerchantLocalOffset = new Vector3(.12f, 0f, -.50f);
        internal Vector3 CounterPoint => transform.TransformPoint(new Vector3(0f, 1.25f, -1.5f));
        internal Vector3 MerchantPosition => transform.TransformPoint(MerchantLocalOffset);
        internal bool Arrived => _view != null && _view.IsValid() && _view.GetZDO().GetInt(ArrivedKey, 0) != 0;
        internal void Speak(DialogueCue cue)
        {
            if (_merchant != null) _merchant.GetComponent<MerchantDialogue>()?.Request(cue);
        }

        private void Awake() { _view = GetComponent<ZNetView>(); }

        private void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 1f;
            if (_view == null || !_view.IsValid() || ZNet.instance == null) return;
            var zdo = _view.GetZDO();
            if (zdo == null) return;
            if (_view.IsOwner() && zdo.GetInt(ArrivedKey, 0) == 0)
            {
                long now = (long)ZNet.instance.GetTimeSeconds();
                long arrival = zdo.GetLong(SpawnAtKey, 0L);
                if (arrival <= 0L)
                {
                    arrival = now + NamekeeperPlugin.SpawnDelay.Value;
                    zdo.Set(SpawnAtKey, arrival);
                }
                if (now >= arrival)
                {
                    zdo.Set(ArrivedKey, 1);
                    NamekeeperPlugin.Log.LogInfo("Merchant arrived at " + transform.position);
                }
            }
            if (Arrived && _merchant == null && !ZNet.instance.IsDedicated())
                _merchant = MerchantVisual.Create(this);
        }

        internal bool CanServe(out string reason)
        {
            reason = null;
            if (!Arrived)
            {
                reason = L.T("The merchant has not arrived yet.", "Торговка ещё не прибыла.");
                return false;
            }
            if (NamekeeperPlugin.NeedRoof.Value && !IsRoofed())
            {
                reason = L.T("The shop needs a roof.", "Лавке нужна крыша.");
                return false;
            }
            return true;
        }

        internal bool IsRoofed()
        {
            if (!NamekeeperPlugin.NeedRoof.Value) return true;
            if (Time.time < _nextRoofCheck) return _covered;
            _nextRoofCheck = Time.time + 2f;
            // Native cover ignores "leaky" beams and accepts real own/external roof colliders.
            _covered = Cover.IsUnderRoof(transform.TransformPoint(new Vector3(0f, 2.15f, -.45f)));
            return _covered;
        }

        private void OnDestroy()
        {
            if (NamekeeperPlugin.Instance != null && NamekeeperPlugin.Instance.Ui != null)
                NamekeeperPlugin.Instance.Ui.CloseIfShop(this);
            if (_merchant != null) Object.Destroy(_merchant);
        }
    }
}
