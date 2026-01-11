using System;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using UIBase = ZUI.UI.UniverseLib.UI.UIBase;

namespace ZUI.UI.CustomLib.Panel;

public abstract class ResizeablePanelBase : PanelBase
{
    protected ResizeablePanelBase(UIBase owner) : base(owner) { }

    public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
    public virtual bool ResizeWholePanel => true;
    private string PanelConfigKey => $"{PanelType}{PanelId}".Replace("'", "").Replace("\"", "");
    private bool ApplyingSaveData { get; set; } = true;

    protected override void ConstructPanelContent()
    {
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
        if (ResizeWholePanel)
        {
            Dragger.DraggableArea = Rect;
            // Update resizer elements
            Dragger.OnEndResize();
        }
    }

    /// <summary>
    /// Intended to be called when leaving a server to ensure joining the next can build up the UI correctly again
    /// </summary>
    internal abstract void Reset();

    protected override void OnClosePanelClicked()
    {
        // Default behavior: Hide the panel
        // ContentPanel doesn't have a close/X button usually, but others do.
        this.SetActive(false);
    }

    public override void OnFinishDrag()
    {
        base.OnFinishDrag();
        SaveInternalData();
    }

    public override void OnFinishResize()
    {
        base.OnFinishResize();
        SaveInternalData();
    }

    public void SaveInternalData()
    {
        if (ApplyingSaveData) return;

        SetSaveDataToConfigValue();
    }

    private void SetSaveDataToConfigValue()
    {
        Plugin.Instance.Config.Bind("Panels", PanelConfigKey, "", "Serialized panel data").Value = ToSaveData();
    }

    private string ToSaveData()
    {
        try
        {
            return string.Join("|", new string[]
            {
                Rect.RectAnchorsToString(),
                Rect.RectPositionToString(),
                IsPinned.ToString()
            });
        }
        catch (Exception ex)
        {
            LogUtils.LogWarning($"Exception generating Panel save data: {ex}");
            return "";
        }
    }

    private void ApplySaveData()
    {
        var data = Plugin.Instance.Config.Bind("Panels", PanelConfigKey, "", "Serialized panel data").Value;
        // Load from the old key if the new key is empty. This ensures a good transition to the new format, while not losing existing config.
        // This is deprecated and should be removed in a later version.
        if (string.IsNullOrEmpty(data))
        {
            data = Plugin.Instance.Config.Bind("Panels", $"{PanelType}", "", "Serialized panel data").Value;
        }
        ApplySaveData(data);
    }

    private void ApplySaveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;
        string[] split = data.Split('|');

        try
        {
            Rect.SetAnchorsFromString(split[0]);
            Rect.SetPositionFromString(split[1]);
            if (split.Length > 2 && bool.TryParse(split[2], out var pinned))
                IsPinned = pinned;
            EnsureValidSize();
            EnsureValidPosition();
        }
        catch
        {
            LogUtils.LogWarning("Invalid or corrupt panel save data! Restoring to default.");
            SetDefaultSizeAndPosition();
            SetSaveDataToConfigValue();
        }
    }

    protected override void LateConstructUI()
    {
        ApplyingSaveData = true;

        base.LateConstructUI();

        // apply panel save data or revert to default
        try
        {
            ApplySaveData();
        }
        catch (Exception ex)
        {
            LogUtils.LogError($"Exception loading panel save data: {ex}");
            SetDefaultSizeAndPosition();
        }

        ApplyingSaveData = false;

        if (PinPanelToggleControl != null)
            PinPanelToggleControl.isOn = IsPinned;

        Dragger.OnEndResize();
    }
}

