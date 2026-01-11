using System;
using System.Collections.Generic;
using System.Linq;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.ModContent;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI.Panels;
using UIManagerBase = ZUI.UI.ModernLib.UIManagerBase;

namespace ZUI.UI;

public class BCUIManager : UIManagerBase
{
    private List<IPanelBase> UIPanels { get; } = new();
    private IPanelBase _contentPanel;
    private readonly List<string> _visibilityAffectedPanels = new();

    public override void Reset()
    {
        base.Reset();
        foreach (var value in UIPanels)
        {
            if(value is ResizeablePanelBase panel)
                panel.Reset();
            value.Destroy();
        }

        UIPanels.Clear();
    }

    protected override void AddMainContentPanel()
    {
        AddPanel(PanelType.Base);
    }

    public override void SetActive(bool active)
    {
        if (active && _visibilityAffectedPanels.Any())
        {
            foreach (var p in _visibilityAffectedPanels.Select(panel =>
                         UIPanels.FirstOrDefault(a => a.PanelId.Equals(panel))))
                p?.SetActiveOnly(true);
            _visibilityAffectedPanels.Clear();
        }

        if (!active)
        {
            foreach (var panel in UIPanels.Where(a => a.Enabled))
            {
                _visibilityAffectedPanels.Add(panel.PanelId);
                panel.SetActiveOnly(false);
            }
        }

        _contentPanel?.SetActive(active);
    }

    public void AddPanel(PanelType type, string param = null)
    {
        switch (type)
        {
            case PanelType.Base:
                _contentPanel = new ContentPanel(UiBase);
                break;
            case PanelType.BoxList:
            {
                var panel = GetPanel<BoxListPanel>();
                if (panel == null)
                {
                    var item = new BoxListPanel(UiBase);
                    UIPanels.Add(item);
                    if (Plugin.IS_TESTING)
                    {
                        item.AddListEntry("Test 1 ");
                        item.AddListEntry("My sweet box1");
                        item.AddListEntry("My sweet box2");
                        item.AddListEntry("My sweet box3");
                        item.AddListEntry("My sweet box4");
                        item.AddListEntry("My sweet box5");
                        item.AddListEntry("My sweet box6");
                    }
                }
                else
                {
                    panel.SetActive(true);
                }

                break;
            }
            case PanelType.BoxContent:
            {
                var panel = GetBoxPanel(param);
                if (panel == null)
                    UIPanels.Add(new BoxContentPanel(UiBase, param));
                else
                {
                    panel.SetActive(true);
                }
                break;
            }
            case PanelType.FamStats:
            {
                var panel = GetPanel<FamStatsPanel>();
                if (panel == null)
                {
                    var item = new FamStatsPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
            }
                break;
            case PanelType.TestPanel:
            {
                var panel = GetPanel<TestPanel>();
                if (panel == null)
                {
                    var item = new TestPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }

                break;
            }
            case PanelType.LevelUp:
            {
                var panel = GetPanel<LevelUpPanel>();
                if (panel == null)
                {
                    var item = new LevelUpPanel(UiBase);
                    UIPanels.Add(item);
                    panel = item;
                }
                
                panel.SetActive(true);
                // "param" will optionally be "Weapon" or "Blood"
                bool isWeapon = param == "Weapon";
                panel.SetMode(isWeapon);
                break;
            }
            case PanelType.FamActions:
            {
                var panel = GetPanel<FamActionsPanel>();
                if (panel == null)
                {
                    var item = new FamActionsPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
                break;
            }
            case PanelType.Class:
            {
                var panel = GetPanel<ClassPanel>();
                if (panel == null)
                {
                    var item = new ClassPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
                break;
            }
            case PanelType.Quests:
            {
                var panel = GetPanel<QuestsPanel>();
                if (panel == null)
                {
                    var item = new QuestsPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
                break;
            }
            case PanelType.Signs:
            {
                var panel = GetPanel<SignsPanel>();
                if (panel == null)
                {
                    var item = new SignsPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
                break;
            }
            case PanelType.Admin:
            {
                var panel = GetPanel<AdminPanel>();
                if (panel == null)
                {
                    var item = new AdminPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
                break;
            }
            case PanelType.User:
            {
                var panel = GetPanel<UserPanel>();
                if (panel == null)
                {
                    var item = new UserPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
                break;
            }
            case PanelType.Mods:
            {
                var panel = GetPanel<ModsPanel>();
                if (panel == null)
                {
                    var item = new ModsPanel(UiBase);
                    UIPanels.Add(item);
                }
                else
                {
                    panel.SetActive(!panel.Enabled);
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    internal T GetPanel<T>()
        where T : class
    {
        var t = typeof(T);
        return UIPanels.FirstOrDefault(a => a.GetType() == t) as T;
    }

    internal BoxContentPanel GetBoxPanel(string currentBox)
    {
        return UIPanels.FirstOrDefault(a => a.PanelType == PanelType.BoxContent && a.PanelId.Equals(currentBox)) as
            BoxContentPanel;
    }

    protected override void UiUpdate()
    {
        base.UiUpdate();

        // Check for Escape key to close active panels
        // We iterate backwards or just handle the first active non-base panel
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
        {
            foreach (var panel in UIPanels)
            {
                if (panel.Enabled && panel.PanelType != PanelType.Base)
                {
                    panel.SetActive(false);
                    // Consume the input for one frame? Or close all?
                    // Typically closing one by one or all at once.
                    // Let's close all popups for now as users usually want "Close Menu".
                }
            }
        }
        
        // Per-panel updates if needed
        foreach (var panel in UIPanels)
        {
             if (panel is PanelBase pb && pb.Enabled)
             {
                 pb.Update();
             }
        }
    }
}