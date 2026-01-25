using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Services;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using ZUI.UI.Components;
using Il2CppInterop.Runtime;
namespace ZUI.UI.ModContent
{
    public class CustomModPanel : ResizeablePanelBase
    {
        public string PluginName { get; private set; }
        public string WindowId { get; private set; }
        public override string PanelId => $"{PluginName}_{WindowId}";
        public override PanelType PanelType => PanelType.Base;
        // Dimensions
        private int _initialWidth;
        private int _initialHeight;
        public override int MinWidth => 100;
        public override int MinHeight => 100;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new(0.5f, 0.5f);

        // Layout Mode
        private bool _isTemplateMode;
        private bool _hasCustomTitle;

        // References
        private GameObject _textScrollContent;
        private GameObject _buttonScrollContent;
        private GameObject _absoluteContainer;
        private Image _bgImage;
        private Image _overlayImage;

        // Tab System
        private GameObject _tabBar;
        private GameObject _activeTabContent;
        private List<TabContext> _tabs = new List<TabContext>();

        private Sprite _tabNormalSprite;
        private Sprite _tabActiveSprite;

        // Data Binding System
        private readonly Dictionary<string, Func<string>> _dataContext = new Dictionary<string, Func<string>>();
        private readonly Dictionary<string, ToggleGroup> _radioGroups = new Dictionary<string, ToggleGroup>();

        private class TabContext
        {
            public string Name;
            public GameObject ContentObj;
            public ButtonRef Button;
        }

        private Dictionary<string, GameObject> _elements = new Dictionary<string, GameObject>();

        // This tracks images that are waiting for a download to finish
        private List<PendingSprite> _pendingSprites = new List<PendingSprite>();
        private struct PendingSprite
        {
            public Image TargetImage;
            public string SpriteName;
            public Assembly Assembly;
            public GameObject Owner;
        }

        public CustomModPanel(UIBase owner, string pluginName, string windowId, string template) : base(owner)
        {
            PluginName = pluginName;
            WindowId = windowId;
            _isTemplateMode = true;
            PreloadTabSprites();
            ApplyTemplate(template);
            ZUI.API.ModRegistry.OnButtonsChanged += OnRegistryChanged;
        }

        public CustomModPanel(UIBase owner, string pluginName, string windowId, int width, int height) : base(owner)
        {
            PluginName = pluginName;
            WindowId = windowId;
            _isTemplateMode = false;
            _initialWidth = width;
            _initialHeight = height;
            PreloadTabSprites();
            Rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
            ZUI.API.ModRegistry.OnButtonsChanged += OnRegistryChanged;
        }

        private void PreloadTabSprites()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _tabNormalSprite = SpriteLoader.LoadSpriteFromAssembly(assembly, "button.png", 100f, new Vector4(10, 10, 10, 10));
            _tabActiveSprite = SpriteLoader.LoadSpriteFromAssembly(assembly, "button_selected.png", 100f, new Vector4(10, 10, 10, 10));
        }

        private void OnRegistryChanged()
        {
            for (int i = _pendingSprites.Count - 1; i >= 0; i--)
            {
                var pending = _pendingSprites[i];
                if (pending.TargetImage == null || pending.Owner == null)
                {
                    _pendingSprites.RemoveAt(i);
                    continue;
                }

                var sprite = SpriteLoader.LoadSpriteFromAssembly(pending.Assembly, pending.SpriteName, 100f);
                if (sprite != null)
                {
                    pending.TargetImage.sprite = sprite;
                    pending.TargetImage.color = Color.white;

                    var gifData = SpriteLoader.GetGif(pending.SpriteName);
                    if (gifData != null && pending.Owner.GetComponent<GifPlayer>() == null)
                    {
                        var player = pending.Owner.AddComponent(Il2CppType.Of<GifPlayer>()).Cast<GifPlayer>();
                        player.SetGifData(gifData);
                    }
                    _pendingSprites.RemoveAt(i);
                }
            }
        }

        public void ApplyTemplate(string template)
        {
            Reset();
            _isTemplateMode = true;
            switch (template.ToLower())
            {
                case "small": _initialWidth = 400; _initialHeight = 300; break;
                case "medium": _initialWidth = 600; _initialHeight = 450; break;
                case "large": _initialWidth = 800; _initialHeight = 600; break;
                default: _initialWidth = 600; _initialHeight = 450; break;
            }
            if (Rect != null) Rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
            if (ContentRoot != null) RebuildLayouts();
        }

        public void SetCustomSize(int width, int height)
        {
            Reset();
            _isTemplateMode = false;
            _initialWidth = width;
            _initialHeight = height;
            if (Rect != null) Rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
            if (ContentRoot != null) RebuildLayouts();
        }

        private void RebuildLayouts()
        {
            if (_absoluteContainer)
            {
                if (_absoluteContainer.transform.parent != null && _absoluteContainer.transform.parent.parent != null)
                    UnityEngine.Object.Destroy(_absoluteContainer.transform.parent.parent.gameObject);
                else
                    UnityEngine.Object.Destroy(_absoluteContainer);
            }

            if (_textScrollContent)
            {
                if (_textScrollContent.transform.parent != null && _textScrollContent.transform.parent.parent != null)
                    UnityEngine.Object.Destroy(_textScrollContent.transform.parent.parent.gameObject);
            }

            var split = ContentRoot.transform.Find("SplitGroup");
            if (split != null) UnityEngine.Object.Destroy(split.gameObject);

            if (_isTemplateMode)
            {
                TitleBar.SetActive(true);
                Dragger.DraggableArea = TitleBar.GetComponent<RectTransform>();
                ConstructTemplateLayout();
            }
            else
            {
                TitleBar.SetActive(_hasCustomTitle);
                Dragger.DraggableArea = TitleBar.GetComponent<RectTransform>();
                ConstructAbsoluteLayout();
            }
        }

        public void SetTitleBarVisibility(bool visible) { if (TitleBar != null) TitleBar.SetActive(visible); }

        public void SetWindowTitle(string title)
        {
            _hasCustomTitle = true;
            SetTitle(title);
            SetTitleBarVisibility(true);
            RefreshLayoutOffsets();
        }

        public override void Update()
        {
            base.Update();
            if (_bgImage != null && _bgImage.sprite != null && _bgImage.color != Color.white) _bgImage.color = Color.white;
            if (_overlayImage != null && _overlayImage.color.a > 0f) _overlayImage.color = Color.clear;
        }

        protected override void ConstructPanelContent()
        {
            Reset();
            base.ConstructPanelContent();
            SetTitle($"{PluginName} - {WindowId}");

            var panelSprite = SpriteLoader.LoadSpriteFromAssembly(Assembly.GetExecutingAssembly(), "panel.png", 100f, new Vector4(30, 30, 30, 30));
            _bgImage = ContentRoot.GetComponent<Image>();
            if (_bgImage == null) _bgImage = ContentRoot.AddComponent<Image>();

            if (panelSprite != null) { _bgImage.sprite = panelSprite; _bgImage.type = Image.Type.Sliced; _bgImage.color = Color.white; }
            else { _bgImage.sprite = null; _bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); }

            RebuildLayouts();
            CreateOverlayCloseButton();
        }

        private void CreateOverlayCloseButton()
        {
            if (Rect.gameObject.transform.Find("OverlayCloseButton") != null) return;

            var closeBtnObj = UIFactory.CreateButton(Rect.gameObject, "OverlayCloseButton", "X");
            var layout = closeBtnObj.GameObject.GetComponent<LayoutElement>();
            if (!layout) layout = closeBtnObj.GameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var btnRect = closeBtnObj.GameObject.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-5, -5);
            btnRect.sizeDelta = new Vector2(24, 24);

            var closeSprite = SpriteLoader.LoadSpriteFromAssembly(Assembly.GetExecutingAssembly(), "close_button.png", 100f);

            if (closeSprite != null)
            {
                var img = closeBtnObj.GameObject.GetComponent<Image>();
                if (img) { img.sprite = closeSprite; img.color = Color.white; }
                if (closeBtnObj.ButtonText != null) closeBtnObj.ButtonText.text = "";
                var colors = closeBtnObj.Component.colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f); colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                closeBtnObj.Component.colors = colors;
            }

            closeBtnObj.OnClick = () => { this.SetActive(false); };
        }

        private void ConstructTemplateLayout()
        {
            var splitGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "SplitGroup", true, true, true, true, 0, new Vector4(5, 5, 5, 5), Color.clear);
            UIFactory.SetLayoutElement(splitGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            _overlayImage = splitGroup.GetComponent<Image>();
            if (_overlayImage != null) _overlayImage.color = Color.clear;

            var leftScrollRoot = UIFactory.CreateScrollView(splitGroup, "TextScroll", out _textScrollContent, out _, Color.clear);
            UIFactory.SetLayoutElement(leftScrollRoot, flexibleWidth: 1, flexibleHeight: 9999);
            _textScrollContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 };

            var separator = UIFactory.CreateUIObject("Separator", splitGroup);
            UIFactory.SetLayoutElement(separator, minWidth: 2, flexibleWidth: 0, flexibleHeight: 9999);
            separator.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);

            var rightScrollRoot = UIFactory.CreateScrollView(splitGroup, "ButtonScroll", out _buttonScrollContent, out _, Color.clear);
            UIFactory.SetLayoutElement(rightScrollRoot, flexibleWidth: 1, flexibleHeight: 9999);
            _buttonScrollContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 };
        }

        private void ConstructAbsoluteLayout()
        {
            GameObject scrollRoot = UIFactory.CreateScrollView(ContentRoot, "AbsoluteScroll", out _absoluteContainer, out var scrollbar, Color.clear);
            _overlayImage = scrollRoot.GetComponent<Image>();
            if (_overlayImage != null) _overlayImage.color = Color.clear;
            UIFactory.SetLayoutElement(scrollRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            var viewport = _absoluteContainer.transform.parent;
            if (viewport != null) { var mask = viewport.GetComponent<RectMask2D>(); if (mask == null) viewport.gameObject.AddComponent<RectMask2D>(); }

            var vlg = _absoluteContainer.GetComponent<VerticalLayoutGroup>(); if (vlg) UnityEngine.Object.DestroyImmediate(vlg);
            var csf = _absoluteContainer.GetComponent<ContentSizeFitter>(); if (csf) UnityEngine.Object.DestroyImmediate(csf);

            var rect = _absoluteContainer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero;

            RefreshLayoutOffsets();
        }

        private void RefreshLayoutOffsets()
        {
            if (_isTemplateMode || _absoluteContainer == null) return;
            float topOffset = _hasCustomTitle ? -30f : 0f;
            if (_tabBar != null) topOffset -= 35f;
            float heightReduction = Math.Abs(topOffset);

            var scrollRectObj = _absoluteContainer.transform.parent.parent.GetComponent<RectTransform>();
            scrollRectObj.anchoredPosition = new Vector2(0, topOffset);
            scrollRectObj.sizeDelta = new Vector2(0, -heightReduction);

            var contentRect = _absoluteContainer.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(_initialWidth - 25, _initialHeight);
        }

        #region Public Modification Methods

        public void CreateTab(string name, string tooltip)
        {
            if (_isTemplateMode) return;

            if (_tabBar == null)
            {
                _tabBar = UIFactory.CreateHorizontalGroup(ContentRoot, "TabBar", false, true, true, true, 2, new Vector4(5, 5, 2, 2), Color.clear);
                var tabRect = _tabBar.GetComponent<RectTransform>();
                tabRect.anchorMin = new Vector2(0, 1); tabRect.anchorMax = new Vector2(1, 1); tabRect.pivot = new Vector2(0.5f, 1);
                float yPos = _hasCustomTitle ? -30f : 0f;
                tabRect.anchoredPosition = new Vector2(0, yPos);
                tabRect.sizeDelta = new Vector2(0, 30);

                var le = _tabBar.GetComponent<LayoutElement>();
                if (le) le.ignoreLayout = true;

                _tabBar.transform.SetAsLastSibling();
                RefreshLayoutOffsets();
            }
            else
            {
                _tabBar.SetActive(true);
            }

            var tabBtn = UIFactory.CreateButton(_tabBar, $"Tab_{name}", name);
            UIFactory.SetLayoutElement(tabBtn.GameObject, minWidth: 80, minHeight: 28);

            var img = tabBtn.Component.GetComponent<Image>();
            if (img != null) { img.sprite = _tabNormalSprite; img.color = Color.white; }

            var contentObj = UIFactory.CreateUIObject($"TabContent_{name}", _absoluteContainer);
            var rect = contentObj.GetComponent<RectTransform>();
            if (rect == null) rect = contentObj.AddComponent<RectTransform>();

            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = Vector2.zero;

            var tabData = new TabContext { Name = name, Button = tabBtn, ContentObj = contentObj };
            tabBtn.OnClick = () => SelectTab(tabData);
            _tabs.Add(tabData);
            _activeTabContent = contentObj;

            if (_tabs.Count == 1) SelectTab(tabData);
            else contentObj.SetActive(false);

            RecalculateScrollHeight();
        }

        private void SelectTab(TabContext tab)
        {
            foreach (var t in _tabs)
            {
                bool isActive = t == tab;
                if (t.ContentObj != null) t.ContentObj.SetActive(isActive);

                var img = t.Button.Component.GetComponent<Image>();
                if (img != null) { img.sprite = isActive ? _tabActiveSprite : _tabNormalSprite; img.color = Color.white; }

                var txt = t.Button.ButtonText;
                if (txt != null) txt.color = isActive ? new Color(1f, 0.8f, 0.4f) : Color.white;
            }

            _activeTabContent = tab.ContentObj;
            RecalculateScrollHeight();
        }

        private void RecalculateScrollHeight()
        {
            Transform container = _activeTabContent != null ? _activeTabContent.transform : _absoluteContainer.transform;

            float lowestY = 0;
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                var rt = child.GetComponent<RectTransform>();
                float bottom = Math.Abs(rt.anchoredPosition.y) + rt.sizeDelta.y;
                if (bottom > lowestY) lowestY = bottom;
            }

            var contentRect = _absoluteContainer.GetComponent<RectTransform>();
            float requiredHeight = lowestY + 40;

            float availableViewHeight = _initialHeight;
            if (_hasCustomTitle) availableViewHeight -= 30;
            if (_tabBar != null && _tabBar.activeSelf) availableViewHeight -= 35;

            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Math.Max(requiredHeight, availableViewHeight));
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        public void AddCategory(string name, float x = -1, float y = -1)
        {
            if (_isTemplateMode)
            {
                var label = UIFactory.CreateLabel(_buttonScrollContent, $"Cat_{name}", name, TextAlignmentOptions.Left);
                label.TextMesh.fontSize = 14; label.TextMesh.fontStyle = FontStyles.Bold;
                label.TextMesh.color = new Color(1f, 0.8f, 0.4f);
                UIFactory.SetLayoutElement(label.GameObject, minHeight: 30, flexibleWidth: 9999);
                RegisterElement(name, label.GameObject);
            }
            else
            {
                var parent = _activeTabContent ?? _absoluteContainer;
                var label = UIFactory.CreateLabel(parent, $"Cat_{name}", name, TextAlignmentOptions.Left);
                PositionElement(label.GameObject, x, y, 150, 25);
                RegisterElement(name, label.GameObject);
            }
        }

        public void AddText(string id, string content, float x = -1, float y = -1)
        {
            if (_isTemplateMode)
            {
                var label = UIFactory.CreateLabel(_textScrollContent, id, content, TextAlignmentOptions.TopLeft);
                label.TextMesh.enableWordWrapping = true;
                label.GameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                UIFactory.SetLayoutElement(label.GameObject, minHeight: 0, flexibleHeight: 0, flexibleWidth: 9999);
                RegisterElement(id, label.GameObject);
            }
            else
            {
                var parent = _activeTabContent ?? _absoluteContainer;
                var label = UIFactory.CreateLabel(parent, id, content, TextAlignmentOptions.TopLeft);
                PositionElement(label.GameObject, x, y, 200, 50);
                RegisterElement(id, label.GameObject);
            }
        }

        // ==============================================================================================
        // INPUT COMPONENTS (DATA FIELDS)
        // ==============================================================================================

        public void AddInputField(string id, string placeholder, float x, float y, float w)
        {
            if (_isTemplateMode) return;
            var parent = _activeTabContent ?? _absoluteContainer;

            // Returns InputFieldRef
            var inputRef = UIFactory.CreateInputField(parent, id, placeholder);
            PositionElement(inputRef.GameObject, x, y, w, 30);

            _dataContext[id] = () => inputRef.Component.text;
            RegisterElement(id, inputRef.GameObject);
        }

        public void AddToggle(string id, string labelText, bool defaultValue, float x, float y)
        {
            if (_isTemplateMode) return;
            var parent = _activeTabContent ?? _absoluteContainer;

            // Returns ToggleRef
            var toggleRef = UIFactory.CreateToggle(parent, id, default, 20, 20, labelText);
            toggleRef.Toggle.isOn = defaultValue;

            PositionElement(toggleRef.GameObject, x, y, 150, 25);

            _dataContext[id] = () => toggleRef.Toggle.isOn.ToString().ToLower();
            RegisterElement(id, toggleRef.GameObject);
        }

        public void AddRadioButton(string id, string groupName, string labelText, bool defaultValue, float x, float y)
        {
            if (_isTemplateMode) return;
            var parent = _activeTabContent ?? _absoluteContainer;

            if (!_radioGroups.TryGetValue(groupName, out var group))
            {
                group = parent.GetComponent<ToggleGroup>();
                if (group == null) group = parent.AddComponent<ToggleGroup>();
                _radioGroups[groupName] = group;
            }

            var toggleRef = UIFactory.CreateToggle(parent, id, default, 20, 20, labelText);
            toggleRef.Toggle.group = group;
            toggleRef.Toggle.isOn = defaultValue;

            PositionElement(toggleRef.GameObject, x, y, 150, 25);

            _dataContext[id] = () => toggleRef.Toggle.isOn.ToString().ToLower();
            RegisterElement(id, toggleRef.GameObject);
        }

        public void AddSlider(string id, float min, float max, float defaultValue, float x, float y, float w)
        {
            if (_isTemplateMode) return;
            var parent = _activeTabContent ?? _absoluteContainer;

            // Returns GameObject, out Slider
            var sliderObj = UIFactory.CreateSlider(parent, id, out var sliderComp);
            sliderComp.minValue = min;
            sliderComp.maxValue = max;
            sliderComp.value = defaultValue;

            PositionElement(sliderObj, x, y, w, 30);

            // Add value label
            var valLabel = UIFactory.CreateLabel(sliderObj, $"{id}_Val", defaultValue.ToString("F2"), TextAlignmentOptions.Right);
            var lblRect = valLabel.GameObject.GetComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(1, 0); lblRect.anchorMax = new Vector2(1, 1);
            lblRect.pivot = new Vector2(1, 0.5f);
            lblRect.anchoredPosition = new Vector2(40, 0); // Position to the right of slider
            // Make sure label doesn't block slider interaction
            if (valLabel.TextMesh.gameObject.GetComponent<CanvasGroup>() == null)
                valLabel.TextMesh.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            sliderComp.onValueChanged.AddListener((val) => { valLabel.TextMesh.text = val.ToString("F2"); });

            _dataContext[id] = () => sliderComp.value.ToString("F2");
            RegisterElement(id, sliderObj);
        }
        public void AddDropdown(string id, List<string> options, int defaultIndex, float x, float y, float w)
        {
            if (_isTemplateMode) return;
            var parent = _activeTabContent ?? _absoluteContainer;

            string defaultText = (options.Count > defaultIndex && defaultIndex >= 0) ? options[defaultIndex] : "";

            // Returns GameObject, out TMP_Dropdown
            var dropdownObj = UIFactory.CreateDropdown(parent, id, out var dropdownComp, defaultText, 14, null, options.ToArray());
            dropdownComp.value = defaultIndex;

            PositionElement(dropdownObj, x, y, w, 30);

            _dataContext[id] = () =>
            {
                if (dropdownComp.options.Count > 0 && dropdownComp.value >= 0 && dropdownComp.value < dropdownComp.options.Count)
                    return dropdownComp.options[dropdownComp.value].text;
                return "";
            };
            RegisterElement(id, dropdownObj);
        }

        // ==============================================================================================

        public void AddImage(string id, Assembly assembly, string imageName, float x, float y, float w, float h)
        {
            if (_isTemplateMode) return;
            var parent = _activeTabContent ?? _absoluteContainer;
            var imgObj = UIFactory.CreateUIObject(id, parent);
            var img = imgObj.AddComponent<Image>();

            var sprite = SpriteLoader.LoadSpriteFromAssembly(assembly, imageName, 100f);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;

                var gifData = SpriteLoader.GetGif(imageName);
                if (gifData != null)
                {
                    var player = imgObj.AddComponent(Il2CppType.Of<GifPlayer>()).Cast<GifPlayer>();
                    player.SetGifData(gifData);
                }
            }
            else
            {
                img.color = new Color(1f, 1f, 1f, 0.1f);
                _pendingSprites.Add(new PendingSprite { TargetImage = img, SpriteName = imageName, Assembly = assembly, Owner = imgObj });
            }

            PositionElement(imgObj, x, y, w, h);
            RegisterElement(id, imgObj);
        }

        public void AddButton(string id, string text, string command, float x = -1, float y = -1)
        {
            CreateGenericButton(id, typeof(Plugin).Assembly, text, command, null, x, y, -1, -1, () => { ExecuteCommand(command); });
        }

        public void AddButton(string id, Assembly assembly, string text, string command, string imageName, float x, float y, float w, float h)
        {
            CreateGenericButton(id, assembly, text, command, imageName, x, y, w, h, () => { ExecuteCommand(command); });
        }

        public void AddButtonWithCallback(string id, string text, Action callback, float x = -1, float y = -1)
        {
            CreateGenericButton(id, typeof(Plugin).Assembly, text, null, null, x, y, -1, -1, callback);
        }

        public void AddCloseButton(string id, string text, float x = -1, float y = -1)
        {
            CreateGenericButton(id, typeof(Plugin).Assembly, text, null, null, x, y, -1, -1, () => { this.SetActive(false); });
        }

        private void ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            string finalCommand = ParseCommandData(command);

            // === ZUI INTERNAL COMMAND INTERCEPTION ===
            // This prevents "zui_play" from going to chat/server
            if (finalCommand.StartsWith("zui_play", StringComparison.OrdinalIgnoreCase))
            {
                HandleZuiPlay(finalCommand);
                return;
            }
            // =========================================

            MessageService.EnqueueMessage(finalCommand);
        }
        private void HandleZuiPlay(string command)
        {
            // Format: zui_play <soundname> [volume]
            string[] parts = command.Split(' ');
            if (parts.Length < 2) return;

            string soundName = parts[1];
            float volume = 1.0f;
            if (parts.Length > 2) float.TryParse(parts[2], out volume);

            AudioLoader.Play(soundName, volume);
        }

        private string ParseCommandData(string command)
        {
            return Regex.Replace(command, @"\{([a-zA-Z0-9_]+)\}", match =>
            {
                string key = match.Groups[1].Value;
                if (_dataContext.TryGetValue(key, out var valueFunc))
                {
                    return valueFunc();
                }
                return match.Value;
            });
        }

        private void CreateGenericButton(string id, Assembly assembly, string text, string command, string customImageName, float x, float y, float width, float height, Action onClick)
        {
            GameObject parent = _activeTabContent ?? (_isTemplateMode ? _buttonScrollContent : _absoluteContainer);
            // Returns ButtonRef
            var btnRef = UIFactory.CreateButton(parent, id, text);

            if (_isTemplateMode) UIFactory.SetLayoutElement(btnRef.GameObject, minHeight: 32, flexibleWidth: 9999);
            else { float finalW = width > 0 ? width : 120; float finalH = height > 0 ? height : 30; PositionElement(btnRef.GameObject, x, y, finalW, finalH); }

            var img = btnRef.GameObject.GetComponent<Image>();

            if (!string.IsNullOrEmpty(customImageName))
            {
                var customSprite = SpriteLoader.LoadSpriteFromAssembly(assembly, customImageName, 100f);
                if (customSprite != null)
                {
                    img.sprite = customSprite; img.type = Image.Type.Simple; img.color = Color.white;
                    btnRef.Component.transition = Selectable.Transition.ColorTint;

                    var gifData = SpriteLoader.GetGif(customImageName);
                    if (gifData != null)
                    {
                        var player = btnRef.GameObject.AddComponent(Il2CppType.Of<GifPlayer>()).Cast<GifPlayer>();
                        player.SetGifData(gifData);
                    }
                }
                else
                {
                    img.color = new Color(1f, 1f, 1f, 0.1f);
                    _pendingSprites.Add(new PendingSprite { TargetImage = img, SpriteName = customImageName, Assembly = assembly, Owner = btnRef.GameObject });
                }
            }
            else
            {
                var zuiAssembly = Assembly.GetExecutingAssembly();
                var normalSprite = SpriteLoader.LoadSpriteFromAssembly(zuiAssembly, "button.png", 100f, new Vector4(10, 10, 10, 10));
                var selectedSprite = SpriteLoader.LoadSpriteFromAssembly(zuiAssembly, "button_selected.png", 100f, new Vector4(10, 10, 10, 10));

                if (normalSprite != null)
                {
                    img.sprite = normalSprite; img.type = Image.Type.Sliced; img.color = Color.white;
                    if (selectedSprite != null)
                    {
                        var comp = btnRef.Component; comp.transition = Selectable.Transition.SpriteSwap;
                        var state = comp.spriteState;
                        state.highlightedSprite = selectedSprite; state.pressedSprite = selectedSprite; state.selectedSprite = selectedSprite;
                        comp.spriteState = state;
                    }
                }
                else
                {
                    _pendingSprites.Add(new PendingSprite { TargetImage = img, SpriteName = "button.png", Assembly = zuiAssembly, Owner = btnRef.GameObject });
                }
            }
            if (onClick != null) btnRef.OnClick = onClick;
            RegisterElement(id, btnRef.GameObject);
        }

        public void RemoveElement(string id)
        {
            if (_elements.TryGetValue(id, out var obj)) { if (obj != null) UnityEngine.Object.Destroy(obj); _elements.Remove(id); }
            _dataContext.Remove(id);
        }

        private void RegisterElement(string id, GameObject obj) { RemoveElement(id); _elements[id] = obj; }

        private void PositionElement(GameObject obj, float x, float y, float w, float h)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            RecalculateScrollHeight();
        }
        #endregion
        internal override void Reset()
        {
            ZUI.API.ModRegistry.OnButtonsChanged -= OnRegistryChanged;

            if (_absoluteContainer != null)
            {
                for (int i = _absoluteContainer.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(_absoluteContainer.transform.GetChild(i).gameObject);
                }
            }

            if (_tabBar != null)
            {
                for (int i = _tabBar.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(_tabBar.transform.GetChild(i).gameObject);
                }
                _tabBar.SetActive(false);
            }

            foreach (var el in _elements.Values) { if (el != null) UnityEngine.Object.Destroy(el); }
            _elements.Clear();
            _dataContext.Clear();
            _radioGroups.Clear();
            _pendingSprites.Clear();
            _tabs.Clear();

            _activeTabContent = null;

            ZUI.API.ModRegistry.OnButtonsChanged += OnRegistryChanged;
        }

        public override void Destroy()
        {
            ZUI.API.ModRegistry.OnButtonsChanged -= OnRegistryChanged;
            base.Destroy();
        }
    }
}