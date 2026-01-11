using System.Collections.Generic;
using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Cells.Handlers;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.CustomElements;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.UI.UniverseLib.UI.Widgets.ScrollView;
using ZUI.Utils;
using ProjectM;
using UnityEngine;

namespace ZUI.UI.ModContent
{
    internal class BoxContentPanel : ResizeablePanelBase
    {
        public override string PanelId { get; }
        public override int MinWidth => 340;
        public override int MinHeight => 220;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 1f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.BoxContent;
        public override float Opacity => Settings.UITransparency;

        private readonly string _boxName;
        private bool _isInitialized;
        private ToggleRef _deleteToggle;
        private LabelRef _loadingLabel;
        private FrameTimer _loadingAnimationTimer;
        private int _loadingDots = 0;

        public BoxContentPanel(UIBase owner, string name) : base(owner)
        {
            PanelId = name;
            SetTitle(name);
            _boxName = name;
        }

        protected override void LateConstructUI()
        {
            base.LateConstructUI();
            SendUpdateCommand();
        }

        public override void SetActive(bool active)
        {
            var shouldUpdateData = _isInitialized && active && Enabled == false;
            _isInitialized = true;
            base.SetActive(active);
            if (shouldUpdateData)
                SendUpdateCommand();
        }

        protected override void OnClosePanelClicked()
        {
            HideLoadingIndicator(); // Stop animation when closing
            SetActive(false);
        }

        #region Commands
        public void SendUpdateCommand()
        {
            if (string.IsNullOrEmpty(_boxName))
                return;

            // Clear previous data so loading indicator works correctly
            _dataList.Clear();
            _scrollDataHandler?.RefreshData();
            _scrollPool?.Refresh(true);

            ShowLoadingIndicator();
            EnableAllButtons(false);
            MessageService.EnqueueMessage(string.Format(MessageService.BCCOM_SWITCHBOX, _boxName));
            TimerHelper.OneTickTimer(1000, () =>
            {
                if (Plugin.IS_TESTING)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        AddListEntry(i, $"Test Familiar {i}", AbilitySchoolType.Unholy);
                    }
                }

                MessageService.EnqueueMessage(MessageService.BCCOM_BOXCONTENT);
                MessageService.BoxContentFlag = true;
                
                // Don't hide loading indicator here - let AddListEntry hide it when data actually arrives
                // Just re-enable buttons after a reasonable timeout if no data comes
                TimerHelper.OneTickTimer(5000, () =>
                {
                    // Fallback: If no data after 5 seconds, hide loading and re-enable buttons
                    if (_dataList.Count == 0)
                    {
                        HideLoadingIndicator();
                        EnableAllButtons(true);
                    }
                });
            });
        }

        private void SendBindCommand(int number)
        {
            EnableAllButtons(false);
            MessageService.EnqueueMessage(MessageService.BCCOM_UNBINDFAM);
            TimerHelper.OneTickTimer(3000, () =>
            {
                try
                {
                    var message = string.Format(MessageService.BCCOM_BINDFAM, number);
                    Settings.LastBindCommand = message;
                    MessageService.EnqueueMessage(message);
                }
                finally
                {
                    EnableAllButtons(true);
                }
            });
        }

        private void SendDeleteCommand(int number)
        {
            EnableAllButtons(false);
            MessageService.EnqueueMessage(string.Format(MessageService.BCCOM_DELETEFAM, number));
            TimerHelper.OneTickTimer(2000, () => EnableAllButtons(true));
        }

        #endregion

        public void AddListEntry(int number, string name, AbilitySchoolType? schoolType)
        {
            // First entry being added means loading is complete
            if (_dataList.Count == 0)
            {
                HideLoadingIndicator();
                EnableAllButtons(true);
            }
            
            _dataList.Add(new FamDataListItem { Number = number, Name = name, SpellSchool = schoolType });
            _scrollDataHandler.RefreshData();
            _scrollPool.Refresh(true);
        }

        protected override void ConstructPanelContent()
        {
            // Loading indicator (initially hidden)
            _loadingLabel = UIFactory.CreateLabel(ContentRoot, "LoadingLabel", "Loading.", TMPro.TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(_loadingLabel.GameObject, minHeight: 50, flexibleWidth: 9999, flexibleHeight: 9999);
            _loadingLabel.TextMesh.fontSize = 16;
            _loadingLabel.TextMesh.color = new Color(1f, 1f, 1f, 0.8f);
            _loadingLabel.GameObject.SetActive(false);

            var horGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "CheckGroup", true, false, true, false, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            _deleteToggle = UIFactory.CreateToggle(horGroup, "ToggleDelete", text: "Enable delete buttons");
            UIFactory.SetLayoutElement(_deleteToggle.GameObject, minWidth: 250, minHeight: 25, flexibleWidth: 9999);
            _deleteToggle.Toggle.isOn = false;
            _deleteToggle.GameObject.SetActive(false); // Initially hidden until data loads
            _deleteToggle.OnValueChanged += (value) =>
            {
                foreach (var a in _scrollPool.CellPool)
                {
                    a.DeleteButton.SetEnabled(value);
                }
            };

            _scrollDataHandler = new BoxContentListHandler<FamDataListItem, BoxContentCell>(_scrollPool, GetEntries, SetCell, ShouldDisplay, OnCellClicked, OnDeleteClicked);
            _scrollPool = UIFactory.CreateScrollPool<BoxContentCell>(ContentRoot, "ContentList", out GameObject scrollObj,
                out _, new Color(0.03f, 0.03f, 0.03f, Theme.Opacity));
            _scrollPool.Initialize(_scrollDataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
        }

        internal override void Reset()
        {
            _dataList.Clear();
        }

        private void ShowLoadingIndicator()
        {
            if (_loadingLabel == null) return;
            
            _loadingDots = 0;
            _loadingLabel.GameObject.SetActive(true);
            _loadingLabel.TextMesh.text = "Loading.";
            
            // Hide the delete toggle while loading
            if (_deleteToggle != null)
                _deleteToggle.GameObject.SetActive(false);
            
            // Animate the dots: Loading. -> Loading.. -> Loading... -> Loading.
            _loadingAnimationTimer = new FrameTimer();
            _loadingAnimationTimer.Initialise(() =>
            {
                _loadingDots = (_loadingDots + 1) % 4; // 0, 1, 2, 3, 0...
                _loadingLabel.TextMesh.text = "Loading" + new string('.', _loadingDots == 0 ? 1 : _loadingDots);
            }, System.TimeSpan.FromMilliseconds(400), runOnce: false);
            _loadingAnimationTimer.Start();
        }

        private void HideLoadingIndicator()
        {
            if (_loadingLabel == null) return;
            
            _loadingLabel.GameObject.SetActive(false);
            
            // Show the delete toggle when loading is complete
            if (_deleteToggle != null)
                _deleteToggle.GameObject.SetActive(true);
            
            if (_loadingAnimationTimer != null && _loadingAnimationTimer.Enabled)
            {
                _loadingAnimationTimer.Stop();
            }
        }

        private void EnableAllButtons(bool value)
        {
            _deleteToggle.SetEnabled(value);

            foreach (var a in _scrollPool.CellPool)
            {
                a.ContentButton.SetEnabled(value);
                if(!value || _deleteToggle.Toggle.isOn)
                    a.DeleteButton.SetEnabled(value);
            }
        }

        #region ScrollPool handling

        private static ScrollPool<BoxContentCell> _scrollPool;
        private static BoxContentListHandler<FamDataListItem, BoxContentCell> _scrollDataHandler;

        private List<FamDataListItem> GetEntries() => _dataList;

        private bool ShouldDisplay(FamDataListItem data, string filter) => true;

        private void OnCellClicked(int dataIndex)
        {
            var fam = _dataList[dataIndex];
            SendBindCommand(fam.Number);
        }

        private void OnDeleteClicked(int dataIndex)
        {
            var fam = _dataList[dataIndex];
            SendDeleteCommand(fam.Number);
            _dataList.RemoveAt(dataIndex);
            _scrollDataHandler.RefreshData();
            _scrollPool.Refresh(true);
        }

        private void SetCell(BoxContentCell cell, int index)
        {
            if (index < 0 || index >= _dataList.Count)
            {
                cell.Disable();
                return;
            }

            var data = _dataList[index];
            cell.ContentButton.ButtonText.text = data.Name;
        }

        private readonly List<FamDataListItem> _dataList = new();

        public class FamDataListItem
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public AbilitySchoolType? SpellSchool { get; set; }
        }
        #endregion
    }
}
