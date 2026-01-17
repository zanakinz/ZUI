using System;
using System.Collections.Generic;
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

        // References
        private GameObject _textScrollContent;
        private GameObject _buttonScrollContent;
        private GameObject _absoluteContainer;

        // Element Tracking for Removal
        private Dictionary<string, GameObject> _elements = new();

        public CustomModPanel(UIBase owner, string pluginName, string windowId, string template) : base(owner)
        {
            PluginName = pluginName;
            WindowId = windowId;
            _isTemplateMode = true;

            switch (template.ToLower())
            {
                case "small": _initialWidth = 400; _initialHeight = 300; break;
                case "medium": _initialWidth = 600; _initialHeight = 450; break;
                case "large": _initialWidth = 800; _initialHeight = 600; break;
                default: _initialWidth = 600; _initialHeight = 450; break;
            }

            Rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
        }

        public CustomModPanel(UIBase owner, string pluginName, string windowId, int width, int height) : base(owner)
        {
            PluginName = pluginName;
            WindowId = windowId;
            _isTemplateMode = false; // Custom dimensions implies absolute positioning mode
            _initialWidth = width;
            _initialHeight = height;

            Rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
        }

        public void SetTitleBarVisibility(bool visible)
        {
            if (TitleBar != null) TitleBar.SetActive(visible);
        }

        protected override void ConstructPanelContent()
        {
            base.ConstructPanelContent(); // Setup Dragger
            SetTitle($"{PluginName} - {WindowId}");

            // 1. Create the main content layout
            if (_isTemplateMode)
            {
                // Template Mode: Enable TitleBar and build split layout
                TitleBar.SetActive(true);
                Dragger.DraggableArea = TitleBar.GetComponent<RectTransform>();
                ConstructTemplateLayout();
            }
            else
            {
                // Canvas Mode: Disable TitleBar (default) and build absolute layout
                TitleBar.SetActive(false);
                Dragger.DraggableArea = Rect;
                ConstructAbsoluteLayout();
            }

            // 2. Add the Overlay Close Button (Always visible on top right)
            CreateOverlayCloseButton();
        }

        private void CreateOverlayCloseButton()
        {
            // Attach directly to the Panel Rect (UIRoot)
            // The UIRoot has a VerticalLayoutGroup, so we MUST use ignoreLayout
            var closeBtnObj = UIFactory.CreateButton(Rect.gameObject, "OverlayCloseButton", "X");

            // Explicitly add LayoutElement and set ignoreLayout
            var layout = closeBtnObj.GameObject.GetComponent<LayoutElement>();
            if (!layout) layout = closeBtnObj.GameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            // Position Top-Right
            var btnRect = closeBtnObj.GameObject.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(1, 1);

            // Adjust offset
            btnRect.anchoredPosition = new Vector2(-5, -5);
            btnRect.sizeDelta = new Vector2(24, 24);

            // Styling (Red "Close" look)
            var colors = closeBtnObj.Component.colors;
            colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
            closeBtnObj.Component.colors = colors;

            // Remove the default outline to make it look cleaner
            var outline = closeBtnObj.GameObject.GetComponent<UnityEngine.UI.Outline>();
            if (outline) UnityEngine.Object.Destroy(outline);

            closeBtnObj.OnClick = () => { this.SetActive(false); };
        }

        private void ConstructTemplateLayout()
        {
            // 1. Create Horizontal Split Group
            // Padding Top (5) + TitleBar Height (~25) logic is handled by VerticalLayout of ContentRoot.
            var splitGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "SplitGroup", true, true, true, true, 0, new Vector4(5, 5, 5, 5), new Color(0.1f, 0.1f, 0.1f, 0.95f));

            UIFactory.SetLayoutElement(splitGroup, flexibleWidth: 9999, flexibleHeight: 9999);

            // 2. Left Side (Text/Info) - ScrollView
            var leftScrollRoot = UIFactory.CreateScrollView(splitGroup, "TextScroll", out _textScrollContent, out _, new Color(0, 0, 0, 0));

            // CRITICAL: Set LayoutElement on the ScrollView Root to fill 50% width and 100% available height
            UIFactory.SetLayoutElement(leftScrollRoot, flexibleWidth: 1, flexibleHeight: 9999);

            // Visual Debug: Subtle background for left side
            leftScrollRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            // Configure Text Content Layout
            var textVlg = _textScrollContent.GetComponent<VerticalLayoutGroup>();
            textVlg.spacing = 8;
            textVlg.childAlignment = TextAnchor.UpperLeft;
            textVlg.childForceExpandHeight = false;
            textVlg.childControlWidth = true; // Text expands to width
            textVlg.childForceExpandWidth = true;
            textVlg.padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 };

            // 3. Separator
            var separator = UIFactory.CreateUIObject("Separator", splitGroup);
            UIFactory.SetLayoutElement(separator, minWidth: 2, flexibleWidth: 0, flexibleHeight: 9999);
            var sepImg = separator.AddComponent<Image>();
            sepImg.color = new Color(1f, 1f, 1f, 0.2f);

            // 4. Right Side (Buttons/Commands) - ScrollView
            var rightScrollRoot = UIFactory.CreateScrollView(splitGroup, "ButtonScroll", out _buttonScrollContent, out _, new Color(0, 0, 0, 0));

            // CRITICAL: Set LayoutElement for right side (50% width)
            UIFactory.SetLayoutElement(rightScrollRoot, flexibleWidth: 1, flexibleHeight: 9999);

            // Visual Debug: Subtle background for right side
            rightScrollRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            // Configure Button Content Layout
            var btnVlg = _buttonScrollContent.GetComponent<VerticalLayoutGroup>();
            btnVlg.spacing = 5;
            btnVlg.childAlignment = TextAnchor.UpperCenter;
            btnVlg.childForceExpandHeight = false;
            btnVlg.childControlHeight = false;
            btnVlg.childControlWidth = true;
            btnVlg.childForceExpandWidth = true;
            btnVlg.padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 };
        }

        private void ConstructAbsoluteLayout()
        {
            // 1. Create ScrollView structure
            // This adds the Viewport (Mask) and Scrollbars automatically.
            // Using a dark background for the canvas.
            GameObject scrollRoot = UIFactory.CreateScrollView(ContentRoot, "AbsoluteScroll", out _absoluteContainer, out _, new Color(0.1f, 0.1f, 0.1f, 0.95f));

            // Ensure ScrollView fills the entire window
            UIFactory.SetLayoutElement(scrollRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            // 2. Disable Auto-Layout on Content
            // CreateScrollView adds VLG and ContentSizeFitter. We must destroy them to allow absolute positioning (X, Y).
            var vlg = _absoluteContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg) UnityEngine.Object.DestroyImmediate(vlg);

            var csf = _absoluteContainer.GetComponent<ContentSizeFitter>();
            if (csf) UnityEngine.Object.DestroyImmediate(csf);

            // 3. Configure Content Rect
            var rect = _absoluteContainer.GetComponent<RectTransform>();

            // Anchor Top-Left (0, 1) so (0,0) is always the top-left corner
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);

            // Set size equal to the initial request. 
            // If the window is resized smaller than this, scrollbars will appear.
            rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
            rect.anchoredPosition = Vector2.zero;
        }

        #region Public Modification Methods

        public void AddCategory(string name, float x = -1, float y = -1)
        {
            if (_isTemplateMode)
            {
                var label = UIFactory.CreateLabel(_buttonScrollContent, $"Cat_{name}", name, TextAlignmentOptions.Left);
                label.TextMesh.fontSize = 14;
                label.TextMesh.fontStyle = FontStyles.Bold;
                label.TextMesh.color = new Color(1f, 0.8f, 0.4f); // Gold

                var layout = UIFactory.SetLayoutElement(label.GameObject, minHeight: 30, flexibleWidth: 9999);
                RegisterElement(name, label.GameObject);
            }
            else
            {
                var label = UIFactory.CreateLabel(_absoluteContainer, $"Cat_{name}", name, TextAlignmentOptions.Left);
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

                var fitter = label.GameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                UIFactory.SetLayoutElement(label.GameObject, minHeight: 0, flexibleHeight: 0, flexibleWidth: 9999);
                RegisterElement(id, label.GameObject);
            }
            else
            {
                var label = UIFactory.CreateLabel(_absoluteContainer, id, content, TextAlignmentOptions.TopLeft);
                PositionElement(label.GameObject, x, y, 200, 50);
                RegisterElement(id, label.GameObject);
            }
        }

        public void AddButton(string id, string text, string command, float x = -1, float y = -1)
        {
            CreateGenericButton(id, text, x, y, () => { if (!string.IsNullOrEmpty(command)) MessageService.EnqueueMessage(command); });
        }

        public void AddButtonWithCallback(string id, string text, Action callback, float x = -1, float y = -1)
        {
            CreateGenericButton(id, text, x, y, callback);
        }

        public void AddCloseButton(string id, string text, float x = -1, float y = -1)
        {
            CreateGenericButton(id, text, x, y, () => { this.SetActive(false); });
        }

        private void CreateGenericButton(string id, string text, float x, float y, Action onClick)
        {
            GameObject parent = _isTemplateMode ? _buttonScrollContent : _absoluteContainer;
            var btn = UIFactory.CreateButton(parent, id, text);

            if (_isTemplateMode)
            {
                UIFactory.SetLayoutElement(btn.GameObject, minHeight: 32, flexibleWidth: 9999);
            }
            else
            {
                PositionElement(btn.GameObject, x, y, 120, 30);
            }

            if (onClick != null) btn.OnClick = onClick;
            RegisterElement(id, btn.GameObject);
        }

        public void RemoveElement(string id)
        {
            if (_elements.TryGetValue(id, out var obj))
            {
                if (obj != null) UnityEngine.Object.Destroy(obj);
                _elements.Remove(id);
            }
        }

        private void RegisterElement(string id, GameObject obj)
        {
            RemoveElement(id);
            _elements[id] = obj;
        }

        private void PositionElement(GameObject obj, float x, float y, float w, float h)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }
        #endregion
        internal override void Reset()
        {
            _elements.Clear();
            if (_textScrollContent) foreach (Transform child in _textScrollContent.transform) UnityEngine.Object.Destroy(child.gameObject);
            if (_buttonScrollContent) foreach (Transform child in _buttonScrollContent.transform) UnityEngine.Object.Destroy(child.gameObject);
            if (_absoluteContainer) foreach (Transform child in _absoluteContainer.transform) UnityEngine.Object.Destroy(child.gameObject);
        }
    }
}