using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    // Like Hildir's camp animals: an animated display with an interaction, not a
    // Character/Tameable. No owner, hunger, breeding, follow or rename state exists.
    internal sealed class CampBoar : MonoBehaviour, Hoverable, Interactable
    {
        private WorldCamp _camp;
        private int _index;
        private Animator _animator;
        private AudioSource _voice;
        private readonly List<AudioClip> _petClips = new List<AudioClip>();
        private long _lastMeal;
        private float _reactUntil;
        private float _nextPet;

        internal static CampBoar Create(WorldCamp camp, int index, Vector3 position)
        {
            var native = NativeWorkshopAssets.Source("Boar");
            var root = new GameObject("NamekeeperCampBoar_" + index);
            try
            {
                root.SetActive(false);
                root.transform.SetParent(camp.transform, false);
                root.transform.position = position;
                root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                root.layer = LayerMask.NameToLayer("piece");
                var visualSource = native.transform.Find("Visual");
                if (visualSource == null) throw new InvalidOperationException("Native Boar Visual is unavailable");
                var visual = Object.Instantiate(visualSource.gameObject, root.transform, false);
                // AnimationEvent receivers and native creature logic expect a live Character.
                // Remove them before activation; retain bones, skins, controller and LODs.
                foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
                var component = root.AddComponent<CampBoar>();
                component._camp = camp;
                component._index = index;
                component._animator = visual.GetComponentInChildren<Animator>(true);
                component._animator.applyRootMotion = false;
                component._animator.fireEvents = false;
                component._animator.enabled = true;
                component._animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                component.PrepareVoice(native.GetComponent<Tameable>()?.m_petEffect);
                var nativeCollider = native.GetComponent<CapsuleCollider>();
                var collider = root.AddComponent<CapsuleCollider>();
                collider.center = nativeCollider.center;
                collider.radius = nativeCollider.radius;
                collider.height = nativeCollider.height;
                collider.direction = nativeCollider.direction;
                visual.SetActive(true);
                root.SetActive(true);
                component._animator.SetFloat("forward_speed", 0f);
                component._animator.SetFloat("sideway_speed", 0f);
                component._animator.SetFloat("turn_speed", 0f);
                component._animator.SetFloat("idle", 0f);
                return component;
            }
            catch { Object.DestroyImmediate(root); throw; }
        }

        private void Update()
        {
            if (_animator == null || ZNet.instance == null) return;
            double time = ZNet.instance.GetTimeSeconds();
            // Shared world clock, staggered between the two animals. Root position and
            // rotation never change; the native consume state returns smoothly to idle.
            long meal = (long)Math.Floor((time + _index * 4.5) / 13.0);
            if (meal != _lastMeal && Time.time >= _reactUntil)
            {
                _lastMeal = meal;
                _animator.SetTrigger("consume");
            }
            _animator.SetFloat("idle", Time.time < _reactUntil ? 2f : 0f, .35f, Time.deltaTime);
        }

        internal void React()
        {
            if (Time.time < _nextPet) return;
            _nextPet = Time.time + 2.5f;
            _reactUntil = Time.time + 2.2f;
            if (_petClips.Count > 0) _voice.PlayOneShot(_petClips[_index % _petClips.Count]);
        }

        private void PrepareVoice(EffectList effects)
        {
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 1f;
            _voice.minDistance = 1f;
            _voice.maxDistance = 18f;
            _voice.volume = .65f;
            if (effects?.m_effectPrefabs == null) return;
            foreach (var effect in effects.m_effectPrefabs)
            {
                if (effect?.m_prefab == null || !effect.m_enabled) continue;
                foreach (var sfx in effect.m_prefab.GetComponentsInChildren<ZSFX>(true))
                    if (sfx.m_audioClips != null)
                        foreach (var clip in sfx.m_audioClips)
                            if (clip != null && !_petClips.Contains(clip)) _petClips.Add(clip);
                var source = effect.m_prefab.GetComponentInChildren<AudioSource>(true);
                if (source != null) _voice.outputAudioMixerGroup = source.outputAudioMixerGroup;
            }
        }

        public string GetHoverName() => _index == 0 ? L.T("Inkblot", "Клякса") : L.T("Snout", "Пятачок");
        public float GetHoverOffset() => .7f;

        public string GetHoverText()
        {
            string key = L.Vanilla("$KEY_Use");
            if (string.IsNullOrEmpty(key) || key.StartsWith("$", StringComparison.Ordinal)) key = "E";
            return GetHoverName() + "\n[<color=yellow><b>" + key + "</b></color>] " + L.T("Pet", "Погладить");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user == null || user != Player.m_localPlayer || Time.time < _nextPet ||
                Vector3.Distance(user.transform.position, transform.position) > 3f) return false;
            _camp.Pet(_index);
            user.Message(MessageHud.MessageType.Center,
                GetHoverName() + L.T(" grunts happily.", " довольно похрюкивает."));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
