using System;
using ZUI.Utils;
using UnityEngine;

namespace ZUI.UI.CustomLib.Util;

public static class Theme
{
    // Base colour palette
    public static Color Level1 {get; private set; }
    public static Color Level2 {get; private set; }
    public static Color Level3 {get; private set; }
    public static Color Level4 {get; private set; }
    public static Color Level5 { get; private set; }

    // Colour constants
    public static Color DefaultBar = Level4;
    public static Color Highlight = Color.yellow;
    public static Color PositiveChange = Color.yellow;
    public static Color NegativeChange = Color.red;

    public static Color DarkBackground        {get; private set; }
    public static Color PanelBackground       {get; private set; }
    public static Color SliderFill            {get; private set; }
    public static Color SliderHandle          {get; private set; }
    public static Color SliderNormal          {get; private set; }
    public static Color SliderHighlighted     {get; private set; }
    public static Color SliderPressed         {get; private set; }
    public static Color DefaultText           {get; private set; }
    public static Color PlaceHolderText       {get; private set; }
    public static Color SelectableNormal      {get; private set; }
    public static Color SelectableHighlighted {get; private set; }
    public static Color SelectablePressed     {get; private set; }
    public static Color ScrollbarNormal       {get; private set; }
    public static Color ScrollbarHighlighted  {get; private set; }
    public static Color ScrollbarPressed      {get; private set; }
    public static Color ScrollbarDisabled     {get; private set; }
    public static Color InputFieldNormal      {get; private set; }
    public static Color InputFieldHighlighted {get; private set; }
    public static Color InputFieldPressed     {get; private set; }
    public static Color ViewportBackground    {get; private set; }
    public static Color White                 {get; private set; }
    public static Color ToggleNormal          {get; private set; }
    public static Color ToggleCheckMark { get; private set; }

    public static Color DropDownScrollBarNormal     {get; private set; }
    public static Color DropDownScrollbarHighlighted{get; private set; }
    public static Color DropDownScrollbarPressed    {get; private set; }
    public static Color DropDownToggleNormal        {get; private set; }
    public static Color DropDownToggleHighlighted { get; private set; }

    private static float _opacity;
    public static float Opacity
    {
        get => _opacity;
        set
        {
            _opacity = value;
            UpdateColors();
        }
    }

    static Theme()
    {
        Opacity = 0.8f;
    }

    private static void UpdateColors()
    {
        PanelBackground = new Color(0.07f, 0.07f, 0.07f, Opacity);
        DarkBackground = new Color(0.07f, 0.07f, 0.07f, Opacity);
        SliderFill = new Color(0.3f, 0.3f, 0.3f, Opacity);
        SliderHandle = new Color(0.5f, 0.5f, 0.5f, Opacity);
        SliderNormal = new Color(0.4f, 0.4f, 0.4f, Opacity);
        SliderHighlighted = new Color(0.55f, 0.55f, 0.55f, Opacity);
        SliderPressed = new Color(0.3f, 0.3f, 0.3f, Opacity);
        SelectableNormal = new Color(0.2f, 0.2f, 0.2f, Opacity);
        SelectableHighlighted = new Color(0.3f, 0.3f, 0.3f, Opacity);
        SelectablePressed = new Color(0.15f, 0.15f, 0.15f, Opacity);
        DefaultText = Color.white;
        White = Color.white;
        PlaceHolderText = SliderHandle;

        InputFieldNormal = new Color(1f, 1f, 1f, Opacity);
        InputFieldHighlighted = new Color(0.95f, 0.95f, 0.95f, Opacity);
        InputFieldPressed = new Color(0.78f, 0.78f, 0.78f, Opacity);
        ToggleNormal = new Color(0f, 0f, 0f, Opacity);
        ToggleCheckMark = new Color(0.6f, 0.7f, 0.6f, Opacity);
        ViewportBackground = new Color(0.07f, 0.07f, 0.07f, Opacity);
        ScrollbarNormal = new Color(0.4f, 0.4f, 0.4f, Opacity);
        ScrollbarHighlighted = new Color(0.5f, 0.5f, 0.5f, Opacity);
        ScrollbarPressed = new Color(0.3f, 0.3f, 0.3f, Opacity);
        ScrollbarDisabled = new Color(0.5f, 0.5f, 0.5f, Opacity);

        Level1 = new Color(0.64f, 0, 0, Opacity);
        Level2 = new Color(0.72f, 0.43f, 0, Opacity);
        Level3 = new Color(1, 0.83f, 0.45f, Opacity);
        Level4 = new Color(0.47f, 0.74f, 0.38f, Opacity);
        Level5 = new Color(0.18f, 0.53f, 0.67f, Opacity);

        DropDownScrollBarNormal = new Color(0.45f, 0.45f, 0.45f, Opacity);
        DropDownScrollbarHighlighted = new Color(0.6f, 0.6f, 0.6f, Opacity);
        DropDownScrollbarPressed = new Color(0.4f, 0.4f, 0.4f, Opacity);
        DropDownToggleNormal = new Color(0.35f, 0.35f, 0.35f, Opacity);
        DropDownToggleHighlighted = new Color(0.25f, 0.25f, 0.25f, Opacity);
    }
}
