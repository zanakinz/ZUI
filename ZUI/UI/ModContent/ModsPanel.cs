using System.Collections.Generic;
using ZUI.API;
using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.UI.ModContent
{
    internal class ModsPanel : ResizeablePanelBase
    {
        public override string PanelId => "ModsPanel";
        public override int MinWidth => 400;
        public override int MinHeight => 350;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.Mods;
        public override float Opacity => Settings.UITransparency;

        private GameObject _contentLayout;
        private GameObject _scrollableContainer;
        private LabelRef _noModsLabel;
        private readonly List<GameObject> _uiElements = new();

        // Sprite Cache
        private Sprite _btnNormalSprite;
        private Sprite _btnSelectedSprite;
        private Sprite _subpanelSprite;      // For Plugins
        private Sprite _subpanelInletSprite; // For Categories

        public ModsPanel(UIBase owner) : base(owner)
        {
            // Subscribe to registry changes
            ModRegistry.OnButtonsChanged += RefreshButtons;
        }

        protected override void ConstructPanelContent()
        {
            // --- LOAD SPRITES ---
            var panelSprite = SpriteLoader.LoadSprite("panel.png", 100f, new Vector4(30, 30, 30, 30));

            // Plugin Container Background
            _subpanelSprite = SpriteLoader.LoadSprite("subpanel.png", 100f, new Vector4(20, 20, 20, 20));

            // Category Container Background
            _subpanelInletSprite = SpriteLoader.LoadSprite("subpanel_inlet.png", 100f, new Vector4(20, 20, 20, 20));

            _btnNormalSprite = SpriteLoader.LoadSprite("button.png", 100f, new Vector4(10, 10, 10, 10));
            _btnSelectedSprite = SpriteLoader.LoadSprite("button_selected.png", 100f, new Vector4(10, 10, 10, 10));

            // Apply Panel Background
            if (panelSprite != null)
            {
                var bgImage = ContentRoot.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = panelSprite;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = Color.white;
                }
            }

            SetTitle("External Mods");

            _contentLayout = UIFactory.CreateVerticalGroup(ContentRoot, "ContentLayout", true, true, true, true, 8,
                new Vector4(15, 15, 15, 15), new Color(0.1f, 0.1f, 0.1f, 0f)); // Transparent BG
            UIFactory.SetLayoutElement(_contentLayout, flexibleWidth: 9999, flexibleHeight: 9999);

            // Header
            var headerLabel = UIFactory.CreateLabel(_contentLayout, "Header",
                "Third-Party Mod Commands", TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(headerLabel.GameObject, minHeight: 30, flexibleWidth: 9999);
            headerLabel.TextMesh.fontStyle = FontStyles.Bold;
            headerLabel.TextMesh.fontSize = 14;

            var infoLabel = UIFactory.CreateLabel(_contentLayout, "Info",
                "External mods can register their commands here via the ZUI API", TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(infoLabel.GameObject, minHeight: 25, flexibleWidth: 9999);
            infoLabel.TextMesh.fontSize = 11;
            infoLabel.TextMesh.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // Divider
            var divider = UIFactory.CreateUIObject("Divider", _contentLayout);
            UIFactory.SetLayoutElement(divider, minHeight: 2, flexibleWidth: 9999);
            divider.AddComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // No mods message (shown when empty)
            _noModsLabel = UIFactory.CreateLabel(_contentLayout, "NoMods",
                "No external mods registered\n\nDevelopers: Use ModRegistry.SetPlugin() and AddButton()",
                TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(_noModsLabel.GameObject, minHeight: 100, flexibleWidth: 9999);
            _noModsLabel.TextMesh.fontSize = 12;
            _noModsLabel.TextMesh.color = new Color(0.6f, 0.6f, 0.6f, 1f);

            // Create ScrollView for plugins/categories/buttons
            var scrollView = UIFactory.CreateScrollView(_contentLayout, "ModsScrollView", out _scrollableContainer, out var scrollbar,
                new Color(0.05f, 0.05f, 0.05f, 0f));

            UIFactory.SetLayoutElement(scrollView, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_scrollableContainer, false, false, true, true, 8, 5, 5, 5, 5);

            // Initial population
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            if (_scrollableContainer == null) return;

            // Clear existing UI elements
            foreach (var element in _uiElements)
            {
                if (element != null)
                    UnityEngine.Object.Destroy(element);
            }
            _uiElements.Clear();

            // Get registered plugins
            var registeredPlugins = ModRegistry.GetPlugins();

            // Show/hide no mods message
            if (_noModsLabel != null)
                _noModsLabel.GameObject.SetActive(registeredPlugins.Count == 0);

            // Create UI hierarchy for each plugin
            foreach (var plugin in registeredPlugins)
            {
                CreatePluginSection(plugin);
            }

            Plugin.LogInstance.LogInfo($"[ModsPanel] Refreshed with {registeredPlugins.Count} plugin(s)");
        }

        private void CreatePluginSection(ModRegistry.ModPlugin plugin)
        {
            // Plugin container
            var defaultPluginColor = new Color(0.15f, 0.15f, 0.2f, 0.5f);

            var pluginContainer = UIFactory.CreateVerticalGroup(_scrollableContainer,
                $"Plugin_{plugin.PluginName}", false, false, true, true, 5,
                new Vector4(8, 8, 8, 8), defaultPluginColor);

            UIFactory.SetLayoutElement(pluginContainer, minHeight: 40, flexibleWidth: 9999);
            _uiElements.Add(pluginContainer);

            // --- PLUGIN CONTAINER VISUALS (subpanel.png) ---
            if (_subpanelSprite != null)
            {
                var img = pluginContainer.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = _subpanelSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
            }

            // Plugin name label
            var pluginLabel = UIFactory.CreateLabel(pluginContainer, "PluginName",
                plugin.PluginName, TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(pluginLabel.GameObject, minHeight: 30, flexibleWidth: 9999);
            pluginLabel.TextMesh.fontStyle = FontStyles.Bold;
            pluginLabel.TextMesh.fontSize = 14;
            pluginLabel.TextMesh.color = new Color(0.4f, 0.8f, 1f, 1f); // Light blue

            // Create categories
            foreach (var category in plugin.Categories)
            {
                CreateCategorySection(pluginContainer, plugin.PluginName, category);
            }

            // Add spacing between plugins
            var spacer = UIFactory.CreateUIObject("PluginSpacer", _scrollableContainer);
            UIFactory.SetLayoutElement(spacer, minHeight: 5, flexibleWidth: 9999);
            _uiElements.Add(spacer);
        }

        private void CreateCategorySection(GameObject parent, string pluginName, ModRegistry.ModCategory category)
        {
            // Category container
            var defaultCatColor = new Color(0.12f, 0.12f, 0.15f, 0.8f);

            var categoryContainer = UIFactory.CreateVerticalGroup(parent,
                $"Category_{category.CategoryName}", false, false, true, true, 3,
                new Vector4(10, 5, 5, 5), defaultCatColor);

            UIFactory.SetLayoutElement(categoryContainer, minHeight: 30, flexibleWidth: 9999);

            // --- CATEGORY CONTAINER VISUALS (subpanel_inlet.png) ---
            if (_subpanelInletSprite != null)
            {
                var img = categoryContainer.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = _subpanelInletSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white; // Ensure image colors show
                }
            }

            // Category name label
            var categoryLabel = UIFactory.CreateLabel(categoryContainer, "CategoryName",
                category.CategoryName, TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(categoryLabel.GameObject, minHeight: 25, flexibleWidth: 9999);
            categoryLabel.TextMesh.fontStyle = FontStyles.Bold;
            categoryLabel.TextMesh.fontSize = 12;
            categoryLabel.TextMesh.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Create buttons for this category
            foreach (var button in category.Buttons)
            {
                CreateButton(categoryContainer, pluginName, category.CategoryName, button);
            }
        }

        private void CreateButton(GameObject parent, string pluginName, string categoryName,
            ModRegistry.ModButton modButton)
        {
            var btn = UIFactory.CreateButton(parent,
                $"Btn_{pluginName}_{categoryName}_{modButton.ButtonText}",
                modButton.ButtonText);
            UIFactory.SetLayoutElement(btn.GameObject, minHeight: 30, flexibleWidth: 9999);

            // Apply custom button style
            StyleButton(btn);

            // Style the button text
            btn.ButtonText.fontSize = 11;
            btn.ButtonText.alignment = TextAlignmentOptions.Left;
            btn.ButtonText.margin = new Vector4(10, 0, 0, 0); // Left indent

            var command = modButton.Command;
            var callback = modButton.OnClick;

            if (callback != null)
            {
                btn.OnClick = () =>
                {
                    try
                    {
                        callback.Invoke();
                        Plugin.LogInstance.LogInfo($"[Mods] Executed callback for: {modButton.ButtonText}");
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.LogInstance.LogError($"[Mods] Callback error for {modButton.ButtonText}: {ex.Message}");
                    }
                };
            }
            else if (!string.IsNullOrEmpty(command))
            {
                btn.OnClick = () =>
                {
                    MessageService.EnqueueMessage(command);
                    Plugin.LogInstance.LogInfo($"[Mods] Executing: {command}");
                };
            }

            // Add right-click support
            if (callback != null || !string.IsNullOrEmpty(command))
            {
                var eventTrigger = btn.GameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                    eventTrigger = btn.GameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

                var rightClickEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick
                };
                rightClickEntry.callback.AddListener((data) =>
                {
                    var pointerData = data.TryCast<UnityEngine.EventSystems.PointerEventData>();
                    if (pointerData != null && pointerData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                    {
                        if (callback != null)
                        {
                            try { callback.Invoke(); } catch { }
                        }
                        else if (!string.IsNullOrEmpty(command))
                        {
                            MessageService.EnqueueMessage(command);
                        }
                    }
                });
                eventTrigger.triggers.Add(rightClickEntry);
            }
        }

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

        protected override void OnClosePanelClicked()
        {
            SetActive(false);
        }

        internal override void Reset()
        {
        }

        public override void Destroy()
        {
            // Unsubscribe from events
            ModRegistry.OnButtonsChanged -= RefreshButtons;
            base.Destroy();
        }
    }
}