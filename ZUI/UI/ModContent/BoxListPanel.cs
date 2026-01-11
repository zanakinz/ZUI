using System.Collections.Generic;
using System.Linq;
using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Cells;
using ZUI.UI.CustomLib.Cells.Handlers;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.UI.UniverseLib.UI.Widgets.ScrollView;
using ZUI.Utils;
using UnityEngine;

namespace ZUI.UI.ModContent
{
    internal class BoxListPanel : ResizeablePanelBase
    {
        public override string PanelId => "BoxList";
        public override int MinWidth => 340;
        public override int MinHeight => 180;
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 1f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.BoxList;
        public override float Opacity => Settings.UITransparency;

        private LabelRef _loadingLabel;
        private FrameTimer _loadingAnimationTimer;
        private int _loadingDots = 0;

        public BoxListPanel(UIBase owner) : base(owner)
        {
            SetTitle("Box List");
        }

        public void AddListEntry(string name)
        {
            // First entry means data is arriving - hide loading indicator
            if (_dataList.Count == 0)
            {
                HideLoadingIndicator();
                EnableAllButtons(true);
            }
            
            if (_dataList.Any(a => a.Name.Equals(name)))
                return;
            _dataList.Add(new FamBoxData { Name = name });
            _scrollDataHandler.RefreshData();
            _scrollPool.Refresh(true);
        }

        protected override void LateConstructUI()
        {
            base.LateConstructUI();
            RunUpdateCommand();
        }

        protected override void OnClosePanelClicked()
        {
            HideLoadingIndicator(); // Stop animation when closing
            SetActive(false);
        }

        protected override void ConstructPanelContent()
        {
            // Loading indicator (initially hidden)
            _loadingLabel = UIFactory.CreateLabel(ContentRoot, "LoadingLabel", "Loading.", TMPro.TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(_loadingLabel.GameObject, minHeight: 50, flexibleWidth: 9999, flexibleHeight: 9999);
            _loadingLabel.TextMesh.fontSize = 16;
            _loadingLabel.TextMesh.color = new Color(1f, 1f, 1f, 0.8f);
            _loadingLabel.GameObject.SetActive(false);

            _scrollDataHandler = new ButtonListHandler<FamBoxData, ButtonCell>(_scrollPool, GetEntries, SetCell, ShouldDisplay, OnCellClicked);
            _scrollPool = UIFactory.CreateScrollPool<ButtonCell>(ContentRoot, "ContentList", out GameObject scrollObj,
                out _, new Color(0.03f, 0.03f, 0.03f, Opacity));
            _scrollPool.Initialize(_scrollDataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
        }

        internal override void Reset()
        {
            //Object.Destroy(UIRoot);
            _dataList.Clear();
            _scrollDataHandler.RefreshData();
            _scrollPool.Refresh(true);
        }

        private void ShowLoadingIndicator()
        {
            if (_loadingLabel == null) return;
            
            _loadingDots = 0;
            _loadingLabel.GameObject.SetActive(true);
            _loadingLabel.TextMesh.text = "Loading.";
            
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
            
            if (_loadingAnimationTimer != null && _loadingAnimationTimer.Enabled)
            {
                _loadingAnimationTimer.Stop();
            }
        }

        private void RunUpdateCommand()
        {
            // Clear previous data so loading detection works correctly
            _dataList.Clear();
            _scrollDataHandler?.RefreshData();
            _scrollPool?.Refresh(true);

            ShowLoadingIndicator();
            EnableAllButtons(false);
            MessageService.EnqueueMessage(MessageService.BCCOM_LISTBOXES1);
            
            // Fallback timeout: If no data after 5 seconds, hide loading and re-enable buttons
            TimerHelper.OneTickTimer(5000, () =>
            {
                if (_dataList.Count == 0)
                {
                    HideLoadingIndicator();
                    EnableAllButtons(true);
                }
            });
        }

        private void EnableAllButtons(bool value)
        {
            foreach (var a in _scrollPool.CellPool)
                a.Button.Component.interactable = value;
        }

        public override void SetActive(bool active)
        {
            var shouldUpdateData = _isInitialized && active && Enabled == false;
            _isInitialized = true;
            base.SetActive(active);
            if (shouldUpdateData)
                RunUpdateCommand();
        }

        #region ScrollPool handling

        private ScrollPool<ButtonCell> _scrollPool;
        private ButtonListHandler<FamBoxData, ButtonCell> _scrollDataHandler;
        private readonly List<FamBoxData> _dataList = new();
        private bool _isInitialized;

        private class FamBoxData
        {
            public string Name { get; set; }
        }

        private void OnCellClicked(int dataIndex)
        {
            var famBox = _dataList[dataIndex];
            Plugin.UIManager.AddPanel(PanelType.BoxContent, famBox.Name);
        }

        private bool ShouldDisplay(FamBoxData data, string filter) => true;
        private List<FamBoxData> GetEntries() => _dataList;

        private void SetCell(ButtonCell cell, int index)
        {
            if (index < 0 || index >= _dataList.Count)
            {
                cell.Disable();
                return;
            }
            cell.Button.ButtonText.text = _dataList[index].Name;
        }

        #endregion
    }
}
