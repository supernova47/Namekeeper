using System;
using System.Reflection;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    // The moving leaf is the native wood_door child. It shares the existing shop ZDO,
    // so its state is saved and synchronized without adding another network object.
    internal sealed class WorkshopDoor : MonoBehaviour, Hoverable, Interactable
    {
        private const string StateKey = "sn_namekeeper_door_open";
        private const string RpcName = "SN_NamekeeperDoor";
        private static readonly FieldInfo OpenEffectsField = typeof(Door).GetField("m_openEffects", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CloseEffectsField = typeof(Door).GetField("m_closeEffects", BindingFlags.Instance | BindingFlags.NonPublic);

        private ZNetView _view;
        private EffectList _openEffects;
        private EffectList _closeEffects;
        private int _visualState = -1;
        private float _nextRegister;
        private bool _registered;

        internal void Initialize(Door nativeDoor)
        {
            _openEffects = ReadEffects(OpenEffectsField, nativeDoor);
            _closeEffects = ReadEffects(CloseEffectsField, nativeDoor);
            _view = GetComponentInParent<ZNetView>();
        }

        private static EffectList ReadEffects(FieldInfo field, Door source)
        {
            try { return field != null && source != null ? field.GetValue(source) as EffectList : null; }
            catch (Exception) { return null; }
        }

        private void Update()
        {
            if (_view == null) _view = GetComponentInParent<ZNetView>();
            if (_view == null || !_view.IsValid()) return;
            if (!_registered && Time.time >= _nextRegister)
            {
                _nextRegister = Time.time + 1f;
                _view.Register<int>(RpcName, RPC_SetState);
                _registered = true;
            }
            if (!_registered) return;

            var zdo = _view.GetZDO();
            if (zdo == null) return;
            int state = zdo.GetInt(StateKey, 0) == 0 ? 0 : 1;
            if (_visualState != state)
            {
                if (_visualState >= 0) PlayEffects(state == 1 ? _openEffects : _closeEffects, zdo.m_uid);
                _visualState = state;
            }

            Quaternion target = Quaternion.Euler(0f, state == 1 ? 100f : 0f, 0f);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, 220f * Time.deltaTime);
        }

        private void RPC_SetState(long sender, int state)
        {
            if (_view == null || !_view.IsValid() || !_view.IsOwner()) return;
            _view.GetZDO().Set(StateKey, state == 0 ? 0 : 1);
        }

        private void PlayEffects(EffectList effects, ZDOID id)
        {
            if (effects == null) return;
            try { effects.Create(transform.position, transform.rotation, transform, 1f, -1, id); }
            catch (Exception ex) { NamekeeperPlugin.Log.LogDebug("Workshop door sound unavailable: " + ex.Message); }
        }

        private bool IsOpen => _view != null && _view.IsValid() && _view.GetZDO() != null &&
            _view.GetZDO().GetInt(StateKey, 0) != 0;

        public string GetHoverName() => L.T("Shop door", "Дверь лавки");

        public string GetHoverText()
        {
            if (!_registered || _view == null || !_view.IsValid()) return "";
            string key = L.Vanilla("$KEY_Use");
            if (string.IsNullOrEmpty(key) || key.StartsWith("$", StringComparison.Ordinal)) key = "E";
            return "<color=#F1D7A0><b>" + GetHoverName() + "</b></color>\n[<color=#EFC36B><b>" + key +
                "</b></color>] " + (IsOpen ? L.T("Close", "Закрыть") : L.T("Open", "Открыть"));
        }

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user == null || user != Player.m_localPlayer || !_registered || _view == null || !_view.IsValid()) return false;
            if (Vector3.Distance(user.transform.position, transform.position) > 3.5f) return false;
            int nextState = IsOpen ? 0 : 1;
            _view.InvokeRPC(RpcName, nextState);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
