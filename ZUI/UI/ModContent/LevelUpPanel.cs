using System;
using System.Collections.Generic;
using System.Linq;
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
    public class LevelUpPanel : ResizeablePanelBase
    {
        public override string PanelId => "LevelUpPanel";
        public override PanelType PanelType => PanelType.LevelUp;
        
        // Default size
        public override int MinWidth => 300;
        public override int MinHeight => 400;
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);

        private GameObject _contentLayout;
        private TMP_Dropdown _itemDropdown;
        private GameObject _buttonsContainer;
        private TextMeshProUGUI _titleText;
        
        private string _currentMode = "Weapon"; // "Weapon" or "Blood"
        private string _selectedItem;
        private int _selectedItemIndex = 0; // Store the index

        // Data Lists
        private static readonly List<string> Weapons = new List<string>
        {
            "Sword", "Axe", "Mace", "Spear", "Reaper", "Slashers", 
            "Crossbow", "Pistols", "GreatSword", "Longbow", "Whip", "Daggers"
        };

        private static readonly List<string> Bloods = new List<string>
        {
            "Creature", "Warrior", "Rogue", "Brute", "Scholar", 
            "Worker", "Mutant", "Draculin"
        };
        
        private static readonly List<string> WeaponStats = new List<string>
        {
            "MaxHealth",
            "MovementSpeed", 
            "PrimaryAttackSpeed", 
            "PhysicalLifeLeech",
            "SpellLifeLeech",
            "PrimaryLifeLeech", 
            "PhysicalPower",
            "SpellPower", 
            "PhysicalCritChance",
            "PhysicalCritDamage", 
            "SpellCritChance",
            "SpellCritDamage"
        };

        private static readonly List<string> BloodStats = new List<string>
        {
            "HealingReceived", "DamageReduction", 
            "PhysicalResistance", "SpellResistance", 
            "ResourceYield", "ReducedBloodDrain", 
            "SpellCooldownRecoveryRate", "WeaponCooldownRecoveryRate", 
            "UltimateCooldownRecoveryRate", "MinionDamage", 
            "AbilityAttackSpeed", "CorruptionDamageReduction"
        };

        public LevelUpPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            // Main Layout
            // Ensure padding and background are set correctly
            _contentLayout = UIFactory.CreateVerticalGroup(ContentRoot, "ContentLayout", true, true, true, true, 4, new Vector4(10, 10, 10, 10), new Color(0.1f, 0.1f, 0.1f, 0.95f));

            // Title
            var titleRef = UIFactory.CreateLabel(_contentLayout, "Title", "Level Up", TextAlignmentOptions.Center);
            _titleText = titleRef.TextMesh;
            _titleText.fontSize = 20;
            UIFactory.SetLayoutElement(titleRef.GameObject, minHeight: 30, flexibleWidth: 9999);

            var checkPointsBtn = UIFactory.CreateButton(_contentLayout, "CheckPointsBtn", "Check Unused Points");
            UIFactory.SetLayoutElement(checkPointsBtn.GameObject, minHeight: 25, flexibleWidth: 9999);
            checkPointsBtn.OnClick = () =>
            {
                // For Weapon: .weapon get (no parameters)
                // For Blood: .blood get [BloodType] (needs the selected blood type)
                string cmd;
                if (_currentMode == "Weapon")
                {
                    cmd = MessageService.BCCOM_WEAPON_GET;
                }
                else
                {
                    // Use the selected blood type name
                    cmd = string.Format(MessageService.BCCOM_BLOOD_GET, _selectedItem);
                }
                MessageService.EnqueueMessage(cmd);
            };

            // Dropdown for Item Selection
            UIFactory.CreateDropdown(_contentLayout, "ItemDropdown", out _itemDropdown, "Select Item...", 14, OnDropdownValueChanged);
            UIFactory.SetLayoutElement(_itemDropdown.gameObject, minHeight: 35, flexibleWidth: 9999);

            // Container for Stat Buttons
            // We pass a specific dark color to avoid default red/error color if any
            var scrollObj = UIFactory.CreateScrollView(_contentLayout, "StatsScrollView", out var scrollContent, out var autoScroll, new Color(0.05f, 0.05f, 0.05f, 1f));
            _buttonsContainer = scrollContent;
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999, flexibleWidth: 9999);
            
            // Fix: CreateScrollView adds a VerticalLayoutGroup by default to content.
            // We need to destroy it before adding GridLayoutGroup to avoid the crash.
            var vlg = _buttonsContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
                UnityEngine.Object.DestroyImmediate(vlg);

            // Set up grid layout for buttons
            var grid = _buttonsContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(260, 35);
            grid.spacing = new Vector2(5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.childAlignment = TextAnchor.UpperCenter; // Good practice for grid

            // Initialize with default mode
            RefreshUI();
        }

        internal override void Reset()
        {
            // No reset logic needed for now
        }

        public void SetMode(bool isWeapon)
        {
            _currentMode = isWeapon ? "Weapon" : "Blood";
            if (_titleText != null)
                _titleText.text = isWeapon ? "Weapon Expertise" : "Blood Legacy";
            
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_itemDropdown == null) return;

            // Clear existing buttons
            foreach (Transform child in _buttonsContainer.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            // Setup Dropdown
            _itemDropdown.ClearOptions();
            var options = _currentMode == "Weapon" ? Weapons : Bloods;
            
            foreach (var opt in options)
            {
                _itemDropdown.options.Add(new TMP_Dropdown.OptionData(opt));
            }
            
            _itemDropdown.value = 0;
            _itemDropdown.RefreshShownValue();
            _selectedItemIndex = 0; // Initialize the index
            _selectedItem = options.Count > 0 ? options[0] : "";

            // Create Buttons
            var stats = _currentMode == "Weapon" ? WeaponStats : BloodStats;
            foreach (var stat in stats)
            {
                var btn = UIFactory.CreateButton(_buttonsContainer, $"Btn_{stat}", stat);
                UIFactory.SetLayoutElement(btn.Component.gameObject, minWidth: 260, minHeight: 35);
                
                string currentStat = stat; // Capture for lambda
                btn.OnClick = () => SendCommand(currentStat);
            }
        }

        private void OnDropdownValueChanged(int index)
        {
            var options = _currentMode == "Weapon" ? Weapons : Bloods;
            if (index >= 0 && index < options.Count)
            {
                _selectedItem = options[index];
                _selectedItemIndex = index;
            }
        }

        private void SendCommand(string stat)
        {
            if (string.IsNullOrEmpty(_selectedItem)) return;

            // Get the stat index from the stat lists
            var stats = _currentMode == "Weapon" ? WeaponStats : BloodStats;
            int statIndex = stats.IndexOf(stat);
            if (statIndex < 0) return;

            string cmd;
            if (_currentMode == "Weapon")
            {
                // .weapon choosestat [WeaponIndex] [StatIndex]
                // Both parameters are numeric indices (1-based)
                cmd = string.Format(MessageService.BCCOM_WEAPON_CHOOSESTAT, _selectedItemIndex + 1, statIndex + 1);
            }
            else
            {
                // .blood choosestat [BloodTypeName] [StatIndex]
                // First parameter is the blood type NAME, second is the stat index (1-based)
                cmd = string.Format(MessageService.BCCOM_BLOOD_CHOOSESTAT, _selectedItem, statIndex + 1);
            }

            MessageService.EnqueueMessage(cmd);
        }
    }
}
