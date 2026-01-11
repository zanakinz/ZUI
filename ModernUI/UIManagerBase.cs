using ModernUI.Common;
using UnityEngine;

namespace ModernUI;

public abstract class UIManagerBase
{
    protected UIBaseEx UiBase { get; set; }
    public GameObject UIRoot => UiBase.GetRootObject();
    public bool IsInitialized { get; protected set; }

    public virtual void SetupAndShowUI()
    {
        if (IsInitialized) return;
        Common.ModernUI.Initialize();

        if (UiBase == null)
        {
            UiBase = new UIBaseEx(PluginInfo.PLUGIN_GUID, UiUpdate);
            AddMainContentPanel();
        }
        SetActive(true);

        IsInitialized = true;
    }

    public abstract void SetActive(bool active);

    public virtual void Reset()
    {
        IsInitialized = false;
        SetActive(false);
    }

    protected abstract void AddMainContentPanel();
    /// <summary>
    /// Called once per frame when your UI is being displayed.
    /// </summary>
    protected virtual void UiUpdate()
    {
    }
}