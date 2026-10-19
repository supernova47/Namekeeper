using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    internal static class MerchantVisual
    {
        private const int FemalePlayerModel = 1;
        private const string MerchantHair = "Hair6"; // Long and loose
        private const string MerchantDress = "ArmorDress10"; // Simple undyed dress
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags PrivateInstanceMethod = BindingFlags.Instance | BindingFlags.NonPublic;

        internal static GameObject Create(ShopState shop)
        {
            var holder = new GameObject("NamekeeperMerchant");
            holder.transform.SetParent(shop.transform, false);
            // The player's visual root starts at the feet; align it with the shop floor
            // and leave a clear margin behind the rear edge of the counter.
            holder.transform.localPosition = ShopState.MerchantLocalOffset;
            holder.transform.localRotation = Quaternion.Euler(0, 180, 0);
            int interactionLayer = LayerMask.NameToLayer("piece");
            if (interactionLayer >= 0) holder.layer = interactionLayer;
            holder.SetActive(false);
            var interaction = holder.AddComponent<MerchantInteraction>();
            interaction.Shop = shop;
            var collider = holder.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, .8f, 0f);
            collider.height = 1.6f;
            collider.radius = .46f;
            collider.isTrigger = false;

            bool rendered = TryAttachPlayer(holder.transform);
            if (!rendered)
            {
                var figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                figure.name = "FallbackMerchantVisual";
                figure.transform.SetParent(holder.transform, false);
                figure.transform.localPosition = new Vector3(0f, .8f, 0f);
                figure.transform.localScale = new Vector3(.6f, .8f, .6f);
                var bodyCollider = figure.GetComponent<Collider>();
                if (bodyCollider != null) Object.Destroy(bodyCollider);
                var renderer = figure.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(.46f, .29f, .18f);
                NamekeeperPlugin.Log.LogWarning("Female player visual unavailable: using fallback NPC shape");
            }
            holder.SetActive(true);
            holder.AddComponent<MerchantDialogue>().Initialize(shop);
            return holder;
        }

        private static bool TryAttachPlayer(Transform holder)
        {
            if (ZNetScene.instance == null) return false;
            var prefab = ZNetScene.instance.GetPrefab("Player");
            if (prefab == null) return false;

            bool previous = ZNetView.m_forceDisableInit;
            GameObject clone = null;
            try
            {
                // Keep all cloned player/network scripts asleep until appearance is prepared.
                ZNetView.m_forceDisableInit = true;
                clone = Object.Instantiate(prefab, holder);
                clone.name = "NamekeeperPlayerVisual";
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;

                PreparePlayerAppearance(clone);
                StripGameplayComponents(clone);
                var animator = clone.GetComponentInChildren<Animator>(true);
                if (animator == null) throw new InvalidOperationException("Player prefab has no Animator");
                animator.applyRootMotion = false;
                animator.enabled = true;
                return true;
            }
            catch (Exception ex)
            {
                if (clone != null) Object.DestroyImmediate(clone);
                NamekeeperPlugin.Log.LogWarning("Female player appearance setup failed: " + ex);
                return false;
            }
            finally { ZNetView.m_forceDisableInit = previous; }
        }

        private static void PreparePlayerAppearance(GameObject clone)
        {
            var equipment = clone.GetComponent<VisEquipment>();
            if (equipment == null) throw new InvalidOperationException("Player prefab has no VisEquipment");

            int hairHash = StringExtensionMethods.GetStableHashCode(MerchantHair);
            int dressHash = StringExtensionMethods.GetStableHashCode(MerchantDress);
            Vector3 skinColor = new Vector3(.78f, .61f, .49f);
            Vector3 hairColor = new Vector3(.18f, .08f, .035f);

            // Configure deterministic NPC appearance before VisEquipment's first update.
            SetField(equipment, "m_randomModelIndex", false);
            SetField(equipment, "m_npcHairChance", 0f);
            SetField(equipment, "m_npcBeardChance", 0f);
            SetField(equipment, "m_modelIndex", FemalePlayerModel);
            SetField(equipment, "m_currentModelIndex", -1);
            SetField(equipment, "m_skinColor", skinColor);
            SetField(equipment, "m_hairColor", hairColor);

            // Run the vanilla visual setup once while the clone is under an inactive shop.
            InvokePrivate(equipment, "Awake");
            equipment.SetModel(FemalePlayerModel);
            equipment.SetSkinColor(skinColor);
            equipment.SetHairColor(hairColor);
            equipment.SetHairItem(hairHash);
            equipment.SetBeardItem(0);
            equipment.SetChestItem(dressHash);
            equipment.SetLegItem(0);
            equipment.CustomUpdate(0f, 0f);
            // The first pass creates the hair instance; the second tints its renderers.
            equipment.CustomUpdate(0f, 0f);

            // It was never enabled in the inactive shop, so it was not added to the game's updater.
            // Remove the visual setup component after it has attached hair and clothing to the rig.
            equipment.enabled = false;
            InvokePrivate(equipment, "OnDisable");
            Object.DestroyImmediate(equipment);
        }

        private static void SetField(VisEquipment target, string name, object value)
        {
            FieldInfo field = typeof(VisEquipment).GetField(name, InstanceFields);
            if (field == null) throw new MissingFieldException(typeof(VisEquipment).FullName, name);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(VisEquipment target, string name)
        {
            MethodInfo method = typeof(VisEquipment).GetMethod(name, PrivateInstanceMethod);
            if (method == null) throw new MissingMethodException(typeof(VisEquipment).FullName, name);
            method.Invoke(target, null);
        }

        private static void StripGameplayComponents(GameObject clone)
        {
            foreach (var script in clone.GetComponentsInChildren<MonoBehaviour>(true))
                if (script != null) Object.DestroyImmediate(script);
            foreach (var col in clone.GetComponentsInChildren<Collider>(true))
                if (col != null) Object.DestroyImmediate(col);
            foreach (var rigid in clone.GetComponentsInChildren<Rigidbody>(true))
                if (rigid != null) Object.DestroyImmediate(rigid);
            foreach (var audio in clone.GetComponentsInChildren<AudioSource>(true))
            {
                audio.Stop();
                Object.DestroyImmediate(audio);
            }
            foreach (var particles in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Object.DestroyImmediate(particles);
            }
            foreach (var renderer in clone.GetComponentsInChildren<ParticleSystemRenderer>(true))
                if (renderer != null) Object.DestroyImmediate(renderer);
            foreach (var light in clone.GetComponentsInChildren<Light>(true))
                if (light != null) Object.DestroyImmediate(light);
        }
    }

    // The merchant is a local representation of one networked shop, not a separate ZDO entity.
    internal sealed class MerchantInteraction : MonoBehaviour, Hoverable, Interactable
    {
        internal ShopState Shop;

        public string GetHoverName() => L.T("Namekeeper", "Хранительница имён");
        public string GetHoverText()
        {
            if (Shop == null || (NamekeeperPlugin.Instance != null &&
                NamekeeperPlugin.Instance.Ui != null && NamekeeperPlugin.Instance.Ui.IsOpen)) return "";
            string heading = "<color=#F1D7A0><b>" + GetHoverName() + "</b></color>";
            if (!Shop.CanServe(out string message))
                return heading + "\n<color=#D6A97A>" + message + "</color>";
            // $KEY_Use does not resolve on some recent Valheim builds.
            // Avoid exposing the raw localization token in the hover prompt.
            string key = L.Vanilla("$KEY_Use");
            if (string.IsNullOrEmpty(key) || key.StartsWith("$", StringComparison.Ordinal)) key = "E";
            return heading + "\n[<color=#EFC36B><b>" + key + "</b></color>] " +
                L.T("Personalize item", "Изменить предмет");
        }
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || Shop == null || user != Player.m_localPlayer) return false;
            if (Vector3.Distance(user.transform.position, transform.position) > 3.5f) return false;
            if (!Shop.CanServe(out string message))
            {
                Shop.Speak(DialogueCue.NoRoof);
                if (MessageHud.instance != null)
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return false;
            }
            NamekeeperPlugin.Instance?.Ui?.Open(Shop);
            return true;
        }
        public float GetHoverOffset() => 0f;
        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (user == Player.m_localPlayer) Shop?.Speak(DialogueCue.ItemUnavailable);
            return false;
        }
    }
}
