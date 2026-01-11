using System;

namespace ZUI.UI.CustomLib.Controls;

public class ToggleDraggerSettingButton : SettingsButtonBase
{
    private readonly Action<bool> _action;
    private bool currentState => State == "True";
    public ToggleDraggerSettingButton(Action<bool> action) : base("ShowDragAnchor", "true")
    {
        action(currentState);
        _action = action;
    }

    public override string PerformAction()
    {
        _action(!currentState);
        return $"{!currentState}";
    }

    protected override string Label()
    {
        var state = currentState ? "on" : "off";
        return $"Toggle drag anchor [{state}]";
    }
}
