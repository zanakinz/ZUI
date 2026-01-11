using System;
using ZUI.UI.UniverseLib.UI.Widgets.ScrollView;

namespace ZUI.UI.CustomLib.Cells;

public interface IFormedCell : ICell
{
    public int CurrentDataIndex { get; set; }
    public Action<int> OnClick { get; set; }
}
