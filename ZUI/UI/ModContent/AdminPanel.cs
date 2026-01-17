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
using UnityEngine.UI;

namespace ZUI.UI.ModContent
{
    internal class AdminPanel : ResizeablePanelBase
    {
        public override string PanelId => "AdminPanel";
        public override int MinWidth => 450;
        public override int MinHeight => 600;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.Admin;
        public override float Opacity => Settings.UITransparency;

        private GameObject _contentLayout;
        private InputFieldRef _playerInput;
        private InputFieldRef _xInput, _yInput, _zInput;
        private InputFieldRef _dayInput, _hourInput;
        private InputFieldRef _regionInput;
        private InputFieldRef _itemPrefabInput, _itemAmountInput;
        private InputFieldRef _npcGuidInput, _npcAmountInput, _npcLevelInput;

        // Sprite Cache
        private Sprite _btnNormalSprite;
        private Sprite _btnSelectedSprite;

        public AdminPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            // --- LOAD SPRITES ---
            var panelSprite = SpriteLoader.LoadSprite("panel.png", 100f, new Vector4(30, 30, 30, 30));
            _btnNormalSprite = SpriteLoader.LoadSprite("button.png", 100f, new Vector4(10, 10, 10, 10));
            _btnSelectedSprite = SpriteLoader.LoadSprite("button_selected.png", 100f, new Vector4(10, 10, 10, 10));

            // Apply Panel Background
            if (panelSprite != null)
            {
                var bgImage = ContentRoot.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = panelSprite;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = Color.white;
                }
            }

            SetTitle("Admin Commands");

            var scrollView = UIFactory.CreateScrollView(ContentRoot, "ScrollView", out _contentLayout, out var autoScroll,
                new Color(0.05f, 0.05f, 0.05f, 0f)); // Transparent bg for scroll view to let panel show through
            UIFactory.SetLayoutElement(scrollView, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_contentLayout, false, false, true, true, 5, 10, 10, 10, 10);

            // Check for KindredCommands dependency
            if (!DependencyService.HasKindredCommands)
            {
                CreateMissingDependencyMessage("KindredCommands");
                return;
            }

            // Warning label
            var warningLabel = UIFactory.CreateLabel(_contentLayout, "Warning",
                "? ADMIN ONLY - Use these commands responsibly ?", TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(warningLabel.GameObject, minHeight: 30, flexibleWidth: 9999);
            warningLabel.TextMesh.fontStyle = FontStyles.Bold;
            warningLabel.TextMesh.fontSize = 14;
            warningLabel.TextMesh.color = new Color(1f, 0.5f, 0f, 1f);

            CreateDivider();

            // PLAYER MANAGEMENT SECTION
            CreateSectionTitle("Player Management");

            // Player input field (shared)
            CreateInputField("Player Name:", ref _playerInput, "PlayerName");

            // God Mode & Mortal Mode buttons
            var godMortalContainer = UIFactory.CreateHorizontalGroup(_contentLayout, "GodMortalContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(godMortalContainer, minHeight: 35, flexibleWidth: 9999);

            var godBtn = UIFactory.CreateButton(godMortalContainer, "GodBtn", "God Mode");
            UIFactory.SetLayoutElement(godBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(godBtn);
            godBtn.OnClick = () => ExecuteCommand(string.Format(MessageService.BCCOM_ADMIN_GOD, _playerInput.Text));

            var mortalBtn = UIFactory.CreateButton(godMortalContainer, "MortalBtn", "Mortal Mode");
            UIFactory.SetLayoutElement(mortalBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(mortalBtn);
            mortalBtn.OnClick = () => ExecuteCommand(string.Format(MessageService.BCCOM_ADMIN_MORTAL, _playerInput.Text));

            // Revive & Kill buttons
            var reviveKillContainer = UIFactory.CreateHorizontalGroup(_contentLayout, "ReviveKillContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(reviveKillContainer, minHeight: 35, flexibleWidth: 9999);

            var reviveBtn = UIFactory.CreateButton(reviveKillContainer, "ReviveBtn", "Revive Player");
            UIFactory.SetLayoutElement(reviveBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(reviveBtn);
            reviveBtn.OnClick = () => ExecuteCommand(string.Format(MessageService.BCCOM_ADMIN_REVIVE, _playerInput.Text));

            var killBtn = UIFactory.CreateButton(reviveKillContainer, "KillBtn", "Kill Player");
            UIFactory.SetLayoutElement(killBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(killBtn);
            killBtn.OnClick = () => ExecuteCommand(string.Format(MessageService.BCCOM_ADMIN_KILL, _playerInput.Text));

            // Teleport section
            var teleportLabel = UIFactory.CreateLabel(_contentLayout, "TeleportLabel", "Teleport Coordinates:", TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(teleportLabel.GameObject, minHeight: 20, flexibleWidth: 9999);
            teleportLabel.TextMesh.fontSize = 11;

            var coordsContainer = UIFactory.CreateHorizontalGroup(_contentLayout, "CoordsContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(coordsContainer, minHeight: 30, flexibleWidth: 9999);

            _xInput = CreateSmallInput(coordsContainer, "X", "0");
            _yInput = CreateSmallInput(coordsContainer, "Y", "0");
            _zInput = CreateSmallInput(coordsContainer, "Z", "0");

            var teleportBtn = UIFactory.CreateButton(_contentLayout, "TeleportBtn", "Teleport Player");
            UIFactory.SetLayoutElement(teleportBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(teleportBtn);
            teleportBtn.OnClick = () =>
            {
                var cmd = $".teleport {_xInput.Text} {_yInput.Text} {_zInput.Text} {_playerInput.Text}";
                ExecuteCommand(cmd);
            };

            CreateDivider();

            // SERVER MANAGEMENT SECTION
            CreateSectionTitle("Server Management");

            // Set Time
            var timeLabel = UIFactory.CreateLabel(_contentLayout, "TimeLabel", "Set Server Time:", TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(timeLabel.GameObject, minHeight: 20, flexibleWidth: 9999);
            timeLabel.TextMesh.fontSize = 11;

            var timeContainer = UIFactory.CreateHorizontalGroup(_contentLayout, "TimeContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(timeContainer, minHeight: 30, flexibleWidth: 9999);

            var dayLabel = UIFactory.CreateLabel(timeContainer, "DayLabel", "Day:");
            UIFactory.SetLayoutElement(dayLabel.GameObject, minWidth: 40, minHeight: 30);
            _dayInput = CreateSmallInput(timeContainer, "Day", "1");

            var hourLabel = UIFactory.CreateLabel(timeContainer, "HourLabel", "Hour:");
            UIFactory.SetLayoutElement(hourLabel.GameObject, minWidth: 45, minHeight: 30);
            _hourInput = CreateSmallInput(timeContainer, "Hour", "12");

            var setTimeBtn = UIFactory.CreateButton(_contentLayout, "SetTimeBtn", "Set Time");
            UIFactory.SetLayoutElement(setTimeBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(setTimeBtn);
            setTimeBtn.OnClick = () =>
            {
                var cmd = $".settime {_dayInput.Text} {_hourInput.Text}";
                ExecuteCommand(cmd);
            };

            // Region Management
            CreateInputField("Region Name:", ref _regionInput, "dunley");

            var regionContainer = UIFactory.CreateHorizontalGroup(_contentLayout, "RegionContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(regionContainer, minHeight: 35, flexibleWidth: 9999);

            var lockRegionBtn = UIFactory.CreateButton(regionContainer, "LockRegionBtn", "Lock Region");
            UIFactory.SetLayoutElement(lockRegionBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(lockRegionBtn);
            lockRegionBtn.OnClick = () => ExecuteCommand(string.Format(MessageService.BCCOM_ADMIN_REGION_LOCK, _regionInput.Text));

            var unlockRegionBtn = UIFactory.CreateButton(regionContainer, "UnlockRegionBtn", "Unlock Region");
            UIFactory.SetLayoutElement(unlockRegionBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(unlockRegionBtn);
            unlockRegionBtn.OnClick = () => ExecuteCommand(string.Format(MessageService.BCCOM_ADMIN_REGION_UNLOCK, _regionInput.Text));

            CreateDivider();

            // ITEMS & SPAWNING SECTION
            CreateSectionTitle("Items & Spawning");

            // Give Item
            var itemLabel = UIFactory.CreateLabel(_contentLayout, "ItemLabel", "Give Item:", TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(itemLabel.GameObject, minHeight: 20, flexibleWidth: 9999);
            itemLabel.TextMesh.fontSize = 11;

            CreateInputField("Item Prefab GUID:", ref _itemPrefabInput, "-257494203");
            CreateInputField("Amount:", ref _itemAmountInput, "100");

            var giveItemBtn = UIFactory.CreateButton(_contentLayout, "GiveItemBtn", "Give Item");
            UIFactory.SetLayoutElement(giveItemBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(giveItemBtn);
            giveItemBtn.OnClick = () =>
            {
                var cmd = $".give {_itemPrefabInput.Text} {_itemAmountInput.Text}";
                ExecuteCommand(cmd);
            };

            // Spawn NPC
            var npcLabel = UIFactory.CreateLabel(_contentLayout, "NpcLabel", "Spawn NPC:", TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(npcLabel.GameObject, minHeight: 20, flexibleWidth: 9999);
            npcLabel.TextMesh.fontSize = 11;

            CreateInputField("NPC GUID:", ref _npcGuidInput, "CHAR_Bandit_Bomber");

            var npcParamsContainer = UIFactory.CreateHorizontalGroup(_contentLayout, "NpcParams", false, false, true, true, 5);
            UIFactory.SetLayoutElement(npcParamsContainer, minHeight: 30, flexibleWidth: 9999);

            var amtLabel = UIFactory.CreateLabel(npcParamsContainer, "AmtLabel", "Amount:");
            UIFactory.SetLayoutElement(amtLabel.GameObject, minWidth: 60, minHeight: 30);
            _npcAmountInput = CreateSmallInput(npcParamsContainer, "Amount", "1");

            var lvlLabel = UIFactory.CreateLabel(npcParamsContainer, "LvlLabel", "Level:");
            UIFactory.SetLayoutElement(lvlLabel.GameObject, minWidth: 50, minHeight: 30);
            _npcLevelInput = CreateSmallInput(npcParamsContainer, "Level", "50");

            var spawnNpcBtn = UIFactory.CreateButton(_contentLayout, "SpawnNpcBtn", "Spawn NPC");
            UIFactory.SetLayoutElement(spawnNpcBtn.GameObject, minHeight: 35, flexibleWidth: 9999);
            StyleButton(spawnNpcBtn);
            spawnNpcBtn.OnClick = () =>
            {
                var cmd = $".spawnnpc {_npcGuidInput.Text} {_npcAmountInput.Text} {_npcLevelInput.Text}";
                ExecuteCommand(cmd);
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

        private void CreateSectionTitle(string title)
        {
            var label = UIFactory.CreateLabel(_contentLayout, $"{title}Title", title, TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(label.GameObject, minHeight: 30, flexibleWidth: 9999);
            label.TextMesh.fontStyle = FontStyles.Bold;
            label.TextMesh.fontSize = 14;
            label.TextMesh.color = new Color(0.3f, 0.7f, 1f, 1f);
        }

        private void CreateDivider()
        {
            var divider = UIFactory.CreateUIObject("Divider", _contentLayout);
            UIFactory.SetLayoutElement(divider, minHeight: 2, flexibleWidth: 9999);
            divider.AddComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }

        private void CreateInputField(string label, ref InputFieldRef field, string placeholder)
        {
            var labelObj = UIFactory.CreateLabel(_contentLayout, $"{label}Label", label, TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(labelObj.GameObject, minHeight: 20, flexibleWidth: 9999);
            labelObj.TextMesh.fontSize = 11;

            field = UIFactory.CreateInputField(_contentLayout, $"{label}Input", placeholder);
            UIFactory.SetLayoutElement(field.GameObject, minHeight: 30, flexibleWidth: 9999);
        }

        private InputFieldRef CreateSmallInput(GameObject parent, string name, string placeholder)
        {
            var input = UIFactory.CreateInputField(parent, name, placeholder);
            UIFactory.SetLayoutElement(input.GameObject, minHeight: 30, flexibleWidth: 9999);
            return input;
        }

        private void StyleButton(ButtonRef btn)
        {
            if (_btnNormalSprite == null) return;

            var img = btn.GameObject.GetComponent<Image>();
            if (img)
            {
                img.sprite = _btnNormalSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            if (_btnSelectedSprite != null)
            {
                var comp = btn.Component;
                comp.transition = Selectable.Transition.SpriteSwap;
                var state = comp.spriteState;
                state.highlightedSprite = _btnSelectedSprite;
                state.pressedSprite = _btnSelectedSprite;
                state.selectedSprite = _btnSelectedSprite;
                comp.spriteState = state;
            }
        }

        private void ExecuteCommand(string command)
        {
            MessageService.EnqueueMessage(command);
            Plugin.LogInstance.LogInfo($"Admin command: {command}");
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