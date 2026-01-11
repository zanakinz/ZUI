using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.UI.UniverseLib.UI.Models
{
    public class ToggleRef
    {
        public Toggle Toggle
        {
            get => _toggle;
            set
            {
                _toggle = value;
                if (_toggle != null)
                {
                    _toggle.onValueChanged.AddListener(OnToggleValueChanged);
                    _toggle.onValueChanged.AddListener(_ => { _toggle.OnDeselect(null); });
                }
            }
        }

        public TextMeshProUGUI Text { get; set; }
        public GameObject GameObject { get; set; }
        public Action<bool> OnValueChanged;
        private Toggle _toggle;

        public void SetToggleValueWithoutEvent(bool value)
        {
            // Temporarily remove listener from Unity's toggle event
            Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);

            // Change the value
            Toggle.isOn = value;

            // Re-add the listener
            Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool value)
        {
            // Call your custom action
            OnValueChanged?.Invoke(value);
        }

        public void SetEnabled(bool value)
        {
            Toggle.interactable = value;
        }

        public void DisableWithTimer(int interval)
        {
            Toggle.interactable = false;

            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += (sender, e) =>
            {
                Toggle.interactable = true;
                timer.Stop();
                timer.Dispose();
            };
            timer.AutoReset = false;
            timer.Enabled = true;
            timer.Start();
        }
    }
}
