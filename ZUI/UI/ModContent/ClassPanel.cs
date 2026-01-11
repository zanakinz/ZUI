using System.Collections.Generic;
using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using TMPro;
using UnityEngine;

namespace ZUI.UI.ModContent
{
    internal class ClassPanel : ResizeablePanelBase
    {
        public override string PanelId => "ClassPanel";
        public override int MinWidth => 480;  // Increased from 400 to fit content properly
        public override int MinHeight => 450;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.Class;
        public override float Opacity => Settings.UITransparency;

        private GameObject _contentLayout;
        private LabelRef _infoLabel;
        private GameObject _buttonContainer;
        
        private readonly Dictionary<string, string> _classPrices = new()
        {
            { "BloodKnight", "750 Greater Blood Essence" },
            { "DemonHunter", "750 Greater Blood Essence" },
            { "VampireLord", "750 Greater Blood Essence" },
            { "ShadowBlade", "750 Greater Blood Essence" },
            { "ArcaneSorcerer", "750 Greater Blood Essence" },
            { "DeathMage", "750 Greater Blood Essence" }
        };

        public ClassPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            SetTitle("Class Selection");
            
            _contentLayout = UIFactory.CreateVerticalGroup(ContentRoot, "ContentLayout", true, true, true, true, 5,
                new Vector4(10, 10, 10, 10), new Color(0.1f, 0.1f, 0.1f, 0.95f));
            UIFactory.SetLayoutElement(_contentLayout, flexibleWidth: 9999, flexibleHeight: 9999);

            // Check for BloodCraft dependency
            if (!DependencyService.HasBloodCraft)
            {
                CreateMissingDependencyMessage("BloodCraft");
                return;
            }

            // Info section
            var infoContainer = UIFactory.CreateVerticalGroup(_contentLayout, "InfoContainer", false, false, true, true, 3);
            UIFactory.SetLayoutElement(infoContainer, minHeight: 150, flexibleWidth: 9999);
            
            var titleLabel = UIFactory.CreateLabel(infoContainer, "Title", "Available Classes", TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(titleLabel.GameObject, minHeight: 30, flexibleWidth: 9999);
            titleLabel.TextMesh.fontStyle = FontStyles.Bold;
            titleLabel.TextMesh.fontSize = 16;
            
            _infoLabel = UIFactory.CreateLabel(infoContainer, "Info", GetClassListText(), TextAlignmentOptions.TopLeft);
            UIFactory.SetLayoutElement(_infoLabel.GameObject, minHeight: 100, flexibleWidth: 9999);
            _infoLabel.TextMesh.fontSize = 12;

            // Buttons section
            var buttonsTitle = UIFactory.CreateLabel(_contentLayout, "ButtonsTitle", "Select or Change Class:", TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(buttonsTitle.GameObject, minHeight: 25, flexibleWidth: 9999);
            buttonsTitle.TextMesh.fontStyle = FontStyles.Bold;
            
            _buttonContainer = UIFactory.CreateVerticalGroup(_contentLayout, "ButtonsContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(_buttonContainer, flexibleWidth: 9999, flexibleHeight: 9999);

            CreateClassButtons();
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

        private string GetClassListText()
        {
            return "• <color=#FF6B6B>Blood Knight</color> - Physical/Unholy hybrid tank\n" +
                   "• <color=#4ECDC4>Demon Hunter</color> - Agile physical damage dealer\n" +
                   "• <color=#9B59B6>Vampire Lord</color> - Spell/Physical hybrid caster\n" +
                   "• <color=#FFE66D>Shadow Blade</color> - Critical strike assassin\n" +
                   "• <color=#45B7D1>Arcane Sorcerer</color> - Pure spell power caster\n" +
                   "• <color=#2ECC71>Death Mage</color> - Necromancer with minions\n\n" +
                   "Use .class liststats [ClassName] to view synergies";
        }

        private void CreateClassButtons()
        {
            var classes = new[] { "BloodKnight", "DemonHunter", "VampireLord", "ShadowBlade", "ArcaneSorcerer", "DeathMage" };
            var displayNames = new[] { "Blood Knight", "Demon Hunter", "Vampire Lord", "Shadow Blade", "Arcane Sorcerer", "Death Mage" };
            
            for (int i = 0; i < classes.Length; i++)
            {
                var className = classes[i];
                var displayName = displayNames[i];
                var price = _classPrices.ContainsKey(className) ? _classPrices[className] : "Unknown";
                
                var buttonContainer = UIFactory.CreateHorizontalGroup(_buttonContainer, $"Class_{className}", false, false, true, true, 5);
                UIFactory.SetLayoutElement(buttonContainer, minHeight: 35, flexibleWidth: 9999);
                
                // Select button
                var selectBtn = UIFactory.CreateButton(buttonContainer, $"Select_{className}", $"Select {displayName}");
                UIFactory.SetLayoutElement(selectBtn.GameObject, minWidth: 180, minHeight: 35);
                selectBtn.OnClick = () => OnSelectClass(className);
                
                // Change button
                var changeBtn = UIFactory.CreateButton(buttonContainer, $"Change_{className}", $"Change ({price})");
                UIFactory.SetLayoutElement(changeBtn.GameObject, minWidth: 200, minHeight: 35, flexibleWidth: 9999);
                changeBtn.OnClick = () => OnChangeClass(className);
                
                // Apply green color to price
                var changeBtnText = changeBtn.ButtonText;
                changeBtnText.text = $"Change (<color=#2ECC71>{price}</color>)";
                
                // Stats info button
                var infoBtn = UIFactory.CreateButton(buttonContainer, $"Info_{className}", "?");
                UIFactory.SetLayoutElement(infoBtn.GameObject, minWidth: 35, minHeight: 35);
                infoBtn.OnClick = () => OnShowClassInfo(className);
            }
        }

        private void OnSelectClass(string className)
        {
            var command = string.Format(MessageService.BCCOM_CLASS_SELECT, className);
            MessageService.EnqueueMessage(command);
            Plugin.LogInstance.LogInfo($"Selecting class: {className}");
        }

        private void OnChangeClass(string className)
        {
            var command = string.Format(MessageService.BCCOM_CLASS_CHANGE, className);
            MessageService.EnqueueMessage(command);
            Plugin.LogInstance.LogInfo($"Changing to class: {className}");
        }

        private void OnShowClassInfo(string className)
        {
            var command = string.Format(MessageService.BCCOM_CLASS_LISTSTATS, className);
            MessageService.EnqueueMessage(command);
            Plugin.LogInstance.LogInfo($"Showing stats for class: {className}");
        }

        protected override void OnClosePanelClicked()
        {
            SetActive(false);
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();
            // Set a larger default size to fit all content properly
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 480);  // Increased from 450 to match MinWidth
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 550);
        }

        internal override void Reset()
        {
        }
    }
}
