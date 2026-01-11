using ZUI.Config;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using UnityEngine;
using UIBase = ZUI.UI.UniverseLib.UI.UIBase;

namespace ZUI.UI.ModContent
{
    internal class TestPanel : UIBehaviourModel, IPanelBase
    {
        private GameObject _uiRoot;

        public UIBase Owner { get; }
        public RectTransform Rect { get; private set; }
        public PanelType PanelType => PanelType.TestPanel;
        public string PanelId => "TestPanel";
        public override GameObject UIRoot => _uiRoot;
        public PanelDragger Dragger { get; internal set; }
        public float Opacity => Settings.UITransparency;

        public TestPanel(UIBase uiBase)
        {
            Owner = uiBase;
            ConstructUI();
            Owner.Panels.AddPanel(this);
        }

        private void ConstructUI()
        {
            _uiRoot = UIFactory.CreatePanel(PanelId, Owner.Panels.PanelHolder, out GameObject contentRoot);
        }


        public override void Destroy()
        {
        }

        public void EnsureValidSize()
        {

        }

        public void EnsureValidPosition()
        {

        }

        public void SetActiveOnly(bool active)
        {
            throw new System.NotImplementedException();
        }
    }
}

