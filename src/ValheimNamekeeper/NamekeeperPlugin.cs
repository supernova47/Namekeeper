using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Utils;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("com.jotunn.jotunn", "2.30.2")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    public sealed class NamekeeperPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.supernova.valheimnamekeeper";
        public const string Name = "SuperNova Valheim Namekeeper";
        public const string Version = "0.3.10";

        internal static NamekeeperPlugin Instance;
        internal static ManualLogSource Log;
        internal static ConfigEntry<int> NameFee;
        internal static ConfigEntry<int> DescriptionFee;
        internal static ConfigEntry<int> SpawnDelay;
        internal static ConfigEntry<int> WoodCost;
        internal static ConfigEntry<int> FineWoodCost;
        internal static ConfigEntry<int> CoreWoodCost;
        internal static ConfigEntry<int> StoneCost;
        internal static ConfigEntry<int> ShopCoinCost;
        internal static ConfigEntry<int> NameLimit;
        internal static ConfigEntry<int> DescriptionLimit;
        internal static ConfigEntry<bool> NeedRoof;
        internal static ConfigEntry<bool> VisualEffects;
        internal static ConfigEntry<string> Language;
        internal static ConfigEntry<bool> DialogueEnabled;
        internal static ConfigEntry<bool> VoiceEnabled;
        internal static ConfigEntry<float> VoiceVolume;
        internal NamekeeperGui Ui;
        private Harmony _harmony;
        private bool _headless;
        private bool _refreshRecipesPending;
        private float _nextRecipeRefresh;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            NameFee = BindSynced("Services", "NamePrice", 25, "Coins for name change/reset.", new AcceptableValueRange<int>(0, 100000));
            DescriptionFee = BindSynced("Services", "DescriptionPrice", 25, "Coins for description change/reset.", new AcceptableValueRange<int>(0, 100000));
            SpawnDelay = BindSynced("Shop", "MerchantSpawnDelaySeconds", 60, "Delay after building the shop.", new AcceptableValueRange<int>(0, 86400));
            WoodCost = BindSynced("Building", "Wood", 20, "Shop recipe; restart required after changing.", new AcceptableValueRange<int>(0, 10000));
            FineWoodCost = BindSynced("Building", "FineWood", 10, "Fine wood from birch or oak; unlocks the shop during Black Forest progression.", new AcceptableValueRange<int>(0, 10000));
            CoreWoodCost = BindSynced("Building", "RoundLog", 10, "Core wood from pine; unlocks the shop during Black Forest progression.", new AcceptableValueRange<int>(0, 10000));
            StoneCost = BindSynced("Building", "Stone", 10, "Shop recipe; restart required after changing.", new AcceptableValueRange<int>(0, 10000));
            ShopCoinCost = BindSynced("Building", "Coins", 100, "Shop recipe; restart required after changing.", new AcceptableValueRange<int>(0, 10000));
            var servicePriceMigration = Config.Bind("Internal", "ServicePricesMigratedTo033", false,
                "Internal migration marker for the 0.3.3 service price defaults.");
            if (!servicePriceMigration.Value)
            {
                // Carry the new balance forward when an existing config still has the old defaults.
                if (NameFee.Value == 50) NameFee.Value = 25;
                if (DescriptionFee.Value == 50) DescriptionFee.Value = 25;
                servicePriceMigration.Value = true;
            }
            NameLimit = BindSynced("Text", "MaxNameLength", 40, "Maximum name length.", new AcceptableValueRange<int>(1, 256));
            DescriptionLimit = BindSynced("Text", "MaxDescriptionLength", 250, "Maximum description length.", new AcceptableValueRange<int>(1, 2000));
            NeedRoof = BindSynced("Shop", "RequireRoof", true, "Disable service without a roof above merchant.");
            VisualEffects = Config.Bind("Visual", "SuccessEffects", true, "Show gold particles after successful edit.");
            Language = Config.Bind("UI", "Language", "English", "English or Russian.");
            DialogueEnabled = Config.Bind("Dialogue", "Enabled", true, "Show local merchant lines above the NPC. Russian only when the game language is Russian; otherwise English.");
            VoiceEnabled = Config.Bind("Dialogue", "VoiceEnabled", true, "Play Hildir voice clips with merchant lines.");
            VoiceVolume = Config.Bind("Dialogue", "VoiceVolume", .7f,
                new ConfigDescription("Local merchant voice source volume (0..1); native audio mixer retained when available.", new AcceptableValueRange<float>(0f, 1f)));

            _headless = GUIManager.IsHeadless();
            if (!_headless) Ui = new NamekeeperGui();
            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;
            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(NamekeeperPlugin).Assembly);
            var locale = LocalizationManager.Instance.GetLocalization();
            locale.AddTranslation("English", new Dictionary<string, string>
            {
                { "sn_namekeeper_roof", "Namekeeper's Roof" },
                { "sn_namekeeper_roof_desc", "Replacement roof for the Namekeeper workshop. The original roof is included with the shop." },
                { "sn_namekeeper_shop", "Namekeeper's Shop" },
                { "sn_namekeeper_shop_desc", "A merchant personalizes the names and stories of your gear." }
            });
            locale.AddTranslation("Russian", new Dictionary<string, string>
            {
                { "sn_namekeeper_roof", "Крыша лавки имён" },
                { "sn_namekeeper_roof_desc", "Сменная крыша мастерской. При постройке лавки первая крыша уже включена в её стоимость." },
                { "sn_namekeeper_shop", "Лавка имён" },
                { "sn_namekeeper_shop_desc", "Торговка меняет имена и описания ваших вещей за монеты." }
            });
            var snapEnglish = new Dictionary<string, string>();
            var snapRussian = new Dictionary<string, string>();
            foreach (var anchor in WorkshopLayout.BodyAnchors())
            {
                string key = "sn_snap_" + anchor.Key;
                string place = anchor.X < 0 ? "left" : anchor.X > 0 ? "right" : "center";
                string depth = anchor.Z < 0 ? "front" : anchor.Z > 0 ? "back" : "middle";
                string ruPlace = anchor.X < 0 ? "слева" : anchor.X > 0 ? "справа" : "по центру";
                string ruDepth = anchor.Z < 0 ? "спереди" : anchor.Z > 0 ? "сзади" : "в середине";
                snapEnglish[key] = (anchor.Y > 0 ? "Roof mount: " : anchor.Y < 0 ? "Feet: " : "Floor: ") + place + ", " + depth;
                snapRussian[key] = (anchor.Y > 0 ? "Крепление крыши: " : anchor.Y < 0 ? "Низ опор: " : "Пол: ") + ruPlace + ", " + ruDepth;
            }
            snapEnglish["sn_snap_center"] = "Center";
            snapRussian["sn_snap_center"] = "Центр";
            foreach (var anchor in WorkshopLayout.RoofAnchors())
            {
                string key = "sn_snap_" + anchor.Key;
                if (snapEnglish.ContainsKey(key)) continue;
                snapEnglish[key] = "Roof corner " + (anchor.X < 0 ? "left" : "right") + ", " + (anchor.Z < 0 ? "front" : "back");
                snapRussian[key] = "Угол крыши " + (anchor.X < 0 ? "слева" : "справа") + ", " + (anchor.Z < 0 ? "спереди" : "сзади");
            }
            locale.AddTranslation("English", snapEnglish);
            locale.AddTranslation("Russian", snapRussian);
            PrefabManager.OnVanillaPrefabsAvailable += ShopRegistry.Register;
            Logger.LogInfo(Name + " " + Version + " initialized (headless=" + _headless + ")");
        }

        private ConfigEntry<T> BindSynced<T>(string section, string key, T defaultValue, string description,
            AcceptableValueBase acceptableValues = null) =>
            Config.BindConfig(section, key, defaultValue, description, true, null, acceptableValues);

        private void OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs args)
        {
            _refreshRecipesPending = true;
            _nextRecipeRefresh = 0f;
        }

        private void Update()
        {
            if (_refreshRecipesPending && Time.time >= _nextRecipeRefresh && ObjectDB.instance != null && ZNetScene.instance != null)
            {
                _nextRecipeRefresh = Time.time + 2f;
                try { _refreshRecipesPending = !ShopRegistry.RefreshSynchronizedRecipes(); }
                catch (Exception ex)
                {
                    _nextRecipeRefresh = Time.time + 5f;
                    Logger.LogError("Could not apply server-synchronized shop recipes: " + ex);
                }
            }
            if (Ui != null) Ui.Tick();
        }

        private void LateUpdate()
        {
            Ui?.LateTick();
        }

        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= ShopRegistry.Register;
            SynchronizationManager.OnConfigurationSynchronized -= OnConfigurationSynchronized;
            Ui?.Dispose();
            _harmony?.UnpatchSelf();
            if (Instance == this) Instance = null;
        }
    }
}
