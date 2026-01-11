using System.Collections.Generic;
using ZUI.Services;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIBase = ZUI.UI.UniverseLib.UI.UIBase;

namespace ZUI.UI.ModContent
{
    public class FamActionsPanel : ResizeablePanelBase
    {
        public override string PanelId => "FamActionsPanel";
        public override PanelType PanelType => PanelType.FamActions;

        public override int MinWidth => 250;
        public override int MinHeight => 350;
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);

        private GameObject _contentLayout;
        private TMP_Dropdown _shinyDropdown;
        private InputFieldRef _challengeInput;
        private InputFieldRef _echoesInput;

        private static readonly List<string> SpellSchools = new List<string>
        {
            "Blood", "Unholy", "Illusion", "Frost", "Chaos", "Storm"
        };

        public FamActionsPanel(UIBase owner) : base(owner) { }

        protected override void ConstructPanelContent()
        {
            _contentLayout = UIFactory.CreateVerticalGroup(ContentRoot, "ContentLayout", true, true, true, true, 4, new Vector4(10, 10, 10, 10), new Color(0.1f, 0.1f, 0.1f, 0.5f));

            // Title - Changed from "Familiar Actions" to "Fam. Actions"
            SetTitle("Fam. Actions");
            var titleRef = UIFactory.CreateLabel(_contentLayout, "Title", "Fam. Actions", TextAlignmentOptions.Center);
            titleRef.TextMesh.fontSize = 20;
            UIFactory.SetLayoutElement(titleRef.GameObject, minHeight: 30, flexibleWidth: 9999);

            AddSpacer(10);

            // --- Shiny Section ---
            UIFactory.CreateLabel(_contentLayout, "ShinyLabel", "Apply Shiny Buff:", TextAlignmentOptions.Left);
            UIFactory.CreateDropdown(_contentLayout, "ShinyDropdown", out _shinyDropdown, "Select School...", 14, null);
            _shinyDropdown.ClearOptions();
            foreach (var school in SpellSchools) _shinyDropdown.options.Add(new TMP_Dropdown.OptionData(school));
            _shinyDropdown.RefreshShownValue();
            UIFactory.SetLayoutElement(_shinyDropdown.gameObject, minHeight: 30, flexibleWidth: 9999);

            var shinyBtn = UIFactory.CreateButton(_contentLayout, "ShinyBtn", "Apply Shiny");
            UIFactory.SetLayoutElement(shinyBtn.GameObject, minHeight: 30, flexibleWidth: 9999);
            shinyBtn.OnClick = () =>
            {
                if (_shinyDropdown.value >= 0 && _shinyDropdown.value < SpellSchools.Count)
                {
                    string school = SpellSchools[_shinyDropdown.value];
                    MessageService.EnqueueMessage($".fam shiny {school}");
                    shinyBtn.DisableWithTimer(2000);
                }
            };

            AddSpacer(15);

            // --- Battle / Challenge Section ---
            UIFactory.CreateLabel(_contentLayout, "BattleLabel", "Challenge Player:", TextAlignmentOptions.Left);
            var challengeGroup = UIFactory.CreateHorizontalGroup(_contentLayout, "ChallengeGroup", false, true, true, true, 4, default, Color.clear);
            
            _challengeInput = UIFactory.CreateInputField(challengeGroup, "ChallengeInput", "Player Name...");
            UIFactory.SetLayoutElement(_challengeInput.GameObject, minHeight: 30, flexibleWidth: 9999);

            var challengeBtn = UIFactory.CreateButton(challengeGroup, "ChallengeBtn", "Fight!");
            UIFactory.SetLayoutElement(challengeBtn.GameObject, minHeight: 30, minWidth: 60);
            challengeBtn.OnClick = () =>
            {
                if (!string.IsNullOrEmpty(_challengeInput.Text))
                {
                    MessageService.EnqueueMessage($".fam challenge {_challengeInput.Text}");
                    challengeBtn.DisableWithTimer(2000);
                }
            };
            
            var arenaBtn = UIFactory.CreateButton(_contentLayout, "SetArenaBtn", "Set Current Pos as Arena");
            UIFactory.SetLayoutElement(arenaBtn.GameObject, minHeight: 30, flexibleWidth: 9999);
            arenaBtn.OnClick = () =>
            {
                MessageService.EnqueueMessage(".fam sba");
                arenaBtn.DisableWithTimer(2000);
            };

            AddSpacer(15);

            // --- Echoes / VBlood Unlock ---
            UIFactory.CreateLabel(_contentLayout, "EchoesLabel", "Unlock VBlood (Echoes):", TextAlignmentOptions.Left);
            var echoesGroup = UIFactory.CreateHorizontalGroup(_contentLayout, "EchoesGroup", false, true, true, true, 4, default, Color.clear);
            
            _echoesInput = UIFactory.CreateInputField(echoesGroup, "EchoesInput", "VBlood Name...");
            UIFactory.SetLayoutElement(_echoesInput.GameObject, minHeight: 30, flexibleWidth: 9999);

            var echoesBtn = UIFactory.CreateButton(echoesGroup, "EchoesBtn", "Unlock");
            UIFactory.SetLayoutElement(echoesBtn.GameObject, minHeight: 30, minWidth: 60);
            echoesBtn.OnClick = () =>
            {
                if (!string.IsNullOrEmpty(_echoesInput.Text))
                {
                    MessageService.EnqueueMessage($".fam echoes {_echoesInput.Text}");
                    echoesBtn.DisableWithTimer(2000);
                }
            };

            AddSpacer(15);

            // --- Misc / Emotes ---
            var emotesBtn = UIFactory.CreateButton(_contentLayout, "EmotesBtn", "Toggle Emotes");
            UIFactory.SetLayoutElement(emotesBtn.GameObject, minHeight: 30, flexibleWidth: 9999);
            emotesBtn.OnClick = () =>
            {
                MessageService.EnqueueMessage(".fam e");
                emotesBtn.DisableWithTimer(2000);
            };
        }

        private void AddSpacer(float height)
        {
            var spacer = UIFactory.CreateUIObject("Spacer", _contentLayout);
            UIFactory.SetLayoutElement(spacer, minHeight: (int)height);
        }

        internal override void Reset() { }
    }
}

