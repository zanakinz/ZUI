using System.Collections.Generic;
using ZUI.UI.UniverseLib.UI;
using UnityEngine;

namespace ZUI.UI.CustomLib.Controls;

public class UIScaleSettingButton : SettingsButtonBase
{
    private readonly List<(string, float)> _scales =
    [
        ("small", 1f),
        ("normal", 1.5f),
        ("medium", 2f)
    ];

    private int _scaleIndex;

    public UIScaleSettingButton() : base("UIScale", "normal")
    {
        _scaleIndex = State switch
        {
            "small" => 0,
            "normal" => 1,
            "medium" => 2,
            _ => 1
        };

        ApplyScale();
    }

    public override string PerformAction()
    {
        _scaleIndex = (_scaleIndex + 1) % _scales.Count;
        ApplyScale();
        Setting.Value = _scales[_scaleIndex].Item1;
        return _scales[_scaleIndex].Item1;
    }

    protected override string Label()
    {
        return $"Toggle screen size [{_scales[_scaleIndex].Item1}]";
    }

    private void ApplyScale()
    {
        foreach (var uiBase in UniversalUI.uiBases)
        {
            uiBase.Panels.PanelHolder.GetComponent<RectTransform>().localScale = new Vector3(_scales[_scaleIndex].Item2, _scales[_scaleIndex].Item2, 1f);
            //uiBase.Panels.UpdatePanelsPlacement();
            uiBase.Panels.ValidatePanels();
        }
    }
}
