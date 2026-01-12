using System.Collections.Generic;
using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Controls;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
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
        //public override int MaxWidth => 150;

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

        public ContentPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            TitleBar.SetActive(false);
            _uiAnchor = Settings.UseHorizontalContentLayout
                ? UIFactory.CreateHorizontalGroup(ContentRoot, "UIAnchor", true, true, true, true)
                : UIFactory.CreateVerticalGroup(ContentRoot, "UIAnchor", false, true, true, true, padding: new Vector4(5,5,5,5));

            Dragger.DraggableArea = Rect;
            Dragger.OnEndResize();

            _objectsList = new List<GameObject>();

            // Pin button
            if (CanDrag)
            {
                var pinButton = UIFactory.CreateToggle(_uiAnchor, "PinButton");
                UIFactory.SetLayoutElement(pinButton.GameObject, minHeight: 15, preferredHeight: 15, flexibleHeight: 0,
                    minWidth: 15, preferredWidth: 15, flexibleWidth: 0, ignoreLayout: false);
                // Initialize with saved value
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
                    ("Rebind", () => 
                    { 
                        if (!string.IsNullOrEmpty(Settings.LastBindCommand))
                            MessageService.EnqueueMessage(Settings.LastBindCommand);
                    }),
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
                UIFactory.SetLayoutElement(questsButton.GameObject, ignoreLayout: false, minWidth: 65, minHeight: 25);
                _objectsList.Add(questsButton.GameObject);
                questsButton.OnClick = () => Plugin.UIManager.AddPanel(PanelType.Quests);
            }

            // CLASS button - BloodCraft required
            if (DependencyService.HasBloodCraft)
            {
                var classButton = UIFactory.CreateButton(_uiAnchor, "ClassButton", "Class");
                UIFactory.SetLayoutElement(classButton.GameObject, ignoreLayout: false, minWidth: 60, minHeight: 25);
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
                UIFactory.SetLayoutElement(combatToggle.GameObject, ignoreLayout: false, minWidth: 80, minHeight: 25);
            }

            // UTILS submenu - conditional items based on dependencies
            CreateUtilsSubmenu();

            // MODS button - Always available
            var modsButton = UIFactory.CreateButton(_uiAnchor, "ModsButton", "Mods");
            UIFactory.SetLayoutElement(modsButton.GameObject, ignoreLayout: false, minWidth: 60, minHeight: 25);
            _objectsList.Add(modsButton.GameObject);
            modsButton.OnClick = () => Plugin.UIManager.AddPanel(PanelType.Mods);

            // SCALE button with resize icon
            var scaleButton = UIFactory.CreateButton(_uiAnchor, "ScaleButton", "");
            UIFactory.SetLayoutElement(scaleButton.GameObject, ignoreLayout: false, minWidth: 25, minHeight: 25);
            _objectsList.Add(scaleButton.GameObject);
            
            // Load and apply resize sprite with smart scaling
            var resizeSprite = SpriteLoader.LoadSprite("resize.png");
            if (resizeSprite != null)
            {
                // Hide the button text
                scaleButton.ButtonText.gameObject.SetActive(false);
                
                // Create a child GameObject for the icon
                var iconObj = new GameObject("ResizeIcon");
                iconObj.transform.SetParent(scaleButton.GameObject.transform, false);
                
                var iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = resizeSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                
                // Set the rect transform to fit within the button with padding
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-6, -6); // 3px padding on each side
                iconRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                // Fallback to text if sprite fails to load
                scaleButton.ButtonText.text = "*";
            }
            
            _scaleButtonData = new UIScaleSettingButton();
            scaleButton.OnClick = () =>
            {
                _scaleButtonData.PerformAction();
                var panel = Plugin.UIManager.GetPanel<FamStatsPanel>();
                if(panel != null && panel.UIRoot.active)
                    panel.RecalculateHeight();
            };

            // TEST button (only in testing)
            if (Plugin.IS_TESTING)
            {
                var b = UIFactory.CreateButton(_uiAnchor, "TestButton", "T");
                UIFactory.SetLayoutElement(b.GameObject, ignoreLayout: false, minWidth: 25, minHeight: 25);
                _objectsList.Add(scaleButton.GameObject);
                b.OnClick = () => Plugin.UIManager.AddPanel(PanelType.TestPanel);
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
            UIFactory.SetLayoutElement(button.GameObject, ignoreLayout: false, minWidth: 70, minHeight: 25);
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
            UIFactory.SetLayoutElement(button.GameObject, ignoreLayout: false, minWidth: 70, minHeight: 25);
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