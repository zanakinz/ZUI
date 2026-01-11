using UnityEngine;
using Object = UnityEngine.Object;

namespace ModernUI.Common;

/// <summary>
/// An abstract UI object which does not exist as an actual UI Component, but which may be a reference to one.
/// </summary>
public abstract class UIModelEx
{
    public abstract GameObject UIRoot { get; }

    /// <summary>
    /// Gets or sets if component is enabled
    /// </summary>
    public bool IsActive
    {
        get => UIRoot && UIRoot.activeInHierarchy;
        set
        {
            if (!UIRoot || IsActive == value)
                return;
            UIRoot.SetActive(value);
            OnEnabled?.Invoke(value);
        }
    }
    /// <summary>
    /// Event that fires when Enabled property is changed
    /// </summary>
    public event Action<bool> OnEnabled;

    public virtual void ToggleActive() => SetActive(!IsActive);

    public virtual void SetActive(bool active)
    {
        if (UIRoot)
            UIRoot.SetActive(active);
    }

    public virtual void Destroy()
    {
        if (UIRoot)
            Object.Destroy(UIRoot);
    }
}