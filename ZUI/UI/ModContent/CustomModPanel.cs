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
using ZUI.Utils;

namespace ZUI.UI.ModContent
{
    public class CustomModPanel : ResizeablePanelBase
    {
        public string PluginName { get; private set; }
        public string WindowId { get; private set; }
        public override string PanelId => $"{PluginName}_{WindowId}";
        public override PanelType PanelType => PanelType.Base;

        private int _initialWidth;
        private int _initialHeight;
        public override int MinWidth => 100;
        public override int MinHeight => 100;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new(0.5f, 0.5f);

        private bool _isTemplateMode;
        private GameObject _textScrollContent;
        private GameObject _buttonScrollContent;
        private GameObject _absoluteContainer;
        private Image _bgImage;
        private Image _overlayImage; // Track the inner container image to force transparency

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
            _isTemplateMode = false;
            _initialWidth = width;
            _initialHeight = height;
            Rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
        }

        public void SetTitleBarVisibility(bool visible)
        {
            if (TitleBar != null) TitleBar.SetActive(visible);
        }

        // --- FIX: Force Colors on Update ---
        public override void Update()
        {
            base.Update();

            // 1. Force Main Panel Background to White (so sprite shows)
            if (_bgImage != null && _bgImage.sprite != null && _bgImage.color != Color.white)
            {
                _bgImage.color = Color.white;
            }

            // 2. Force Inner ScrollView/Layout Background to Transparent (Remove Red Overlay)
            if (_overlayImage != null && _overlayImage.color.a > 0f)
            {
                _overlayImage.color = Color.clear;
            }
        }

        protected override void ConstructPanelContent()
        {
            base.ConstructPanelContent();
            SetTitle($"{PluginName} - {WindowId}");

            // --- MAIN PANEL BACKGROUND ---
            var panelSprite = SpriteLoader.LoadSpriteFromAssembly(typeof(Plugin).Assembly, "panel.png", 100f, new Vector4(30, 30, 30, 30));

            _bgImage = ContentRoot.GetComponent<Image>();
            if (_bgImage == null) _bgImage = ContentRoot.AddComponent<Image>();

            if (panelSprite != null)
            {
                _bgImage.sprite = panelSprite;
                _bgImage.type = Image.Type.Sliced;
                _bgImage.color = Color.white;
            }
            else
            {
                _bgImage.sprite = null;
                _bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            }

            // --- LAYOUT CONSTRUCTION ---
            if (_isTemplateMode)
            {
                TitleBar.SetActive(true);
                Dragger.DraggableArea = TitleBar.GetComponent<RectTransform>();
                ConstructTemplateLayout();
            }
            else
            {
                TitleBar.SetActive(false);
                Dragger.DraggableArea = Rect;
                ConstructAbsoluteLayout();
            }

            // Close button handled by ResizeablePanelBase logic now, 
            // but we can keep specific logic here if we need to reposition it differently.
            // Since ResizeablePanelBase adds it to Rect (UIRoot), it sits on top of everything.
        }

        private void ConstructTemplateLayout()
        {
            var splitGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "SplitGroup", true, true, true, true, 0, new Vector4(5, 5, 5, 5), Color.clear);
            UIFactory.SetLayoutElement(splitGroup, flexibleWidth: 9999, flexibleHeight: 9999);

            // Capture the image of this group to ensure it stays transparent
            _overlayImage = splitGroup.GetComponent<Image>();
            if (_overlayImage != null) _overlayImage.color = Color.clear;

            var leftScrollRoot = UIFactory.CreateScrollView(splitGroup, "TextScroll", out _textScrollContent, out _, Color.clear);
            UIFactory.SetLayoutElement(leftScrollRoot, flexibleWidth: 1, flexibleHeight: 9999);

            var textVlg = _textScrollContent.GetComponent<VerticalLayoutGroup>();
            textVlg.spacing = 8;
            textVlg.childAlignment = TextAnchor.UpperLeft;
            textVlg.childControlWidth = true;
            textVlg.childForceExpandWidth = true;
            textVlg.padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 };

            var separator = UIFactory.CreateUIObject("Separator", splitGroup);
            UIFactory.SetLayoutElement(separator, minWidth: 2, flexibleWidth: 0, flexibleHeight: 9999);
            var sepImg = separator.AddComponent<Image>();
            sepImg.color = new Color(1f, 1f, 1f, 0.2f);

            var rightScrollRoot = UIFactory.CreateScrollView(splitGroup, "ButtonScroll", out _buttonScrollContent, out _, Color.clear);
            UIFactory.SetLayoutElement(rightScrollRoot, flexibleWidth: 1, flexibleHeight: 9999);

            var btnVlg = _buttonScrollContent.GetComponent<VerticalLayoutGroup>();
            btnVlg.spacing = 5;
            btnVlg.childAlignment = TextAnchor.UpperCenter;
            btnVlg.childControlWidth = true;
            btnVlg.childForceExpandWidth = true;
            btnVlg.padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 };
        }

        private void ConstructAbsoluteLayout()
        {
            // --- SCROLLVIEW OVERLAY FIX ---
            // Create ScrollView. The last parameter is 'bgColor'. We pass Color.clear.
            GameObject scrollRoot = UIFactory.CreateScrollView(ContentRoot, "AbsoluteScroll", out _absoluteContainer, out var scrollbar, Color.clear);

            // Capture the image component of this ScrollView root to enforce transparency in Update()
            _overlayImage = scrollRoot.GetComponent<Image>();
            if (_overlayImage != null) _overlayImage.color = Color.clear;

            UIFactory.SetLayoutElement(scrollRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            // Ensure Mask for clipping
            var viewport = _absoluteContainer.transform.parent;
            if (viewport != null)
            {
                var mask = viewport.GetComponent<RectMask2D>();
                if (mask == null) viewport.gameObject.AddComponent<RectMask2D>();
            }

            // Disable Auto-Layout for absolute positioning
            var vlg = _absoluteContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg) UnityEngine.Object.DestroyImmediate(vlg);

            var csf = _absoluteContainer.GetComponent<ContentSizeFitter>();
            if (csf) UnityEngine.Object.DestroyImmediate(csf);

            // Configure Content Rect
            var rect = _absoluteContainer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(_initialWidth, _initialHeight);
        }

        #region Public Modification Methods

        public void AddCategory(string name, float x = -1, float y = -1)
        {
            if (_isTemplateMode)
            {
                var label = UIFactory.CreateLabel(_buttonScrollContent, $"Cat_{name}", name, TextAlignmentOptions.Left);
                label.TextMesh.fontSize = 14;
                label.TextMesh.fontStyle = FontStyles.Bold;
                label.TextMesh.color = new Color(1f, 0.8f, 0.4f);
                UIFactory.SetLayoutElement(label.GameObject, minHeight: 30, flexibleWidth: 9999);
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

            if (_isTemplateMode) UIFactory.SetLayoutElement(btn.GameObject, minHeight: 32, flexibleWidth: 9999);
            else PositionElement(btn.GameObject, x, y, 120, 30);

            var normalSprite = SpriteLoader.LoadSpriteFromAssembly(typeof(Plugin).Assembly, "button.png", 100f, new Vector4(10, 10, 10, 10));
            var selectedSprite = SpriteLoader.LoadSpriteFromAssembly(typeof(Plugin).Assembly, "button_selected.png", 100f, new Vector4(10, 10, 10, 10));

            if (normalSprite != null)
            {
                var img = btn.GameObject.GetComponent<Image>();
                if (img)
                {
                    img.sprite = normalSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                if (selectedSprite != null)
                {
                    var comp = btn.Component;
                    comp.transition = Selectable.Transition.SpriteSwap;
                    var state = comp.spriteState;
                    state.highlightedSprite = selectedSprite;
                    state.pressedSprite = selectedSprite;
                    state.selectedSprite = selectedSprite;
                    comp.spriteState = state;
                }
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

            if (_absoluteContainer != null)
            {
                var contentRect = _absoluteContainer.GetComponent<RectTransform>();
                float requiredHeight = y + h + 20;
                if (requiredHeight > contentRect.sizeDelta.y)
                {
                    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, requiredHeight);
                }
            }
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