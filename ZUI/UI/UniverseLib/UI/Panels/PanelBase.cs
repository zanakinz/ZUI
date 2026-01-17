using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZUI.Utils; // Added for SpriteLoader
using ButtonRef = ZUI.UI.UniverseLib.UI.Models.ButtonRef;
using Object = UnityEngine.Object;

namespace ZUI.UI.UniverseLib.UI.Panels;

public abstract class PanelBase : UIBehaviourModel, IPanelBase
{
    public UIBase Owner { get; }
    public abstract PanelType PanelType { get; }

    public abstract string PanelId { get; }

    public abstract int MinWidth { get; }
    public abstract int MinHeight { get; }
    public virtual int MaxWidth { get; }

    public abstract Vector2 DefaultAnchorMin { get; }
    public abstract Vector2 DefaultAnchorMax { get; }
    public virtual Vector2 DefaultPivot => Vector2.one * 0.5f;
    public virtual Vector2 DefaultPosition { get; }
    public virtual float Opacity { get; set; } = 1.0f;

    public virtual bool CanDrag { get; protected set; } = true;
    public virtual PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
    public PanelDragger Dragger { get; internal set; }

    public override GameObject UIRoot => uiRoot;
    protected GameObject uiRoot;
    public RectTransform Rect { get; private set; }
    public GameObject ContentRoot { get; protected set; }

    public GameObject TitleBar { get; private set; }
    private LabelRef TitleLabel { get; set; }
    public GameObject CloseButton { get; private set; }
    protected Toggle PinPanelToggleControl;

    public virtual bool IsPinned { get; protected set; }

    public PanelBase(UIBase owner)
    {
        Owner = owner;

        ConstructUI();

        // Add to owner
        Owner.Panels.AddPanel(this);
    }

    protected void ForceRecalculateBasePanelWidth(List<GameObject> data = null)
    {
        float contentWidth = 0;
        if (data != null)
        {
            foreach (var obj in data)
            {
                var child = obj.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(child);
                var width = LayoutUtility.GetPreferredWidth(child);
                contentWidth = Math.Max(contentWidth, width);
            }
        }
        else
        {
            foreach (var child in uiRoot.transform)
            {
                var childRect = child as RectTransform;
                if (childRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(childRect);
                    var width = LayoutUtility.GetPreferredWidth(childRect.GetComponent<RectTransform>());
                    contentWidth = Math.Max(contentWidth, width);
                }
            }
        }

        Rect.sizeDelta = new Vector2(contentWidth, Rect.sizeDelta.y);
    }

    public void SetTitle(string label)
    {
        TitleLabel.TextMesh.SetText(label);
    }

    public override void Destroy()
    {
        Owner.Panels.RemovePanel(this);
        base.Destroy();
    }

    public virtual void OnFinishResize()
    {
    }

    public virtual void OnFinishDrag()
    {
    }

    public override void SetActive(bool active)
    {
        if (Enabled != active)
            base.SetActive(active);

        if (!active)
        {
            Dragger.WasDragging = false;
        }
        else
        {
            UIRoot.transform.SetAsLastSibling();
            Owner.Panels.InvokeOnPanelsReordered();
        }
    }

    public void SetActiveOnly(bool active)
    {
        if (Enabled != active)
            base.SetActive(active);

        if (!active)
        {
            Dragger.WasDragging = false;
        }
        else
        {
            UIRoot.transform.SetAsLastSibling();
            Owner.Panels.InvokeOnPanelsReordered();
        }
    }

    protected virtual void OnClosePanelClicked()
    {
        SetActive(false);
    }

    // Setting size and position

    public virtual void SetDefaultSizeAndPosition()
    {
        Rect.localPosition = DefaultPosition;
        Rect.pivot = DefaultPivot;

        Rect.anchorMin = DefaultAnchorMin;
        Rect.anchorMax = DefaultAnchorMax;

        LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);

        EnsureValidPosition();
        EnsureValidSize();

        Dragger.OnEndResize();
    }

    public virtual void EnsureValidSize()
    {
        if (Rect.rect.width < MinWidth)
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
        if (MaxWidth > 0 && Rect.rect.width > MaxWidth)
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MaxWidth);

        if (Rect.rect.height < MinHeight)
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);

        Dragger.OnEndResize();
    }

    public virtual void EnsureValidPosition()
    {
        var scale = UniversalUI.uiBases.First().Panels.PanelHolder.GetComponent<RectTransform>().localScale.x;
        // Prevent panel going outside screen bounds
        Vector2 pos = Rect.anchoredPosition;
        Vector2 dimensions = Owner.Scaler.referenceResolution / scale;
        float halfW = dimensions.x * 0.5f;
        float halfH = dimensions.y * 0.5f;

        // Account for localScale by multiplying width and height
        float scaledWidth = Rect.rect.width;
        float scaledHeight = Rect.rect.height;

        // Calculate min/max positions accounting for scaled dimensions
        float minPosX = -halfW + scaledWidth * 0.5f;
        float maxPosX = halfW - scaledWidth * 0.5f;
        float minPosY = -halfH + scaledHeight * 0.5f;
        float maxPosY = halfH - scaledHeight * 0.5f;

        // Apply clamping to keep the panel within screen bounds
        pos.x = Math.Clamp(pos.x, minPosX, maxPosX);
        pos.y = Math.Clamp(pos.y, minPosY, maxPosY);
        Rect.anchoredPosition = pos;
    }

    // UI Construction

    protected abstract void ConstructPanelContent();

    protected virtual PanelDragger CreatePanelDragger() => new(this);

    public virtual void ConstructUI()
    {
        // create core canvas 
        uiRoot = UIFactory.CreatePanel(PanelId, Owner.Panels.PanelHolder, out GameObject contentRoot, opacity: Opacity);
        ContentRoot = contentRoot;
        Rect = uiRoot.GetComponent<RectTransform>();

        UIFactory.SetLayoutElement(ContentRoot, 0, 0, flexibleWidth: 9999, flexibleHeight: 9999);

        // Title bar
        TitleBar = UIFactory.CreateHorizontalGroup(ContentRoot, "TitleBar", false, true, true, true, 2,
            new Vector4(2, 2, 2, 2), Theme.PanelBackground);
        UIFactory.SetLayoutElement(TitleBar, minHeight: 25, flexibleHeight: 0);

        // Title text
        TitleLabel = UIFactory.CreateLabel(TitleBar, "TitleBar", PanelId, TextAlignmentOptions.Center, Theme.DefaultText, outlineWidth: 0.05f, fontSize: 16);
        UIFactory.SetLayoutElement(TitleLabel.GameObject, 50, 25, 9999, 0);

        // close button
        CloseButton = UIFactory.CreateUIObject("CloseHolder", TitleBar);
        UIFactory.SetLayoutElement(CloseButton, minHeight: 25, flexibleHeight: 0, minWidth: 30, flexibleWidth: 9999);
        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(CloseButton, false, false, true, true, 3, childAlignment: TextAnchor.MiddleRight);

        ButtonRef closeBtn = UIFactory.CreateButton(CloseButton, "CloseButton", "—"); // Default placeholder
        Object.Destroy(closeBtn.Component.gameObject.GetComponent<Outline>());
        UIFactory.SetLayoutElement(closeBtn.Component.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0);

        // --- CUSTOM CLOSE BUTTON LOGIC ---
        // Attempt to load the custom sprite from ZUI assembly
        var closeSprite = SpriteLoader.LoadSpriteFromAssembly(typeof(Plugin).Assembly, "close_button.png", 100f);

        if (closeSprite != null)
        {
            // IMAGE FOUND
            var img = closeBtn.Component.GetComponent<Image>();
            img.sprite = closeSprite;
            img.color = Color.white;

            // Clear text
            closeBtn.ButtonText.text = "";

            // Neutral colors for sprite mode
            var colors = closeBtn.Component.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            closeBtn.Component.colors = colors;
        }
        else
        {
            // FALLBACK: Red "X" Style
            closeBtn.ButtonText.text = "X";
            var colors = closeBtn.Component.colors;
            colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
            colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
            closeBtn.Component.colors = colors;
        }

        closeBtn.OnClick += OnClosePanelClicked;

        if (!(CanDrag || CanResize > 0)) TitleBar.SetActive(false);

        // Panel dragger

        Dragger = CreatePanelDragger();
        Dragger.OnFinishResize += OnFinishResize;
        Dragger.OnFinishDrag += OnFinishDrag;

        // content (abstract)

        ConstructPanelContent();
        SetDefaultSizeAndPosition();

        CoroutineUtility.StartCoroutine(LateSetupCoroutine());
    }

    private IEnumerator LateSetupCoroutine()
    {
        yield return null;

        LateConstructUI();
    }

    protected virtual void LateConstructUI()
    {
        SetDefaultSizeAndPosition();
    }

    public override void Update()
    {
        // Close on Escape if this panel is active and top-most or focused
        // For simplicity, if any panel is active and Escape is pressed, we close it if it's not the Base panel.
        if (Enabled && Input.GetKeyDown(KeyCode.Escape))
        {
            if (PanelType != PanelType.Base)
            {
                SetActive(false);
            }
        }
    }
}