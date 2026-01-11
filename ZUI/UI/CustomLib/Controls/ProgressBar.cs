using System;
using ZUI.UI.CustomLib.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIFactory = ZUI.UI.UniverseLib.UI.UIFactory;

namespace ZUI.UI.CustomLib.Controls;

public class ProgressBar
{
    public const int BaseWidth = 200;
    public const int BarHeight = 22;

    private readonly GameObject _contentBase;
    private readonly CanvasGroup _canvasGroup;
    private readonly Outline _highlight;
    private readonly TextMeshProUGUI _tooltipText;
    private readonly Image _fillImage;
    private readonly GameObject _maskObject;
    private readonly TextMeshProUGUI _changeText;
    private readonly RectTransform _maskRect;

    private readonly FrameTimer _timer = new();
    private int _alertTimeRemainingMs = 0;
    private bool _alertTransitionOff = true;
    private const int TaskIterationDelay = 15;

    // Timeline constants
    private const int FlashInLengthMs = 150;
    private const int FlashLengthMs = 150;
    private const int FlashOutLengthMs = 150;
    private const int VisibleLengthMs = 500;
    private const int FadeOutLengthMs = 500;
    private const int FlashPulseInEnds = FlashLengthMs + FlashOutLengthMs;
    private const int FlashPulseLengthMs = FlashInLengthMs + FlashLengthMs + FlashOutLengthMs;
    private const int FlashPulseEndsMs = VisibleLengthMs + FadeOutLengthMs;
    private const int AlertAnimationLength = FlashPulseLengthMs * 3 + FlashPulseEndsMs;

    public bool IsActive => _contentBase.active;

    public event EventHandler ProgressBarMinimised;

    private ActiveState _activeState = ActiveState.Unchanged;
    private float _currentProgress = 0f;

    public ProgressBar(GameObject panel, Color fillColor, Color backColor)
    {
        // Create base container
        _contentBase = UIFactory.CreateUIObject("MaskedProgressBar", panel);
        UIFactory.SetLayoutElement(_contentBase, minHeight: BarHeight, flexibleWidth: 9999, flexibleHeight: 0);

        // Set rect transform to fill width
        RectTransform rect = _contentBase.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.sizeDelta = new Vector2(0, BarHeight);

        // Add background image
        var bgImage = _contentBase.AddComponent<Image>();
        bgImage.color = backColor;

        // Add canvas group and outline
        _canvasGroup = _contentBase.AddComponent<CanvasGroup>();
        _highlight = _contentBase.AddComponent<Outline>();
        _highlight.effectColor = backColor;

        // Create a mask object for clipping the fill
        _maskObject = UIFactory.CreateUIObject("MaskRect", _contentBase);
        _maskRect = _maskObject.GetComponent<RectTransform>();

        // Configure mask rect to start with zero width
        _maskRect.anchorMin = new Vector2(0, 0);
        _maskRect.anchorMax = new Vector2(0, 1);
        _maskRect.pivot = new Vector2(0, 0.5f);
        _maskRect.sizeDelta = new Vector2(0, 0);

        // Add mask component
        var mask = _maskObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Add a graphic to make the mask work
        var maskImage = _maskObject.AddComponent<Image>();
        maskImage.color = Color.white;

        // Create a fill image inside the mask
        var fillObj = UIFactory.CreateUIObject("Fill", _maskObject);
        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.color = fillColor;

        // Set fill image to cover entire bar area
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Add tooltip text on top of everything
        var tooltipObj = UIFactory.CreateUIObject("Tooltip", _contentBase);
        _tooltipText = tooltipObj.AddComponent<TextMeshProUGUI>();
        UIFactory.SetDefaultTextValues(_tooltipText);
        _tooltipText.alignment = TextAlignmentOptions.Center;
        _tooltipText.text = "";

        // Position tooltip to cover entire bar
        RectTransform tooltipRect = tooltipObj.GetComponent<RectTransform>();
        tooltipRect.anchorMin = Vector2.zero;
        tooltipRect.anchorMax = Vector2.one;
        tooltipRect.sizeDelta = Vector2.zero;

        // Add outline to tooltip
        var tooltipOutline = tooltipObj.AddComponent<Outline>();
        tooltipOutline.effectColor = Color.black;

        // Add change text indicator
        var changeTextObj = UIFactory.CreateUIObject("ChangeText", _contentBase);
        _changeText = changeTextObj.AddComponent<TextMeshProUGUI>();
        UIFactory.SetDefaultTextValues(_changeText);
        _changeText.alignment = TextAlignmentOptions.MidlineLeft;
        _changeText.text = "";

        // Position change text
        RectTransform changeTextRect = changeTextObj.GetComponent<RectTransform>();
        changeTextRect.anchorMin = new Vector2(0, 0.5f);
        changeTextRect.anchorMax = new Vector2(0, 0.5f);
        changeTextRect.pivot = new Vector2(0, 0.5f);
        changeTextRect.anchoredPosition = new Vector2(5, 0);
        changeTextRect.sizeDelta = new Vector2(50, 20);

        // Initialize change text as inactive
        _changeText.gameObject.SetActive(false);

        // Set up animation timer
        _timer.Initialise(
            AlertIteration,
            TimeSpan.FromMilliseconds(TaskIterationDelay),
            false);
    }

    public void Reset()
    {
        _timer.Stop();
    }

    public void SetProgress(float progress, string header, string tooltip, ActiveState activeState, Color colour,
        string changeText, bool flash)
    {
        // Store current progress value
        _currentProgress = Mathf.Clamp01(progress);
        UpdateMaskWidth();

        // Update visual elements
        _fillImage.color = colour;
        _tooltipText.text = tooltip;
        _changeText.text = changeText;
        _changeText.color = changeText.StartsWith("-") ? Theme.NegativeChange : Theme.PositiveChange;

        // Handle active state
        switch (activeState)
        {
            case ActiveState.NotActive:
                FadeOut();
                break;
            case ActiveState.Active:
            case ActiveState.OnlyActive:
                _activeState = ActiveState.Active;
                _contentBase.SetActive(true);
                _contentBase.transform.parent.gameObject.SetActive(true);
                _canvasGroup.alpha = 1;
                _alertTransitionOff = false;
                break;
            case ActiveState.Unchanged:
                break;
        }

        // Handle flash animation
        if (flash)
        {
            _activeState = ActiveState.Active;
            _canvasGroup.alpha = 1;
            _alertTimeRemainingMs = AlertAnimationLength;
            _timer.Start();
        }
    }

    private void UpdateMaskWidth()
    {
        // Get parent width to calculate mask width
        RectTransform contentRect = _contentBase.GetComponent<RectTransform>();
        float parentWidth = contentRect.rect.width;

        // Set mask width based on progress percentage
        float width = parentWidth * _currentProgress;
        _maskRect.sizeDelta = new Vector2(width, 0);
    }

    public void FadeOut()
    {
        if (_alertTimeRemainingMs > 0)
        {
            if (!_alertTransitionOff)
            {
                _alertTimeRemainingMs = Math.Max(FadeOutLengthMs, _alertTimeRemainingMs);
                _alertTransitionOff = true;
            }
            _timer.Start();
        }
        else if (_activeState == ActiveState.Active)
        {
            _alertTimeRemainingMs = FadeOutLengthMs;
            _alertTransitionOff = true;
            _timer.Start();
        }
        else if (_activeState == ActiveState.Unchanged)
        {
            _activeState = ActiveState.NotActive;
            _contentBase.SetActive(false);
        }
    }

    private void AlertIteration()
    {
        // Update mask width in case parent size has changed
        UpdateMaskWidth();

        if (!IsActive) return;

        switch (_alertTimeRemainingMs)
        {
            case > FlashPulseEndsMs:
                // Flash pulse animation
                var flashPulseTimeMs = (_alertTimeRemainingMs - FlashPulseEndsMs) % FlashPulseLengthMs;
                switch (flashPulseTimeMs)
                {
                    case > FlashPulseInEnds:
                        // Fade in to full colour
                        _highlight.effectColor = Color.Lerp(Theme.Highlight, Color.black, Math.Max((float)(flashPulseTimeMs - FlashPulseInEnds) / FlashInLengthMs, 0));
                        break;
                    case > FlashOutLengthMs:
                        // Stay at full visibility
                        _highlight.effectColor = Theme.Highlight;
                        break;
                    case > 0:
                        // Start fading highlight out
                        _highlight.effectColor = Color.Lerp(Color.black, Theme.Highlight, Math.Max((float)flashPulseTimeMs / FlashLengthMs, 0));
                        break;
                }
                // Show change text
                _changeText.gameObject.SetActive(true);
                break;
            case > FadeOutLengthMs:
                // After flashing, maintain visibility
                _highlight.effectColor = Color.black;
                if (_changeText.gameObject.active) _changeText.gameObject.SetActive(false);
                break;
            case > 0:
                // Fade out
                if (_alertTransitionOff) _canvasGroup.alpha = Math.Min((float)_alertTimeRemainingMs / FadeOutLengthMs, 1.0f);
                else _alertTimeRemainingMs = 0;
                break;
            default:
                // Animation complete
                _timer.Stop();
                if (_alertTransitionOff)
                {
                    _activeState = ActiveState.NotActive;
                    _contentBase.SetActive(false);
                    OnProgressBarMinimised();
                }
                break;
        }

        _alertTimeRemainingMs = Math.Max(_alertTimeRemainingMs - TaskIterationDelay, 0);
    }

    private void OnProgressBarMinimised()
    {
        ProgressBarMinimised?.Invoke(this, EventArgs.Empty);
    }
}

