using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.UI.ModContent
{
    internal class QuestsPanel : ResizeablePanelBase
    {
        public override string PanelId => "QuestsPanel";
        public override int MinWidth => 350;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.Quests;
        public override float Opacity => Settings.UITransparency;

        private GameObject _contentLayout;

        public QuestsPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            SetTitle("Quest Management");
            
            _contentLayout = UIFactory.CreateVerticalGroup(ContentRoot, "ContentLayout", true, true, true, true, 8,
                new Vector4(15, 15, 15, 15), new Color(0.1f, 0.1f, 0.1f, 0.95f));
            UIFactory.SetLayoutElement(_contentLayout, flexibleWidth: 9999, flexibleHeight: 9999);

            // Check for BloodCraft dependency
            if (!DependencyService.HasBloodCraft)
            {
                CreateMissingDependencyMessage("BloodCraft");
                return;
            }

            // Title section
            var titleLabel = UIFactory.CreateLabel(_contentLayout, "Title", "Quest Commands", TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(titleLabel.GameObject, minHeight: 30, flexibleWidth: 9999);
            titleLabel.TextMesh.fontStyle = FontStyles.Bold;
            titleLabel.TextMesh.fontSize = 16;

            // Tracking section
            CreateSection("Track Quests", new[]
            {
                ("Track Daily Quest", MessageService.BCCOM_QUEST_TRACK_DAILY, "Locate and track your daily quest target"),
                ("Track Weekly Quest", MessageService.BCCOM_QUEST_TRACK_WEEKLY, "Locate and track your weekly quest target")
            });

            // Progress section
            CreateSection("View Progress", new[]
            {
                ("Daily Progress", MessageService.BCCOM_QUEST_PROGRESS_DAILY, "Display current daily quest progress"),
                ("Weekly Progress", MessageService.BCCOM_QUEST_PROGRESS_WEEKLY, "Display current weekly quest progress")
            });

            // Reroll section
            CreateSection("Reroll Quests", new[]
            {
                ("Reroll Daily", MessageService.BCCOM_QUEST_REROLL_DAILY, "Reroll daily quest (costs items)"),
                ("Reroll Weekly", MessageService.BCCOM_QUEST_REROLL_WEEKLY, "Reroll weekly quest (costs items)")
            });

            // Quest log toggle
            var divider = UIFactory.CreateUIObject("Divider", _contentLayout);
            UIFactory.SetLayoutElement(divider, minHeight: 2, flexibleWidth: 9999);
            divider.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var toggleBtn = UIFactory.CreateButton(_contentLayout, "ToggleLog", "Toggle Quest Logging");
            UIFactory.SetLayoutElement(toggleBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            toggleBtn.OnClick = () =>
            {
                MessageService.EnqueueMessage(".quest log");
                Plugin.LogInstance.LogInfo("Toggling quest logging");
            };
        }

        private void CreateMissingDependencyMessage(string dependencyName)
        {
            var warningLabel = UIFactory.CreateLabel(_contentLayout, "MissingDependency", 
                $"<color=#FF6B6B>? {dependencyName} Not Detected</color>\n\n" +
                $"This feature requires the {dependencyName} mod to be installed on the server.\n\n" +
                "Please contact your server administrator or install the required mod.",
                TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(warningLabel.GameObject, minHeight: 200, flexibleWidth: 9999);
            warningLabel.TextMesh.fontSize = 14;
            warningLabel.TextMesh.fontStyle = FontStyles.Bold;
        }

        private void CreateSection(string sectionTitle, (string buttonText, string command, string tooltip)[] buttons)
        {
            // Section title
            var titleLabel = UIFactory.CreateLabel(_contentLayout, $"{sectionTitle}Label", sectionTitle, TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(titleLabel.GameObject, minHeight: 25, flexibleWidth: 9999);
            titleLabel.TextMesh.fontStyle = FontStyles.Bold;
            titleLabel.TextMesh.fontSize = 13;
            titleLabel.TextMesh.color = new Color(0.8f, 0.9f, 1f, 1f);

            // Buttons container
            var container = UIFactory.CreateVerticalGroup(_contentLayout, $"{sectionTitle}Container", false, false, true, true, 3);
            UIFactory.SetLayoutElement(container, flexibleWidth: 9999);

            foreach (var (buttonText, command, tooltip) in buttons)
            {
                var btn = UIFactory.CreateButton(container, $"{buttonText}Btn", buttonText);
                UIFactory.SetLayoutElement(btn.GameObject, minHeight: 30, flexibleWidth: 9999);
                btn.OnClick = () =>
                {
                    MessageService.EnqueueMessage(command);
                    Plugin.LogInstance.LogInfo($"Quest command: {command}");
                };
            }

            // Spacing
            var spacer = UIFactory.CreateUIObject("Spacer", _contentLayout);
            UIFactory.SetLayoutElement(spacer, minHeight: 5);
        }

        protected override void OnClosePanelClicked()
        {
            SetActive(false);
        }

        internal override void Reset()
        {
        }
    }
}
