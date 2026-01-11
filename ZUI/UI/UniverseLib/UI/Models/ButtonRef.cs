using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.UI.UniverseLib.UI.Models;

/// <summary>
/// A simple helper class to handle a button's OnClick more effectively, along with some helpers.
/// </summary>
public class ButtonRef
{
    /// <summary>
    /// Invoked when the Button is clicked.
    /// </summary>
    public Action OnClick;

    /// <summary>
    /// The actual Button component this object is a reference to.
    /// </summary>
    public Button Component { get; }

    /// <summary>
    /// The Text component on the button.
    /// </summary>
    public TextMeshProUGUI ButtonText { get; }

    /// <summary>
    /// The GameObject this Button is attached to.
    /// </summary>
    public GameObject GameObject => Component.gameObject;

    /// <summary>
    /// The RectTransform for this Button.
    /// </summary>
    public RectTransform Transform { get; }


    public ButtonRef(Button button)
    {
        Component = button;
        ButtonText = button.GetComponentInChildren<TextMeshProUGUI>();
        Transform = button.GetComponent<RectTransform>();

        button.onClick.AddListener(new Action(() => { OnClick?.Invoke(); }));
    }

    /// <summary>
    /// Enable or disable the button's interaction.
    /// </summary>
    /// <param name="value"></param>
    public void SetEnabled(bool value)
    {
        Component.interactable = value;
    }

    public void DisableWithTimer(int interval)
    {
        Component.interactable = false;

        var timer = new System.Timers.Timer(interval);
        timer.Elapsed += (sender, e) =>
        {
            Component.interactable = true;
            timer.Stop();
            timer.Dispose();
        };
        timer.AutoReset = false;
        timer.Enabled = true;
        timer.Start();
    }
}
