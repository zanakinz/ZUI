using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Panels;
using TMPro;
using UnityEngine;

namespace ZUI.UI.ModContent
{
    internal class UserPanel : ResizeablePanelBase
    {
        public override string PanelId => "UserPanel";
        public override int MinWidth => 350;
        public override int MinHeight => 450;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.User;
        public override float Opacity => Settings.UITransparency;

        private GameObject _contentLayout;

        public UserPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            SetTitle("User Commands");
            
            _contentLayout = UIFactory.CreateVerticalGroup(ContentRoot, "ContentLayout", true, true, true, true, 6,
                new Vector4(15, 15, 15, 15), new Color(0.1f, 0.1f, 0.1f, 0.95f));
            UIFactory.SetLayoutElement(_contentLayout, flexibleWidth: 9999, flexibleHeight: 9999);

            // Title
            var titleLabel = UIFactory.CreateLabel(_contentLayout, "Title", "Player Utilities", TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(titleLabel.GameObject, minHeight: 30, flexibleWidth: 9999);
            titleLabel.TextMesh.fontStyle = FontStyles.Bold;
            titleLabel.TextMesh.fontSize = 15;

            // Info & Stats Section (Mixed dependencies)
            CreateSectionWithDependencies("Information", new[]
            {
                ("User Stats", MessageService.BCCOM_USER_STATS, "View your player statistics", DependencyService.HasBloodCraft),
                ("Server Time", ".time", "Display current server time", DependencyService.HasKindredCommands),
                ("Check Ping", ".ping", "Check your connection latency", DependencyService.HasKindredCommands),
                ("View Staff", ".staff", "List online staff members", DependencyService.HasKindredCommands)
            });

            // Actions Section (Mixed dependencies)
            CreateSectionWithDependencies("Actions", new[]
            {
                ("Toggle AFK", ".afk", "Enter/exit AFK mode (locks movement)", DependencyService.HasKindredCommands),
                ("Reset Music", MessageService.BCCOM_RESET_MUSIC, "Fix stuck combat music", DependencyService.HasBloodCraft),
                ("Quest Prepare Skip", MessageService.BCCOM_QUEST_PREPARE, "Complete GettingReadyForTheHunt", DependencyService.HasBloodCraft),
                ("Starter Kit", MessageService.BCCOM_STARTER_KIT, "Receive starter items (one-time)", DependencyService.HasBloodCraft)
            });

            // Clan Section (KindredCommands)
            CreateSectionWithDependencies("Clan & Social", new[]
            {
                ("List Clans", ".clan list 1", "View populated clans (page 1)", DependencyService.HasKindredCommands),
                ("Open Plots", ".openplots", "View available castle plots by region", DependencyService.HasKindredCommands),
                ("Boss Status", ".boss list", "View locked/unlocked bosses", DependencyService.HasKindredCommands)
            });

            // Region Section (KindredCommands)
            CreateSectionWithDependencies("World Information", new[]
            {
                ("Region List", ".region list", "View region access status", DependencyService.HasKindredCommands),
                ("Shard Status", ".gear soulshardstatus", "View soul shard information", DependencyService.HasKindredCommands)
            });
        }

        private void CreateSectionWithDependencies(string sectionTitle, (string buttonText, string command, string tooltip, bool enabled)[] buttons)
        {
            // Section title
            var titleLabel = UIFactory.CreateLabel(_contentLayout, $"{sectionTitle}Label", sectionTitle, TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(titleLabel.GameObject, minHeight: 25, flexibleWidth: 9999);
            titleLabel.TextMesh.fontStyle = FontStyles.Bold;
            titleLabel.TextMesh.fontSize = 13;
            titleLabel.TextMesh.color = new Color(0.4f, 0.8f, 1f, 1f);

            // Buttons container
            var container = UIFactory.CreateVerticalGroup(_contentLayout, $"{sectionTitle}Container", false, false, true, true, 3);
            UIFactory.SetLayoutElement(container, flexibleWidth: 9999);

            foreach (var (buttonText, command, tooltip, enabled) in buttons)
            {
                var btn = UIFactory.CreateButton(container, $"{buttonText}Btn", buttonText);
                UIFactory.SetLayoutElement(btn.GameObject, minHeight: 32, flexibleWidth: 9999);
                
                if (!enabled)
                {
                    // Disable and gray out button
                    btn.Component.interactable = false;
                    var textComp = btn.Component.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                        textComp.text = $"{buttonText} (Unavailable)";
                    }
                }
                else
                {
                    btn.OnClick = () =>
                    {
                        MessageService.EnqueueMessage(command);
                        Plugin.LogInstance.LogInfo($"User command: {command}");
                    };
                }
            }

            // Spacing
            var spacer = UIFactory.CreateUIObject("Spacer", _contentLayout);
            UIFactory.SetLayoutElement(spacer, minHeight: 5);
        }

        private void CreateSection(string sectionTitle, (string buttonText, string command, string tooltip)[] buttons)
        {
            // Section title
            var titleLabel = UIFactory.CreateLabel(_contentLayout, $"{sectionTitle}Label", sectionTitle, TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(titleLabel.GameObject, minHeight: 25, flexibleWidth: 9999);
            titleLabel.TextMesh.fontStyle = FontStyles.Bold;
            titleLabel.TextMesh.fontSize = 13;
            titleLabel.TextMesh.color = new Color(0.4f, 0.8f, 1f, 1f);

            // Buttons container
            var container = UIFactory.CreateVerticalGroup(_contentLayout, $"{sectionTitle}Container", false, false, true, true, 3);
            UIFactory.SetLayoutElement(container, flexibleWidth: 9999);

            foreach (var (buttonText, command, tooltip) in buttons)
            {
                var btn = UIFactory.CreateButton(container, $"{buttonText}Btn", buttonText);
                UIFactory.SetLayoutElement(btn.GameObject, minHeight: 32, flexibleWidth: 9999);
                btn.OnClick = () =>
                {
                    MessageService.EnqueueMessage(command);
                    Plugin.LogInstance.LogInfo($"User command: {command}");
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
