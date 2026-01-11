using BepInEx.Configuration;

namespace ZUI.UI.CustomLib.Controls;

public abstract class SettingsButtonBase
{
    private const string GROUP = "UISettings";

    protected readonly ConfigEntry<string> Setting;
    protected string State => Setting.Value;

    protected SettingsButtonBase(string id, string defaultValue)
    {
        Setting = Plugin.Instance.Config.Bind(GROUP, $"{id}", defaultValue);
    }

    // Implementers to use this to set/toggle/perform action
    // This should return the new config that can be stored in the config file
    public abstract string PerformAction();

    // Gets the label that should be displayed on the button due to the current state
    protected abstract string Label();

    private void OnToggle()
    {
        Setting.Value = PerformAction();

        UpdateButton();
    }

    public void UpdateButton()
    {
        // Update the label on the button
        /* BCUIManager.ContentPanel.SetButton(new ActionSerialisedMessage()
         {
             Group = Group,
             ID = _id,
             Label = Label(),
             Enabled = true
         }, OnToggle);*/
    }
}
