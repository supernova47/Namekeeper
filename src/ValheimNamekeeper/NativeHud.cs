using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    // Real uGUI controls styled by the installed Jotunn GUIManager, not an IMGUI skin.
    internal sealed class NativeHud : IDisposable
    {
        private static readonly Vector2 TopLeft = new Vector2(0, 1);
        private static readonly Color Gold = new Color(1f, .78f, .36f);
        private readonly NamekeeperGui _owner;
        private readonly List<Action> _translations = new List<Action>();
        private readonly List<Row> _rows = new List<Row>();
        private GameObject _root, _left, _right, _form, _emptyEditor;
        private RectTransform _rootRect;
        private Text _title, _subtitle, _coins, _itemName, _quality, _nameCount, _descriptionCount;
        private Text _previewName, _previewDescription, _total, _notice, _emptyList;
        private Text _listHeading, _listHint;
        private Image _selectedIcon;
        private InputField _filter, _name, _description;
        private Button _language, _close, _resetName, _resetDescription, _confirm;
        private ScrollRect _inventoryScroll, _editorScroll;
        private bool _russian;
        private float _width, _height;
        internal bool IsAlive => _root != null;

        private sealed class Row
        {
            internal ItemDrop.ItemData Item;
            internal Button Button;
            internal Text Caption;
            internal Image Icon;
            internal ColorBlock Colors;
        }

        internal NativeHud(NamekeeperGui owner)
        {
            _owner = owner;
            try { Build(); }
            catch { if (_root != null) Object.Destroy(_root); throw; }
        }

        private void Build()
        {
            var manager = GUIManager.Instance;
            if (manager == null || GUIManager.CustomGUIBack == null)
                throw new InvalidOperationException("Jotunn custom GUI is not ready");
            // Native NPC speech remains above this panel in Valheim's normal GUI canvas.
            _root = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIBack.transform,
                new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(24, 0), 940, 760, false);
            _root.name = "SN_Namekeeper_NativeHUD";
            _rootRect = _root.GetComponent<RectTransform>();
            _rootRect.pivot = new Vector2(0, .5f);
            _root.SetActive(false);
            _title = Label(_root.transform, "NAMEKEEPER'S SHOP", "ЛАВКА ИМЁН", 28, Gold);
            _title.font = GUIManager.Instance.AveriaSerifBold;
            _subtitle = Label(_root.transform, "Give your equipment a name and a story.", "Подари своей вещи имя и историю.", 17);
            _language = MakeButton(_root.transform, "EN / RU", "EN / RU", _owner.SwitchLanguage);
            _close = MakeButton(_root.transform, "×", "×", _owner.Close);
            _left = Box(_root.transform, "Equipment");
            _right = Box(_root.transform, "Personalization");
            _listHeading = Label(_left.transform, "YOUR EQUIPMENT", "ВАШИ ПРЕДМЕТЫ", 20, Gold);
            _filter = MakeInput(_left.transform, false);
            _filter.characterLimit = 100;
            if (_filter.placeholder is Text searchHint)
                _translations.Add(() => searchHint.text = L.T("Search items...", "Поиск предметов..."));
            _filter.onValueChanged.AddListener(_owner.SetFilter);
            _listHint = Label(_left.transform, "Single items only · maximum stack 1", "Только отдельные вещи · макс. стак 1", 14);
            _inventoryScroll = MakeScroll(_left.transform);
            _emptyList = Label(_inventoryScroll.content, "No matching items", "Подходящих предметов нет", 17);
            _coins = DynamicLabel(_left.transform, 19, Gold);
            _editorScroll = MakeScroll(_right.transform);
            _form = new GameObject("EditorForm", typeof(RectTransform));
            _form.layer = GUIManager.UILayer;
            _form.transform.SetParent(_editorScroll.content, false);
            _emptyEditor = Label(_editorScroll.content,
                "Choose an item on the left.\n\nYour item stays in your inventory while I work.",
                "Выбери вещь слева.\n\nВо время работы она остаётся в твоём инвентаре.", 21).gameObject;
            _selectedIcon = MakeImage(_form.transform, "SelectedIcon");
            _itemName = DynamicLabel(_form.transform, 22, Gold);
            _quality = DynamicLabel(_form.transform, 16);
            Place(Label(_form.transform, "NAME", "ИМЯ", 18, Gold), 0, 88, 300, 26);
            _name = MakeInput(_form.transform, false);
            _name.onValueChanged.AddListener(_owner.SetName);
            _name.onEndEdit.AddListener(_ => _owner.FinishNameEdit());
            _nameCount = DynamicLabel(_form.transform, 14);
            _resetName = MakeButton(_form.transform, "", "", _owner.ToggleNameReset);
            Place(Label(_form.transform, "DESCRIPTION", "ОПИСАНИЕ", 18, Gold), 0, 226, 300, 26);
            _description = MakeInput(_form.transform, true);
            _description.onValueChanged.AddListener(_owner.SetDescription);
            _description.onEndEdit.AddListener(_ => _owner.FinishDescriptionEdit());
            _descriptionCount = DynamicLabel(_form.transform, 14);
            _resetDescription = MakeButton(_form.transform, "", "", _owner.ToggleDescriptionReset);
            Place(Label(_form.transform, "PREVIEW", "ПРЕДПРОСМОТР", 18, Gold), 0, 464, 300, 26);
            _previewName = DynamicLabel(_form.transform, 21, Gold);
            _previewDescription = DynamicLabel(_form.transform, 17);
            _total = DynamicLabel(_right.transform, 21, Gold);
            _notice = DynamicLabel(_right.transform, 15, Gold);
            _confirm = MakeButton(_right.transform, "", "", _owner.Commit);
            RefreshLanguage();
            Reflow();
        }

        internal void Show()
        {
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _inventoryScroll.verticalNormalizedPosition = 1;
            _editorScroll.verticalNormalizedPosition = 1;
            _filter.SetTextWithoutNotify("");
            Refresh();
        }
        internal void Hide()
        {
            if (_root == null) return;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(_root.transform))
                EventSystem.current.SetSelectedGameObject(null);
            _name.DeactivateInputField();
            _description.DeactivateInputField();
            _filter.DeactivateInputField();
            _root.SetActive(false);
        }
        internal void FocusName()
        {
            _editorScroll.verticalNormalizedPosition = 1;
            if (_name != null) { _name.Select(); _name.ActivateInputField(); }
        }
        internal void RefreshLanguage()
        {
            _russian = L.Ru;
            foreach (var translate in _translations) translate();
            Refresh();
        }
        internal void Refresh()
        {
            if (!IsAlive) return;
            if (_russian != L.Ru) { RefreshLanguage(); return; }
            Reflow();
            RefreshInventory();
            RefreshEditor();
        }

        private void RefreshInventory()
        {
            List<ItemDrop.ItemData> items = _owner.EligibleItems();
            bool rebuild = items.Count != _rows.Count;
            if (!rebuild)
                for (int i = 0; i < items.Count; i++)
                    if (!ReferenceEquals(items[i], _rows[i].Item)) { rebuild = true; break; }
            if (rebuild)
            {
                foreach (var row in _rows) { row.Button.gameObject.SetActive(false); Object.Destroy(row.Button.gameObject); }
                _rows.Clear();
                foreach (var item in items)
                {
                    var captured = item;
                    var button = MakeButton(_inventoryScroll.content, "", "", () => _owner.Select(captured), false);
                    var caption = button.GetComponentInChildren<Text>();
                    caption.alignment = TextAnchor.MiddleLeft;
                    caption.fontSize = 16;
                    var textRect = caption.rectTransform;
                    textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(52, 2); textRect.offsetMax = new Vector2(-6, -2);
                    var icon = MakeImage(button.transform, "Icon");
                    Place(icon, 6, 5, 36, 36);
                    _rows.Add(new Row { Item = item, Button = button, Caption = caption, Icon = icon, Colors = button.colors });
                }
            }
            float width = Mathf.Max(180, _inventoryScroll.viewport.rect.width - 22);
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                Place(row.Button, 0, i * 50, width, 46);
                row.Caption.text = ItemText.GetName(row.Item);
                SetIcon(row.Icon, row.Item);
                var colors = row.Colors;
                if (ReferenceEquals(row.Item, _owner.SelectedItem)) colors.normalColor = new Color(1f, .72f, .32f);
                row.Button.colors = colors;
            }
            _emptyList.gameObject.SetActive(items.Count == 0);
            Place(_emptyList, 2, 8, width, 70);
            _inventoryScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(80, _rows.Count * 50));
            _coins.text = L.T("Your coins: ", "Ваши монеты: ") + _owner.Coins;
        }

        internal void RefreshEditor()
        {
            if (!IsAlive) return;
            var item = _owner.SelectedItem;
            _form.SetActive(item != null);
            _emptyEditor.SetActive(item == null);
            float width = Mathf.Max(200, _editorScroll.viewport.rect.width - 22);
            if (item != null)
            {
                _itemName.text = ItemText.GetName(item);
                _quality.text = L.T("Quality: ", "Качество: ") + item.m_quality;
                SetIcon(_selectedIcon, item);
                SetField(_name, _owner.ResetName ? L.OriginalName(item) : _owner.NameDraft, _owner.ResetName);
                SetField(_description, _owner.ResetDescription ? L.OriginalDescription(item) : _owner.DescriptionDraft, _owner.ResetDescription);
                Caption(_resetName, _owner.ResetName ? L.T("Cancel name reset", "Отменить сброс имени") :
                    L.T("Restore original name", "Вернуть оригинальное имя") + " · " + NamekeeperPlugin.NameFee.Value);
                Caption(_resetDescription, _owner.ResetDescription ? L.T("Cancel description reset", "Отменить сброс описания") :
                    L.T("Restore original description", "Вернуть оригинальное описание") + " · " + NamekeeperPlugin.DescriptionFee.Value);
                _nameCount.text = _owner.ResetName ? L.T("Original name will be restored", "Будет возвращено оригинальное имя") :
                    _owner.NameDraft.Trim().Length + " / " + NamekeeperPlugin.NameLimit.Value + " · " + NamekeeperPlugin.NameFee.Value + L.T(" coins", " монет");
                _descriptionCount.text = _owner.ResetDescription ? L.T("Original description will be restored", "Будет возвращено оригинальное описание") :
                    _owner.DescriptionDraft.Length + " / " + NamekeeperPlugin.DescriptionLimit.Value + " · " + NamekeeperPlugin.DescriptionFee.Value + L.T(" coins", " монет");
                _previewName.text = _owner.ResetName ? L.OriginalName(item) : _owner.NameDraft.Trim();
                _previewName.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                float nameHeight = Mathf.Max(38, _previewName.preferredHeight + 4);
                Place(_previewName, 0, 496, width, nameHeight);
                _previewDescription.supportRichText = _owner.ResetDescription;
                _previewDescription.text = _owner.ResetDescription ? L.OriginalDescription(item) : _owner.DescriptionDraft;
                _previewDescription.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                float previewHeight = Mathf.Max(90, _previewDescription.preferredHeight + 8);
                Place(_previewDescription, 0, 506 + nameHeight, width, previewHeight);
                _editorScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 522 + nameHeight + previewHeight);
            }
            else _editorScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 180);
            _total.text = L.T("Total: ", "Итого: ") + _owner.TotalCost + L.T(" coins", " монет");
            _total.color = _owner.Coins < _owner.TotalCost ? new Color(1f, .4f, .3f) : Gold;
            _notice.text = _owner.Notice;
            Caption(_confirm, _owner.CanSubmit ? L.T("CONFIRM AND PAY", "ПОДТВЕРДИТЬ И ОПЛАТИТЬ") :
                item == null ? L.T("SELECT AN ITEM", "ВЫБЕРИТЕ ПРЕДМЕТ") : L.T("NO CHANGES", "НЕТ ИЗМЕНЕНИЙ"));
            _confirm.interactable = _owner.CanSubmit;
        }

        private void Reflow()
        {
            var parent = _rootRect.parent as RectTransform;
            var layout = new HudLayout(parent != null && parent.rect.width > 1 ? parent.rect.width : Screen.width,
                parent != null && parent.rect.height > 1 ? parent.rect.height : Screen.height);
            if (Mathf.Approximately(layout.Width, _width) && Mathf.Approximately(layout.Height, _height) &&
                Mathf.Approximately(_rootRect.localScale.x, layout.Scale)) return;
            _width = layout.Width; _height = layout.Height;
            _rootRect.sizeDelta = new Vector2(_width, _height);
            _rootRect.localScale = Vector3.one * layout.Scale;
            _rootRect.anchoredPosition = new Vector2(layout.LeftInset, 0);
            float leftWidth = Mathf.Clamp(_width * .31f, 240, 292);
            float rightWidth = _width - leftWidth - 64;
            float bodyHeight = _height - 132;
            Place(_title, 24, 16, _width - 220, 46);
            Place(_subtitle, 24, 66, _width - 48, 36);
            Place(_language, _width - 184, 24, 100, 32);
            Place(_close, _width - 70, 24, 42, 32);
            Place(_left.GetComponent<RectTransform>(), 24, 110, leftWidth, bodyHeight);
            Place(_right.GetComponent<RectTransform>(), 40 + leftWidth, 110, rightWidth, bodyHeight);
            Place(_filter, 12, 44, leftWidth - 24, 36);
            Place(_listHeading, 12, 12, leftWidth - 24, 26);
            Place(_listHint, 12, 86, leftWidth - 24, 36);
            Place(_inventoryScroll, 12, 130, leftWidth - 24, bodyHeight - 194);
            Bottom(_coins.rectTransform, 12, 16, leftWidth - 24, 32);
            Place(_editorScroll, 12, 12, rightWidth - 24, bodyHeight - 158);
            Bottom(_total.rectTransform, 12, 108, rightWidth - 24, 30);
            Bottom(_notice.rectTransform, 12, 56, rightWidth - 24, 48);
            Bottom(_confirm.GetComponent<RectTransform>(), 12, 10, rightWidth - 24, 40);
            float formWidth = rightWidth - 64;
            Place(_form.GetComponent<RectTransform>(), 0, 0, formWidth, 680);
            Place(_emptyEditor.GetComponent<RectTransform>(), 2, 16, formWidth, 160);
            Place(_selectedIcon, 0, 0, 48, 48);
            Place(_itemName, 60, 0, formWidth - 60, 50);
            Place(_quality, 60, 54, formWidth - 60, 24);
            Place(_name, 0, 118, formWidth, 36);
            Place(_nameCount, 0, 158, formWidth, 20);
            Place(_resetName, 0, 184, formWidth, 32);
            Place(_description, 0, 256, formWidth, 126);
            Place(_descriptionCount, 0, 386, formWidth, 20);
            Place(_resetDescription, 0, 412, formWidth, 32);
            Place(_previewName, 0, 496, formWidth, 38);
        }

        private Text Label(Transform parent, string en, string ru, int size, Color? color = null)
        {
            Text text = DynamicLabel(parent, size, color);
            _translations.Add(() => { if (text != null) text.text = L.T(en, ru); });
            text.text = L.T(en, ru);
            return text;
        }
        private static Text DynamicLabel(Transform parent, int size, Color? color = null)
        {
            var go = GUIManager.Instance.CreateText("", parent, TopLeft, TopLeft, Vector2.zero,
                GUIManager.Instance.AveriaSerif, size, color ?? GUIManager.Instance.ValheimBeige, false, Color.black, 100, 30, false);
            var text = go.GetComponent<Text>();
            text.raycastTarget = false;
            text.supportRichText = false;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private Button MakeButton(Transform parent, string en, string ru, Action click, bool translate = true)
        {
            var button = GUIManager.Instance.CreateButton(L.T(en, ru), parent, TopLeft, TopLeft, Vector2.zero, 180, 34).GetComponent<Button>();
            button.onClick.AddListener(() => click());
            var caption = button.GetComponentInChildren<Text>();
            caption.fontSize = 16;
            caption.supportRichText = false;
            if (translate) _translations.Add(() => { if (button != null) Caption(button, L.T(en, ru)); });
            return button;
        }
        private static InputField MakeInput(Transform parent, bool multiline)
        {
            var input = GUIManager.Instance.CreateInputField(parent, TopLeft, TopLeft, Vector2.zero,
                InputField.ContentType.Standard, "", 17, 250, multiline ? 126 : 36).GetComponent<InputField>();
            input.characterLimit = 0; // Validate before billing; never truncate an original description.
            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            input.textComponent.supportRichText = false;
            input.textComponent.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            var textRect = input.textComponent.rectTransform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6); textRect.offsetMax = new Vector2(-10, -6);
            return input;
        }
        private static ScrollRect MakeScroll(Transform parent)
        {
            // Jotunn's CreateScrollView returns a canvas wrapper with automatic layout.
            // Use Unity's direct ScrollRect hierarchy and Jotunn's native styling instead.
            var go = DefaultControls.CreateScrollView(GUIManager.Instance.ValheimControlResources);
            go.transform.SetParent(parent, false);
            var scroll = go.GetComponent<ScrollRect>();
            if (scroll == null || scroll.content == null || scroll.viewport == null)
                throw new InvalidOperationException("Native scroll view hierarchy is unavailable");
            GUIManager.Instance.ApplyScrollRectStyle(scroll);
            if (scroll.horizontalScrollbar != null)
            {
                scroll.horizontalScrollbar.gameObject.SetActive(false);
                scroll.horizontalScrollbar = null;
            }
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            if (scroll.verticalScrollbar != null)
            {
                var scrollbar = scroll.verticalScrollbar.GetComponent<RectTransform>();
                scrollbar.anchorMin = new Vector2(1, 0); scrollbar.anchorMax = Vector2.one;
                scrollbar.pivot = new Vector2(1, .5f); scrollbar.anchoredPosition = Vector2.zero;
                scrollbar.sizeDelta = new Vector2(12, 0);
            }
            scroll.viewport.anchorMin = Vector2.zero; scroll.viewport.anchorMax = Vector2.one;
            scroll.viewport.offsetMin = Vector2.zero; scroll.viewport.offsetMax = new Vector2(-16, 0);
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            var content = scroll.content;
            content.anchorMin = TopLeft; content.anchorMax = new Vector2(1, 1);
            content.pivot = TopLeft; content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-20, 680);
            return scroll;
        }
        private static GameObject Box(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = GUIManager.UILayer;
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(.045f, .035f, .025f, .86f);
            return go;
        }
        private static Image MakeImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = GUIManager.UILayer;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.raycastTarget = false; image.preserveAspect = true;
            return image;
        }
        private static void SetIcon(Image image, ItemDrop.ItemData item)
        {
            var icons = item?.m_shared?.m_icons;
            image.sprite = icons != null && icons.Length > 0 ? icons[Mathf.Clamp(item.m_variant, 0, icons.Length - 1)] : null;
            image.enabled = image.sprite != null;
        }
        private static void SetField(InputField input, string value, bool reset)
        {
            if (input.text != value) input.SetTextWithoutNotify(value);
            input.readOnly = reset;
            input.image.color = reset ? new Color(.65f, .65f, .65f) : Color.white;
        }
        private static void Caption(Button button, string value) { button.GetComponentInChildren<Text>().text = value; }
        private static void Place(Component component, float x, float y, float width, float height) =>
            Place(component.GetComponent<RectTransform>(), x, y, width, height);
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = TopLeft; rect.pivot = TopLeft;
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        private static void Bottom(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
        public void Dispose() { if (_root != null) Object.Destroy(_root); _rows.Clear(); _translations.Clear(); }
    }
}
