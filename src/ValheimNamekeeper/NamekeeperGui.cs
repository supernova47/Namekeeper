using System;
using System.Collections.Generic;
using System.Reflection;
using Jotunn.Managers;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    internal sealed class NamekeeperGui : IDisposable
    {
        private ShopState _shop;
        private NativeHud _hud;
        private bool _open, _submitting, _inputBlocked, _cursorSaved;
        private CursorLockMode _oldLock;
        private bool _oldVisible;
        private float _suppressInputUntil, _lastSubmit, _nextRefresh;
        private static readonly FieldInfo OtherInputRequests = typeof(GUIManager).GetField("InputBlockRequests",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        internal ItemDrop.ItemData SelectedItem { get; private set; }
        internal string NameDraft { get; private set; } = "";
        internal string DescriptionDraft { get; private set; } = "";
        internal bool ResetName { get; private set; }
        internal bool ResetDescription { get; private set; }
        internal string Filter { get; private set; } = "";
        internal string Notice { get; private set; } = "";
        internal bool IsOpen => _open;
        internal bool InputSuppressed => _open || Time.unscaledTime < _suppressInputUntil;
        internal bool NameChanged => SelectedItem != null && ItemText.NameChanges(SelectedItem, NameDraft.Trim(), ResetName);
        internal bool DescriptionChanged => SelectedItem != null && ItemText.DescriptionChanges(SelectedItem, DescriptionDraft, ResetDescription);
        internal int TotalCost => (NameChanged ? NamekeeperPlugin.NameFee.Value : 0) +
            (DescriptionChanged ? NamekeeperPlugin.DescriptionFee.Value : 0);
        internal int Coins => Player.m_localPlayer?.GetInventory()?.CountItems("$item_coins", -1, true) ?? 0;
        internal bool CanSubmit => SelectedItem != null && !_submitting && (NameChanged || DescriptionChanged);

        internal void Open(ShopState shop)
        {
            if (shop == null || Player.m_localPlayer == null) return;
            if (!shop.CanServe(out string reason)) { Say(reason); Speak(shop, DialogueCue.NoRoof); return; }
            if (_open) Close(false);
            try
            {
                _hud = _hud != null && _hud.IsAlive ? _hud : new NativeHud(this);
                _oldLock = Cursor.lockState;
                _oldVisible = Cursor.visible;
                _cursorSaved = true;
                _shop = shop;
                _open = true;
                _suppressInputUntil = 0;
                SelectedItem = null;
                NameDraft = DescriptionDraft = Filter = Notice = "";
                ResetName = ResetDescription = false;
                SetGameplayInputBlocked(true);
                _hud.Show();
                KeepCursor();
                Speak(shop, EligibleItems().Count == 0 ? DialogueCue.NoEligibleItems : DialogueCue.Open);
            }
            catch (Exception ex)
            {
                NamekeeperPlugin.Log?.LogError("Could not open native shop UI: " + ex);
                Close(false);
                Say(L.T("Could not open the shop. Check the log.", "Не удалось открыть лавку. Проверьте журнал."));
                Speak(shop, DialogueCue.Failure);
            }
        }

        internal void CloseIfShop(ShopState shop) { if (_shop == shop) Close(false); }
        internal void Close() => Close(true);
        private void Close(bool farewell)
        {
            ShopState previous = _shop;
            bool wasOpen = _open;
            _open = false;
            _shop = null;
            _suppressInputUntil = Time.unscaledTime + .16f;
            SelectedItem = null;
            try { _hud?.Hide(); }
            finally
            {
                SetGameplayInputBlocked(false);
                if (_cursorSaved)
                {
                    if (HasOtherInputRequests()) KeepCursor();
                    else { Cursor.visible = _oldVisible; Cursor.lockState = _oldLock; }
                    _cursorSaved = false;
                }
            }
            if (wasOpen && farewell) Speak(previous, DialogueCue.Goodbye);
        }

        internal void Tick()
        {
            if (!_open) return;
            var player = Player.m_localPlayer;
            if (player == null || player.IsDead() || _shop == null || _hud == null || !_hud.IsAlive)
            { Close(false); return; }
            if (Vector3.Distance(player.transform.position, _shop.transform.position) > 5.5f)
            { Speak(_shop, DialogueCue.TooFar); Close(false); return; }
            if (!_shop.CanServe(out string _)) { Speak(_shop, DialogueCue.NoRoof); Close(false); return; }
            KeepCursor();
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .25f;
            try
            {
                if (SelectedItem != null && !HasItem(player.GetInventory(), SelectedItem))
                {
                    SelectedItem = null;
                    Say(L.T("Selected item is no longer in your inventory.", "Выбранного предмета больше нет в инвентаре."));
                    Speak(_shop, DialogueCue.ItemUnavailable);
                }
                _hud.Refresh();
            }
            catch (Exception ex)
            {
                NamekeeperPlugin.Log?.LogWarning("Shop UI refresh failed; closing safely: " + ex);
                Close(false);
            }
        }

        internal void LateTick() { if (_open) KeepCursor(); }
        private static bool HasOtherInputRequests()
        {
            try { return OtherInputRequests?.GetValue(null) is int requests && requests > 0; }
            catch { return false; }
        }
        private static void KeepCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void SetGameplayInputBlocked(bool blocked)
        {
            if (_inputBlocked == blocked) return;
            try { GUIManager.BlockInput(blocked); _inputBlocked = blocked; }
            catch (Exception ex) { NamekeeperPlugin.Log?.LogWarning("GUIManager.BlockInput failed: " + ex); }
        }

        internal List<ItemDrop.ItemData> EligibleItems()
        {
            var result = new List<ItemDrop.ItemData>();
            var inventory = Player.m_localPlayer?.GetInventory();
            if (inventory == null) return result;
            foreach (var item in inventory.GetAllItems())
                if (IsEligible(item) && (string.IsNullOrEmpty(Filter) ||
                    ItemText.GetName(item).IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    L.OriginalName(item).IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0)) result.Add(item);
            return result;
        }
        internal void SetFilter(string value) { Filter = value ?? ""; _hud.Refresh(); }
        internal void SetName(string value) { NameDraft = value ?? ""; _hud.RefreshEditor(); }
        internal void SetDescription(string value) { DescriptionDraft = value ?? ""; _hud.RefreshEditor(); }
        internal void FinishNameEdit() { if (NameChanged && !ResetName) Speak(_shop, DialogueCue.NameEdited); }
        internal void FinishDescriptionEdit() { if (DescriptionChanged && !ResetDescription) Speak(_shop, DialogueCue.DescriptionEdited); }
        internal void ToggleNameReset()
        {
            ResetName = !ResetName;
            Speak(_shop, !ResetName ? DialogueCue.CancelReset : NameChanged ? DialogueCue.ResetName : DialogueCue.NoChanges);
            _hud.RefreshEditor();
        }
        internal void ToggleDescriptionReset()
        {
            ResetDescription = !ResetDescription;
            Speak(_shop, !ResetDescription ? DialogueCue.CancelReset : DescriptionChanged ? DialogueCue.ResetDescription : DialogueCue.NoChanges);
            _hud.RefreshEditor();
        }
        internal void SwitchLanguage()
        {
            NamekeeperPlugin.Language.Value = L.Ru ? "English" : "Russian";
            NamekeeperPlugin.Instance.Config.Save();
            _hud.RefreshLanguage();
        }
        internal void Select(ItemDrop.ItemData item)
        {
            if (!IsEligible(item) || !HasItem(Player.m_localPlayer?.GetInventory(), item)) return;
            SelectedItem = item;
            NameDraft = ItemText.GetName(item);
            DescriptionDraft = ItemText.GetDescription(item);
            ResetName = ResetDescription = false;
            Notice = "";
            _hud.Refresh();
            _hud.FocusName();
            Speak(_shop, DialogueCue.SelectItem);
        }

        internal void Commit()
        {
            if (!_open || _submitting || Time.realtimeSinceStartup - _lastSubmit < .5f) return;
            _submitting = true;
            _lastSubmit = Time.realtimeSinceStartup;
            bool committed = false;
            ShopState shop = _shop;
            try
            {
                var player = Player.m_localPlayer;
                var item = SelectedItem;
                if (player == null || shop == null || Vector3.Distance(player.transform.position, shop.transform.position) > 5f)
                { Reject(L.T("Too far from the shop.", "Слишком далеко от лавки."), DialogueCue.TooFar); return; }
                if (!shop.CanServe(out string reason)) { Reject(reason, DialogueCue.NoRoof); return; }
                var inventory = player.GetInventory();
                if (!IsEligible(item) || !HasItem(inventory, item))
                { Reject(L.T("Item is unavailable.", "Предмет недоступен."), DialogueCue.ItemUnavailable); return; }
                string name = NameDraft.Trim(), description = DescriptionDraft;
                bool rename = ItemText.NameChanges(item, name, ResetName);
                bool redescribe = ItemText.DescriptionChanges(item, description, ResetDescription);
                if (!rename && !redescribe) { Reject(L.T("No changes.", "Нет изменений."), DialogueCue.NoChanges); return; }
                if (rename && !ResetName && (name.Length == 0 || !ItemText.Validate(name, NamekeeperPlugin.NameLimit.Value, false, out reason)))
                { Reject(reason ?? L.T("Name must not be empty.", "Имя не должно быть пустым."), DialogueCue.InvalidText); return; }
                if (redescribe && !ResetDescription && !ItemText.Validate(description, NamekeeperPlugin.DescriptionLimit.Value, true, out reason))
                { Reject(reason, DialogueCue.InvalidText); return; }
                int cost = (rename ? NamekeeperPlugin.NameFee.Value : 0) + (redescribe ? NamekeeperPlugin.DescriptionFee.Value : 0);
                if (Coins < cost) { Reject(L.T("Not enough coins.", "Недостаточно монет."), DialogueCue.NotEnoughCoins); return; }
                bool resetting = (rename && ResetName) || (redescribe && ResetDescription);
                InventoryTransaction.Execute(inventory, item, cost, () =>
                    ItemText.Apply(item, name, description, rename, redescribe, ResetName, ResetDescription));
                committed = true;
                NameDraft = ItemText.GetName(item);
                DescriptionDraft = ItemText.GetDescription(item);
                ResetName = ResetDescription = false;
                bool refreshed = InventoryNotifier.Notify(inventory);
                Say(refreshed ? L.T("Personalization complete.", "Изменение выполнено.") :
                    L.T("Personalization saved. Reopen inventory to refresh it; check the log.",
                        "Изменение сохранено. Переоткройте инвентарь; подробности в журнале."));
                Speak(shop, resetting ? DialogueCue.SuccessReset : rename && redescribe ? DialogueCue.SuccessBoth :
                    rename ? DialogueCue.SuccessName : DialogueCue.SuccessDescription);
                if (NamekeeperPlugin.VisualEffects.Value && shop != null)
                    try { NamekeeperEffects.Play(shop.CounterPoint); }
                    catch (Exception ex) { NamekeeperPlugin.Log?.LogWarning("Purchase completed; cosmetic effect failed: " + ex); }
            }
            catch (Exception ex)
            {
                NamekeeperPlugin.Log?.LogError((committed ? "Purchase completed; presentation failed: " : "Transaction failed: ") + ex);
                Say(committed ? L.T("Personalization saved. Reopen the shop.", "Изменение сохранено. Переоткройте лавку.") :
                    L.T("Could not complete transaction. Check the log.", "Ошибка операции. Проверьте журнал."));
                if (!committed) Speak(shop, DialogueCue.Failure);
            }
            finally
            {
                _submitting = false;
                try { if (_hud != null && _hud.IsAlive) _hud.RefreshEditor(); }
                catch (Exception ex) { NamekeeperPlugin.Log?.LogWarning("Post-purchase UI refresh failed: " + ex); }
            }
        }

        private void Reject(string message, DialogueCue cue) { Say(message); Speak(_shop, cue); }
        private void Say(string message)
        {
            Notice = message ?? "";
            if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, Notice);
        }
        private static void Speak(ShopState shop, DialogueCue cue)
        {
            try { if (shop != null) shop.Speak(cue); }
            catch (Exception ex) { NamekeeperPlugin.Log?.LogWarning("Merchant presentation failed: " + ex.Message); }
        }
        private static bool IsEligible(ItemDrop.ItemData item) => item?.m_shared != null && item.m_shared.m_maxStackSize == 1 && item.m_stack == 1;
        private static bool HasItem(Inventory inventory, ItemDrop.ItemData item)
        {
            if (inventory == null || item == null) return false;
            foreach (var test in inventory.GetAllItems()) if (ReferenceEquals(test, item)) return true;
            return false;
        }
        public void Dispose() { Close(false); _hud?.Dispose(); _hud = null; }
    }

    internal static class InventoryNotifier
    {
        internal static bool Notify(Inventory inventory)
        {
            try { InventoryChangeDispatcher.Notify(inventory); return true; }
            catch (Exception ex) { NamekeeperPlugin.Log?.LogWarning("Purchase committed; inventory notification failed: " + ex); return false; }
        }
    }
}
