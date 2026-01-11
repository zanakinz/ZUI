using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Controls;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using ProjectM;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static Unity.Entities.Conversion.SceneHierarchy;
using Object = UnityEngine.Object;

namespace ZUI.UI.ModContent
{
    internal class FamStatsPanel : ResizeablePanelBase
    {
        public override string PanelId => "FamStatsPanel";
        public override int MinWidth => 220;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPosition => new Vector2(Owner.Scaler.m_ReferenceResolution.x - 240,
            Owner.Scaler.m_ReferenceResolution.y * 0.5f);

        public override bool CanDrag { get; protected set; } = true;
        private readonly Color _pbColor;
        public override float Opacity => Settings.UITransparency;

        // Allow vertical resizing only
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.Horizontal;

        public override PanelType PanelType => PanelType.FamStats;
        private GameObject _uiAnchor;
        private Timer _queryTimer;
        private FamStats _data = new();

        // Controls for an update
        private LabelRef _nameLabel;
        private LabelRef _schoolLabel;
        private LabelRef _levelLabel;
        private GameObject _statsContainer;
        private GameObject _headerContainer;
        private GameObject _progressBarContainer;
        private ProgressBar _progressBar;

        private readonly Dictionary<string, GameObject> _statRowPool = new();

        public FamStatsPanel(UIBase owner) : base(owner)
        {
            _pbColor = new Color(1f, 50f, 32f).GetTransparent(Opacity);
        }

        public void RecalculateHeight()
        {
            if (_uiAnchor == null || Rect == null) return;

            // Get VerticalLayoutGroup to account for its spacing and padding
            var vlg = _uiAnchor.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;

            // Count active children and calculate height
            float contentHeight = 0;
            //int activeChildCount = 0;

            var t = _headerContainer.GetComponent<RectTransform>();
            if (t != null)
                contentHeight += LayoutUtility.GetPreferredHeight(t);

            foreach (var gameObject in _statRowPool.Values)
            {
                if (gameObject.activeSelf)
                {
                    var height = LayoutUtility.GetPreferredHeight(gameObject.GetComponent<RectTransform>());
                    contentHeight += height;
                }
            }

            t = _progressBarContainer.GetComponent<RectTransform>();
            if (t != null)
            {
                contentHeight += LayoutUtility.GetPreferredHeight(t);
                t.anchoredPosition = new Vector2(t.anchoredPosition.x, -contentHeight);
            }

            // Add spacing between children
            contentHeight += (3 - 1) * vlg.spacing;
            // Add padding
            contentHeight += vlg.padding.top + vlg.padding.bottom + 30f;

            // Set panel height
            Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, contentHeight);
        }

        public void UpdateData(FamStats data)
        {
            if (data == null) return;
            data.CurrentHealth = FamiliarStateService.FamStats.CurrentHealth;
            data.Stats = FamiliarStateService.FamStats.Stats;

            var doFlash = _data != null && _data.ExperiencePercent != data.ExperiencePercent;
            _data = data;

            // Ensure we have a name to display
            string nameToShow = !string.IsNullOrEmpty(data.Name) ? data.Name : "Unknown Familiar";
            if (_nameLabel != null)
                _nameLabel.TextMesh.text = nameToShow;

            // Update school if available
            if (_schoolLabel != null)
            {
                if (!string.IsNullOrEmpty(data.School))
                {
                    _schoolLabel.TextMesh.text = data.School;
                    var color = Color.white;
                    if (Enum.TryParse<AbilitySchoolType>(data.School, out var type))
                    {
                        var colorData = GameHelper.GetColorNameFromSchool(type);
                        color = colorData.Color;
                    }
                    _schoolLabel.TextMesh.color = color;
                    _schoolLabel.GameObject.SetActive(true);
                }
                else
                    _schoolLabel.GameObject.SetActive(false);

                //UIFactory.SetLayoutElement(_headerContainer, preferredHeight: _schoolLabel.IsActive() ? 75 : 50);
            }

            // Update level info
            if (_levelLabel != null)
                _levelLabel.TextMesh.text =
                    $"Level: {data.Level}{(data.PrestigeLevel == 0 ? null : $"   Prestige: {data.PrestigeLevel}")}";

            // Track which rows we've used in this update
            var usedKeys = new HashSet<string>();

            // First, update the fixed stats
            UpdateStatRow("Health", $"{data.CurrentHealth}/{data.MaxHealth}", usedKeys);
            UpdateStatRow("Physical Power", data.PhysicalPower, usedKeys);
            UpdateStatRow("Spell Power", data.SpellPower, usedKeys);

            // Then update dynamic stats
            if (data.Stats != null)
            {
                foreach (var kvp in data.Stats)
                {
                    UpdateStatRow(kvp.Key, kvp.Value, usedKeys);
                }
            }

            // Hide any rows that aren't being used in this update
            foreach (var key in _statRowPool.Keys.ToList())
            {
                if (!usedKeys.Contains(key) && _statRowPool.TryGetValue(key, out GameObject row))
                {
                    row.SetActive(false);
                }
            }

            // Update progress bar
            if (_progressBar != null)
            {
                _progressBar.SetProgress(
                    data.ExperiencePercent / 100f,
                    "",
                    $"XP: {data.ExperienceValue} ({data.ExperiencePercent}%)",
                    ActiveState.Active,
                    _pbColor,
                    data.ExperienceValue.ToString(),
                    doFlash
                );
            }

            // Schedule layout rebuild
            CoroutineUtility.StartCoroutine(DelayedLayoutRebuild());
        }

        private IEnumerator DelayedLayoutRebuild()
        {
            // Wait for the end of the frame
            yield return new WaitForEndOfFrame();

            // Then force layout rebuild
            if (_uiAnchor != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_uiAnchor.GetComponent<RectTransform>());
                RecalculateHeight();
            }
        }

        private void UpdateStatRow(string label, string value, HashSet<string> usedKeys)
        {
            if (string.IsNullOrEmpty(label)) return;
            usedKeys.Add(label);

            GameObject row;
            TextMeshProUGUI valueText;

            // Get or create the row
            if (_statRowPool.TryGetValue(label, out row))
            {
                // Row exists, just activate it
                row.SetActive(true);
                valueText = row.transform.Find($"{label}Value")?.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                // Create new row
                CreateStatRow(_statsContainer, label, out row, out var labelRef);
                valueText = labelRef.TextMesh;
                _statRowPool[label] = row;
            }

            // Update the value
            if (valueText != null)
            {
                valueText.text = value ?? "0";
            }
        }

        protected override void ConstructPanelContent()
        {
            /*var source = UnityHelper.FindInHierarchy("BloodOrbParent|BloodOrb");
            var blood = Object.Instantiate(source, Plugin.UIManager.UIRoot.transform);
            blood.GetComponent<RectTransform>().localScale = new Vector3(2f, 2f, 2f);
            var c1 = blood.transform.FindChild("BlackBackground");
            var c2 = c1.transform.FindChild("Blood");
            var trash = blood.GetComponent<BloodOrbComponent>();
            if(trash != null)
            {
                Object.Destroy(trash);
            }

            var t = c2.gameObject.GetComponent<ValidUiRaycastTarget>();
            Object.Destroy(t);
            var t2 = c2.gameObject.GetComponent<EventTrigger>();
            Object.Destroy(t2);

            var img = c2.gameObject.GetComponent<Image>();

            var material = new Material(img.material.shader);
            material.CopyPropertiesFromMaterial(img.material);
            img.material = material;
            img.SetMaterialDirty();
            img.material.SetFloat("_LiquidLevel", 1f);*/

            // Hide the title bar and set up the panel
            TitleBar.SetActive(false);
            Dragger.DraggableArea = Rect;
            Dragger.OnEndResize();

            // Modify ContentRoot to ensure it has no extra padding
            RectTransform contentRootRect = ContentRoot.GetComponent<RectTransform>();
            contentRootRect.anchorMin = Vector2.zero;
            contentRootRect.anchorMax = Vector2.one;
            contentRootRect.offsetMin = Vector2.zero;
            contentRootRect.offsetMax = Vector2.zero;

            // Remove any layout group on ContentRoot that might add spacing
            VerticalLayoutGroup existingVLG = ContentRoot.GetComponent<VerticalLayoutGroup>();
            if (existingVLG != null)
            {
                Object.Destroy(existingVLG);
            }

            // Set ContentRoot layout element to fill available space
            UIFactory.SetLayoutElement(ContentRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            var color = Theme.PanelBackground;

            // Create main container with explicit settings to eliminate bottom space
            _uiAnchor = UIFactory.CreateUIObject("UIAnchor", ContentRoot);
            _uiAnchor.AddComponent<Image>().color = color;

            // Set _uiAnchor to fill ContentRoot exactly
            RectTransform anchorRect = _uiAnchor.GetComponent<RectTransform>();
            anchorRect.anchorMin = Vector2.zero;
            anchorRect.anchorMax = Vector2.one;
            anchorRect.offsetMin = Vector2.zero;
            anchorRect.offsetMax = Vector2.zero;

            // Add a vertical layout group with explicit settings
            VerticalLayoutGroup vlg = _uiAnchor.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter; // Align to top
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 2;
            vlg.padding.left = 8;
            vlg.padding.right = 8;
            vlg.padding.top = 4;
            vlg.padding.bottom = 4;

            // Create header section
            CreateHeaderSection();

            // Create stats container
            CreateStatsSection();

            // Create XP progress bar at the bottom
            CreateProgressBarSection();

            // Set default position
            SetDefaultSizeAndPosition();
        }

        private void CreateHeaderSection()
        {
            // Create container with reduced height and spacing
            _headerContainer = UIFactory.CreateVerticalGroup(_uiAnchor, "HeaderContainer", false, false, true, true, 2,
                default, new Color(0.15f, 0.15f, 0.15f).GetTransparent(Opacity));
            UIFactory.SetLayoutElement(_headerContainer, minHeight: 60, preferredHeight: 50, flexibleHeight: 0, flexibleWidth: 9999);

            // Familiar name with larger font
            _nameLabel = UIFactory.CreateLabel(_headerContainer, "FamNameText", FamiliarStateService.FamStats.Name ?? "Unknown",
                TextAlignmentOptions.Center, Theme.DefaultText, 18, outlineWidth: 0.05f);
            UIFactory.SetLayoutElement(_nameLabel.GameObject, minHeight: 25, preferredHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            _nameLabel.TextMesh.fontStyle = FontStyles.Bold;

            var horGroup = UIFactory.CreateHorizontalGroup(_headerContainer, "txtContainer", false, false, true, true,2,
                childAlignment: TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(horGroup, minHeight: 25, preferredHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Familiar school with larger font
            _schoolLabel = UIFactory.CreateLabel(horGroup, "FamSchoolText", "Unknown",
                TextAlignmentOptions.Center, Theme.DefaultText, 16, outlineWidth: 0f);
            UIFactory.SetLayoutElement(_schoolLabel.GameObject, minWidth: 60, flexibleWidth: 0, minHeight: 28, flexibleHeight: 0);
            _schoolLabel.TextMesh.fontStyle = FontStyles.Bold;

            // Level info - reduced height
            _levelLabel = UIFactory.CreateLabel(horGroup, "FamLevelText", "Level: Unknown   Prestige: Unknown",
                TextAlignmentOptions.Center, Theme.DefaultText, 16, outlineWidth: 0f);
            UIFactory.SetLayoutElement(_levelLabel.GameObject, minWidth: 90, flexibleWidth: 0, minHeight: 28, flexibleHeight: 0);

            if (CanDrag)
            {
                // Create pin button as a child of ContentRoot (panel root) instead of _uiAnchor
                var pinButton = UIFactory.CreateToggle(ContentRoot, "PinButton");

                // Set layout element to position it correctly
                UIFactory.SetLayoutElement(pinButton.GameObject, minHeight: 15, preferredHeight: 15, flexibleHeight: 0,
                    minWidth: 15, preferredWidth: 15, flexibleWidth: 0, ignoreLayout: true);

                // Set RectTransform to position it at the top left
                RectTransform pinRect = pinButton.GameObject.GetComponent<RectTransform>();
                pinRect.anchorMin = new Vector2(0, 1);
                pinRect.anchorMax = new Vector2(0, 1);
                pinRect.pivot = new Vector2(0, 1);
                pinRect.anchoredPosition = new Vector2(5, -5); // Offset from top left corner

                // Set toggle properties
                pinButton.Toggle.isOn = false;
                pinButton.OnValueChanged += value => IsPinned = value;
                PinPanelToggleControl = pinButton.Toggle;

                // Make the label text empty or minimal
                pinButton.Text.text = "";
            }
        }

        private void CreateStatsSection()
        {
            // Stats container
            _statsContainer = UIFactory.CreateVerticalGroup(_uiAnchor, "StatsContainer", true, false, true, true, 2,
                new Vector4(4, 2, 4, 2), new Color(0.12f, 0.12f, 0.12f).GetTransparent(Opacity));
            UIFactory.SetLayoutElement(_statsContainer, minHeight: 20, preferredHeight: 0, flexibleHeight: 9999, flexibleWidth: 9999);
        }

        private void CreateStatRow(GameObject parent, string label, out GameObject rowObj, out LabelRef valueText)
        {
            // Create a horizontal row with reduced height
            rowObj = UIFactory.CreateHorizontalGroup(parent, $"{label}Row", false, false, true, true, 5,
                default, new Color(0.18f, 0.18f, 0.18f).GetTransparent(Opacity));
            UIFactory.SetLayoutElement(rowObj, minHeight: 28, preferredHeight: 28, flexibleHeight: 0, flexibleWidth: 9999);

            // Stat label - reduced height
            var statLabel = UIFactory.CreateLabel(rowObj, $"{label}Label", label, TextAlignmentOptions.Left, Theme.DefaultText, 15, outlineWidth: 0f);
            UIFactory.SetLayoutElement(statLabel.GameObject, minWidth: 90, flexibleWidth: 0, minHeight: 28, flexibleHeight: 0);

            // Value display - reduced height
            valueText = UIFactory.CreateLabel(rowObj, $"{label}Value", "0", TextAlignmentOptions.Right, Theme.DefaultText, 15, outlineWidth: 0f);
            UIFactory.SetLayoutElement(valueText.GameObject, minWidth: 90, flexibleWidth: 9999, minHeight: 28, flexibleHeight: 0);
            valueText.TextMesh.fontStyle = FontStyles.Bold;
        }

        private void CreateProgressBarSection()
        {
            // Create bare container without layout elements
            _progressBarContainer = UIFactory.CreateUIObject("ProgressBarContent", _uiAnchor);

            // Set fixed height with no flexibility
            UIFactory.SetLayoutElement(_progressBarContainer, minHeight: 25, preferredHeight: 25,
                flexibleHeight: 0, flexibleWidth: 9999);

            // Create the progress bar
            _progressBar = new ProgressBar(_uiAnchor, Theme.DefaultBar, Color.black.GetTransparent(Opacity));

            // Set initial progress
            _progressBar.SetProgress(0f, "", "XP: 0 (0%)", ActiveState.Active, Theme.DefaultBar, "", false);
        }

        internal override void Reset()
        {
            // Clean up timer if needed
            if (_queryTimer != null)
            {
                _queryTimer.Stop();
                _queryTimer.Dispose();
                _queryTimer = null;
            }

            // Reset progress bar if needed
            _progressBar?.Reset();

            // Clear the stat row pool on panel reset (when leaving the server, etc.)
            if (_statRowPool != null)
            {
                foreach (var kvp in _statRowPool)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.SetActive(false);
                    }
                }
            }
        }

        public override void OnFinishResize()
        {
            base.OnFinishResize();

            // After manual resize, make sure content still fits
            LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRoot.GetComponent<RectTransform>());
        }

        protected override void LateConstructUI()
        {
            base.LateConstructUI();

            if (Plugin.IS_TESTING)
            {
                UpdateData(new FamStats
                {
                    Name = "TestFamiliar Test Familiar",
                    Level = 99,
                    PrestigeLevel = 5,
                    ExperienceValue = 6500,
                    ExperiencePercent = 65,
                    MaxHealth = "5000",
                    PhysicalPower = "450",
                    SpellPower = "575",
                    School = "Unholy",
                    Stats = new Dictionary<string, string>()
                    {
                        {"Stat1", "Value1"},
                        {"Stat2", "Value2"},
                        {"Stat3", "Value3"},
                        {"Stat4", "Value4"},
                        {"Stat5", "Value5"},
                        {"Stat6", "Value6"},
                        {"Stat7", "Value7"},
                    }
                });
            }

            RecalculateHeight();

            // Start querying for updates
            SendUpdateStatsCommand();
            StartUpdateTimer();
        }

        private void StartUpdateTimer()
        {
            if (_queryTimer == null)
            {
                _queryTimer = new Timer(Settings.FamStatsQueryIntervalInSeconds * 1000);
                _queryTimer.AutoReset = true;
                _queryTimer.Elapsed += (_, _) =>
                {
                    SendUpdateStatsCommand();
                    if (Plugin.IS_TESTING)
                    {
                        _data.ExperiencePercent += 10;
                        if (_data.ExperiencePercent > 100)
                            _data.ExperiencePercent = 0;
                        UpdateData(_data);
                    }
                };
            }

            if (_queryTimer.Enabled) return;

            _queryTimer.Start();
        }

        private void StopUpdateTimer()
        {
            _queryTimer?.Stop();
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            if (active)
            {
                // Start the update timer when the panel is activated
                StartUpdateTimer();
            }
            else
            {
                // Stop the update timer when the panel is deactivated
                StopUpdateTimer();
            }
        }

        private void SendUpdateStatsCommand()
        {
            MessageService.EnqueueMessage(MessageService.BCCOM_FAMSTATS);
        }
    }
}
