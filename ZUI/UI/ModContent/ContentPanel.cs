using System.Collections.Generic;
using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Controls;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using UnityEngine;
using UnityEngine.UI;
using UIBase = ZUI.UI.UniverseLib.UI.UIBase;

namespace ZUI.UI.ModContent
{
    public class ContentPanel : ResizeablePanelBase
    {
        public override string PanelId => "CorePanel";

        public override int MinWidth => Settings.UseHorizontalContentLayout ? 340 : 100;
        public override int MinHeight => 25;
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPosition => new Vector2(0f, Owner.Scaler.m_ReferenceResolution.y);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.None;
        public override PanelType PanelType => PanelType.Base;
        private GameObject _uiAnchor;
        private UIScaleSettingButton _scaleButtonData;
        private List<GameObject> _objectsList;
        private Toggle _pinToggle;
        public override float Opacity => Settings.UITransparency;

        // Standard Button Sprites (For Submenus/Dropdowns)
        private Sprite _btnNormalSprite;
        private Sprite _btnSelectedSprite;

        // Top Bar Specific Sprites
        private Sprite _menuBarBtnNormal;
        private Sprite _menuBarBtnHighlight;

        public ContentPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            TitleBar.SetActive(false);

            // --- LOAD SPRITES ---

            // 1. Main Background (Top Bar)
            var barSprite = SpriteLoader.LoadSprite("menubar.png", 100f, new Vector4(50, 35, 50, 10));

            // 2. Top Bar Buttons (Seamless style)
            _menuBarBtnNormal = SpriteLoader.LoadSprite("menubar_button.png", 100f, new Vector4(5, 5, 5, 5));
            _menuBarBtnHighlight = SpriteLoader.LoadSprite("menubar_button_highlighted.png", 100f, new Vector4(5, 5, 5, 5));

            // 3. Resize Handle
            var resizeNormal = SpriteLoader.LoadSprite("resize.png", 100f);
            var resizeHover = SpriteLoader.LoadSprite("resize_highlighted.png", 100f);

            // 4. Standard Buttons (For Submenus)
            _btnNormalSprite = SpriteLoader.LoadSprite("button.png", 100f, new Vector4(10, 10, 10, 10));
            _btnSelectedSprite = SpriteLoader.LoadSprite("button_selected.png", 100f, new Vector4(10, 10, 10, 10));

            // Apply Panel Background
            if (barSprite != null)
            {
                var bgImage = ContentRoot.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = barSprite;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = Color.white;
                }
            }

            // --- LAYOUT GROUP ---
            // Padding adjusted to help position the thinner buttons correctly within the bar background
            _uiAnchor = Settings.UseHorizontalContentLayout
                ? UIFactory.CreateHorizontalGroup(ContentRoot, "UIAnchor", true, true, true, true, 2, new Vector4(5, 5, 0, 10))
                : UIFactory.CreateVerticalGroup(ContentRoot, "UIAnchor", false, true, true, true, 2, new Vector4(5, 5, 0, 10));

            Dragger.DraggableArea = Rect;
            Dragger.OnEndResize();

            _objectsList = new List<GameObject>();

            // Pin button
            if (CanDrag)
            {
                var pinButton = UIFactory.CreateToggle(_uiAnchor, "PinButton");
                UIFactory.SetLayoutElement(pinButton.GameObject, minHeight: 15, preferredHeight: 15, flexibleHeight: 0,
                    minWidth: 15, preferredWidth: 15, flexibleWidth: 0, ignoreLayout: false);
                pinButton.Toggle.isOn = Settings.IsUILocked;
                IsPinned = Settings.IsUILocked;
                pinButton.OnValueChanged += (value) =>
                {
                    IsPinned = value;
                    Settings.IsUILocked = value;
                };
                _pinToggle = pinButton.Toggle;
                pinButton.Text.text = " ";
            }

            // ZUI Version Label
            var text = UIFactory.CreateLabel(_uiAnchor, "UIAnchorText", $"ZUI 1.0.2");
            UIFactory.SetLayoutElement(text.GameObject, 80, 25, 1, 1);

            // --- FIX: Raise Text by 15px ---
            // We use margin to push the text visual up without breaking the horizontal layout container
            if (text.TextMesh != null)
            {
                text.TextMesh.margin = new Vector4(0, 0, 0, 15);
            }
            _objectsList.Add(text.GameObject);

            // FAMILIAR submenu - BloodCraft required
            if (DependencyService.HasBloodCraft)
            {
                CreateSubmenu("Familiar", new (string, System.Action)[]
                {
                    ("Box List", () => Plugin.UIManager.AddPanel(PanelType.BoxList)),
                    ("Fam Stats", () => Plugin.UIManager.AddPanel(PanelType.FamStats)),
                    ("Toggle", () => { MessageService.EnqueueMessage(MessageService.BCCOM_TOGGLEFAM); }),
                    ("Unbind", () => { MessageService.EnqueueMessage(MessageService.BCCOM_UNBINDFAM); }),
                    ("Rebind", () => { if (!string.IsNullOrEmpty(Settings.LastBindCommand)) MessageService.EnqueueMessage(Settings.LastBindCommand); }),
                    ("Fam. Actions", () => Plugin.UIManager.AddPanel(PanelType.FamActions))
                });
            }

            // LEVELS submenu - BloodCraft required
            if (DependencyService.HasBloodCraft)
            {
                CreateSubmenu("Levels", new (string, System.Action)[]
                {
                    ("Weapon", () => Plugin.UIManager.AddPanel(PanelType.LevelUp, "Weapon")),
                    ("Blood", () => Plugin.UIManager.AddPanel(PanelType.LevelUp, "Blood"))
                });
            }

            // QUESTS button - BloodCraft required
            if (DependencyService.HasBloodCraft)
            {
                var questsButton = UIFactory.CreateButton(_uiAnchor, "QuestsButton", "Quests");
                // Height reduced to 17
                UIFactory.SetLayoutElement(questsButton.GameObject, ignoreLayout: false, minWidth: 65, minHeight: 17);
                StyleMenuBarButton(questsButton);
                _objectsList.Add(questsButton.GameObject);
                questsButton.OnClick = () => Plugin.UIManager.AddPanel(PanelType.Quests);
            }

            // CLASS button - BloodCraft required
            if (DependencyService.HasBloodCraft)
            {
                var classButton = UIFactory.CreateButton(_uiAnchor, "ClassButton", "Class");
                // Height reduced to 17
                UIFactory.SetLayoutElement(classButton.GameObject, ignoreLayout: false, minWidth: 60, minHeight: 17);
                StyleMenuBarButton(classButton);
                _objectsList.Add(classButton.GameObject);
                classButton.OnClick = () => Plugin.UIManager.AddPanel(PanelType.Class);
            }

            // PRESTIGE submenu - BloodCraft required
            if (Settings.IsPrestigeButtonEnabled && DependencyService.HasBloodCraft)
            {
                CreateSubmenu("Prestige", new (string optionText, System.Action action)[]
                {
                    ("Experience", () => MessageService.EnqueueMessage(".prestige me Experience")),
                    ("Familiar", () => MessageService.EnqueueMessage(MessageService.BCCOM_PRESTIGEFAM)),
                    ("Weapon", () => MessageService.EnqueueMessage(".prestige me Weapon")),
                    ("Blood", () => MessageService.EnqueueMessage(".prestige me Blood"))
                });
            }

            // COMBAT MODE toggle - BloodCraft required
            if (Settings.IsCombatButtonEnabled && DependencyService.HasBloodCraft)
            {
                var combatToggle = UIFactory.CreateToggle(_uiAnchor, "FamToggleCombatButton");
                combatToggle.Text.text = "Combat";
                combatToggle.Text.fontSize = 11;
                combatToggle.OnValueChanged += value =>
                {
                    MessageService.EnqueueMessage(MessageService.BCCOM_COMBAT);
                    combatToggle.DisableWithTimer(2000);
                };
                // Height reduced to 17
                UIFactory.SetLayoutElement(combatToggle.GameObject, ignoreLayout: false, minWidth: 80, minHeight: 17);
            }

            // UTILS submenu - conditional items based on dependencies
            CreateUtilsSubmenu();

            // MODS button - Always available
            var modsButton = UIFactory.CreateButton(_uiAnchor, "ModsButton", "Mods");
            // Height reduced to 17
            UIFactory.SetLayoutElement(modsButton.GameObject, ignoreLayout: false, minWidth: 60, minHeight: 17);
            StyleMenuBarButton(modsButton);
            _objectsList.Add(modsButton.GameObject);
            modsButton.OnClick = () => Plugin.UIManager.AddPanel(PanelType.Mods);

            // --- SCALE BUTTON (RESIZE HANDLE) ---
            var scaleButton = UIFactory.CreateButton(_uiAnchor, "ScaleButton", "");

            // --- FIX: Smaller Size & Manual Position ---
            // 1. Ignore Layout so we can place it freely
            // 2. Size reduced to 20x20 (was ~25-32)
            UIFactory.SetLayoutElement(scaleButton.GameObject, ignoreLayout: true, minWidth: 20, minHeight: 20);
            _objectsList.Add(scaleButton.GameObject);

            // 3. Anchor Top Right
            var scaleRect = scaleButton.GameObject.GetComponent<RectTransform>();
            scaleRect.anchorMin = new Vector2(1, 1);
            scaleRect.anchorMax = new Vector2(1, 1);
            scaleRect.pivot = new Vector2(1, 1);
            scaleRect.anchoredPosition = new Vector2(-3, -3); // Tucked into corner

            // Apply Resize Sprites
            if (resizeNormal != null)
            {
                scaleButton.ButtonText.gameObject.SetActive(false);
                var img = scaleButton.GameObject.GetComponent<Image>();
                img.sprite = resizeNormal;
                img.color = Color.white;

                var colors = scaleButton.Component.colors;
                colors.normalColor = Color.white;
                colors.colorMultiplier = 1f;
                scaleButton.Component.colors = colors;

                if (resizeHover != null)
                {
                    var comp = scaleButton.Component;
                    comp.transition = Selectable.Transition.SpriteSwap;
                    var state = comp.spriteState;
                    state.highlightedSprite = resizeHover;
                    state.pressedSprite = resizeHover;
                    state.selectedSprite = resizeHover;
                    comp.spriteState = state;
                }
            }
            else
            {
                scaleButton.ButtonText.text = "*";
            }

            _scaleButtonData = new UIScaleSettingButton();
            scaleButton.OnClick = () =>
            {
                _scaleButtonData.PerformAction();
                var panel = Plugin.UIManager.GetPanel<FamStatsPanel>();
                if (panel != null && panel.UIRoot.active)
                    panel.RecalculateHeight();
            };

            // TEST button (only in testing)
            if (Plugin.IS_TESTING)
            {
                var b = UIFactory.CreateButton(_uiAnchor, "TestButton", "T");
                UIFactory.SetLayoutElement(b.GameObject, ignoreLayout: false, minWidth: 25, minHeight: 17);
                StyleMenuBarButton(b);
                _objectsList.Add(scaleButton.GameObject);
                b.OnClick = () => Plugin.UIManager.AddPanel(PanelType.TestPanel);
            }
        }

        // --- NEW: Styles buttons for the Top Bar (Seamless) ---
        private void StyleMenuBarButton(ButtonRef btn)
        {
            if (_menuBarBtnNormal != null)
            {
                var img = btn.GameObject.GetComponent<Image>();
                if (img)
                {
                    img.sprite = _menuBarBtnNormal;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
            }

            // Force pure white colors to avoid dark tint
            var colors = btn.Component.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.colorMultiplier = 1f;
            btn.Component.colors = colors;

            if (_menuBarBtnHighlight != null)
            {
                var comp = btn.Component;
                comp.transition = Selectable.Transition.SpriteSwap;
                var state = comp.spriteState;
                state.highlightedSprite = _menuBarBtnHighlight;
                state.pressedSprite = _menuBarBtnHighlight;
                state.selectedSprite = _menuBarBtnHighlight;
                comp.spriteState = state;
            }
        }

        // --- STANDARD: Styles buttons for dropdowns/submenus ---
        private void StyleButton(ButtonRef btn)
        {
            if (_btnNormalSprite == null) return;

            var img = btn.GameObject.GetComponent<Image>();
            if (img)
            {
                img.sprite = _btnNormalSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            if (_btnSelectedSprite != null)
            {
                var comp = btn.Component;
                comp.transition = Selectable.Transition.SpriteSwap;
                var state = comp.spriteState;
                state.highlightedSprite = _btnSelectedSprite;
                state.pressedSprite = _btnSelectedSprite;
                state.selectedSprite = _btnSelectedSprite;
                comp.spriteState = state;
            }
        }

        // Creates Utils submenu with dependency-based item disabling
        private void CreateUtilsSubmenu()
        {
            var utilsOptions = new List<(string optionText, System.Action action, bool enabled)>();

            // Signs - requires ScarletSigns
            utilsOptions.Add(("Signs",
                () => Plugin.UIManager.AddPanel(PanelType.Signs),
                DependencyService.HasScarletSigns));

            // Ponds - requires KinPonds
            utilsOptions.Add(("Ponds",
                () => MessageService.EnqueueMessage(MessageService.BCCOM_POND),
                DependencyService.HasKinPonds));

            // User - requires KindredCommands
            utilsOptions.Add(("User",
                () => Plugin.UIManager.AddPanel(PanelType.User),
                DependencyService.HasKindredCommands));

            // Admin - requires KindredCommands
            utilsOptions.Add(("Admin",
                () => Plugin.UIManager.AddPanel(PanelType.Admin),
                DependencyService.HasKindredCommands));

            // Only create submenu if at least one option is available
            if (utilsOptions.Count > 0)
            {
                CreateSubmenuWithDisabledSupport("Utils", utilsOptions.ToArray());
            }
        }

        private void CreateSubmenu(string buttonText, (string optionText, System.Action action)[] options)
        {
            var button = UIFactory.CreateButton(_uiAnchor, $"{buttonText}Button", buttonText);
            // HEIGHT CHANGED: 17
            UIFactory.SetLayoutElement(button.GameObject, ignoreLayout: false, minWidth: 70, minHeight: 17);
            StyleMenuBarButton(button); // Main button uses Bar Style
            _objectsList.Add(button.GameObject);

            // Create submenu container
            var menuContainer = UIFactory.CreateVerticalGroup(button.GameObject, $"{buttonText}MenuContainer",
                true, false, true, true, 2, new Vector4(2, 2, 2, 2), new Color(0.1f, 0.1f, 0.1f, 0.9f));
            var menuRect = menuContainer.GetComponent<RectTransform>();

            // Anchor to bottom of button
            menuRect.anchorMin = new Vector2(0, 0);
            menuRect.anchorMax = new Vector2(1, 0);
            menuRect.pivot = new Vector2(0.5f, 1);
            menuRect.anchoredPosition = new Vector2(0, -2);

            menuRect.sizeDelta = new Vector2(0, 0);
            var menuLe = menuContainer.GetComponent<LayoutElement>();
            if (menuLe) menuLe.ignoreLayout = true;

            menuContainer.SetActive(false);

            // Add option buttons
            foreach (var (optionText, action) in options)
            {
                var optionButton = UIFactory.CreateButton(menuContainer, $"{buttonText}_{optionText}Button", optionText);
                UIFactory.SetLayoutElement(optionButton.GameObject, minHeight: 25, flexibleWidth: 9999);
                StyleButton(optionButton); // Dropdown items use Standard Style
                optionButton.OnClick = () =>
                {
                    menuContainer.SetActive(false);
                    action?.Invoke();
                    // Add visual feedback - disable button briefly to show action was registered
                    optionButton.DisableWithTimer(2000);
                };
            }

            // Toggle menu visibility
            button.OnClick = () =>
            {
                bool show = !menuContainer.activeSelf;
                menuContainer.SetActive(show);
            };
        }


        // Creates a submenu with support for disabled items

        private void CreateSubmenuWithDisabledSupport(string buttonText, (string optionText, System.Action action, bool enabled)[] options)
        {
            var button = UIFactory.CreateButton(_uiAnchor, $"{buttonText}Button", buttonText);
            // HEIGHT CHANGED: 17
            UIFactory.SetLayoutElement(button.GameObject, ignoreLayout: false, minWidth: 70, minHeight: 17);
            StyleMenuBarButton(button); // Main button uses Bar Style
            _objectsList.Add(button.GameObject);

            // Create submenu container
            var menuContainer = UIFactory.CreateVerticalGroup(button.GameObject, $"{buttonText}MenuContainer",
                true, false, true, true, 2, new Vector4(2, 2, 2, 2), new Color(0.1f, 0.1f, 0.1f, 0.9f));
            var menuRect = menuContainer.GetComponent<RectTransform>();

            // Anchor to bottom of button
            menuRect.anchorMin = new Vector2(0, 0);
            menuRect.anchorMax = new Vector2(1, 0);
            menuRect.pivot = new Vector2(0.5f, 1);
            menuRect.anchoredPosition = new Vector2(0, -2);

            menuRect.sizeDelta = new Vector2(0, 0);
            var menuLe = menuContainer.GetComponent<LayoutElement>();
            if (menuLe) menuLe.ignoreLayout = true;

            menuContainer.SetActive(false);

            // Add option buttons
            foreach (var (optionText, action, enabled) in options)
            {
                var optionButton = UIFactory.CreateButton(menuContainer, $"{buttonText}_{optionText}Button", optionText);
                UIFactory.SetLayoutElement(optionButton.GameObject, minHeight: 25, flexibleWidth: 9999);
                StyleButton(optionButton); // Dropdown items use Standard Style

                if (!enabled)
                {
                    // Disable button visually - gray out
                    optionButton.Component.interactable = false;
                    var textComponent = optionButton.Component.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.color = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Gray and semi-transparent
                        textComponent.text = $"{optionText} (Unavailable)";
                    }
                }
                else
                {
                    optionButton.OnClick = () =>
                    {
                        menuContainer.SetActive(false);
                        action?.Invoke();
                        // Add visual feedback - disable button briefly to show action was registered
                        optionButton.DisableWithTimer(2000);
                    };
                }
            }

            // Toggle menu visibility
            button.OnClick = () =>
            {
                bool show = !menuContainer.activeSelf;
                menuContainer.SetActive(show);
            };
        }

        protected override void OnClosePanelClicked()
        {
            SetActive(false);
        }

        internal override void Reset()
        {
        }
    }
}