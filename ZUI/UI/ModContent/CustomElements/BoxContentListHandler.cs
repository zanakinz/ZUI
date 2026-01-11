using System;
using System.Collections.Generic;
using ZUI.UI.UniverseLib.UI.Widgets.ScrollView;

namespace ZUI.UI.ModContent.CustomElements
{
    /// <summary>
    /// A helper to create and handle a simple <see cref="ScrollPool{T}"/> of Buttons, which can be backed by any data.
    /// </summary>
    public class BoxContentListHandler<TData, TCell> : ICellPoolDataSource<TCell> where TCell : BoxContentCell
    {
        public ScrollPool<TCell> ScrollPool { get; private set; }

        public int ItemCount => CurrentEntries.Count;
        public List<TData> CurrentEntries { get; } = new();

        protected readonly Func<List<TData>> GetEntries;
        protected readonly Action<TCell, int> SetICell;
        protected readonly Func<TData, string, bool> ShouldDisplay;
        protected readonly Action<int> OnCellClicked;
        protected readonly Action<int> OnDeleteClicked;

        public string CurrentFilter
        {
            get => _currentFilter;
            set => _currentFilter = value ?? "";
        }
        private string _currentFilter;

        /// <summary>
        /// Create a wrapper to handle your Button ScrollPool.
        /// </summary>
        /// <param name="scrollPool">The ScrollPool&lt;ButtonCell&gt; you have already created.</param>
        /// <param name="getEntriesMethod">A method which should return your current data values.</param>
        /// <param name="setICellMethod">A method which should set the data at the int index to the cell.</param>
        /// <param name="shouldDisplayMethod">A method which should determine if the data at the index should be displayed, with an optional string filter from CurrentFilter.</param>
        /// <param name="onCellClickedMethod">A method invoked when a cell is clicked, containing the data index assigned to the cell.</param>
        public BoxContentListHandler(ScrollPool<TCell> scrollPool, Func<List<TData>> getEntriesMethod,
            Action<TCell, int> setICellMethod, Func<TData, string, bool> shouldDisplayMethod,
            Action<int> onCellClickedMethod, Action<int> onDeleteClicked)
        {
            ScrollPool = scrollPool;

            GetEntries = getEntriesMethod;
            SetICell = setICellMethod;
            ShouldDisplay = shouldDisplayMethod;
            OnCellClicked = onCellClickedMethod;
            OnDeleteClicked = onDeleteClicked;
        }

        public void RefreshData()
        {
            var allEntries = GetEntries();
            CurrentEntries.Clear();

            foreach (var entry in allEntries)
            {
                if (!string.IsNullOrEmpty(_currentFilter))
                {
                    if (!ShouldDisplay(entry, _currentFilter))
                        continue;
                }

                CurrentEntries.Add(entry);
            }
        }

        public virtual void OnCellBorrowed(TCell cell)
        {
            cell.OnClick += OnCellClicked;
            cell.OnDeleteClick += OnDeleteClicked;
        }

        public virtual void SetCell(TCell cell, int index)
        {
            if (CurrentEntries == null)
                RefreshData();

            if (index < 0 || index >= CurrentEntries.Count)
                cell.Disable();
            else
            {
                cell.Enable();
                cell.CurrentDataIndex = index;
                SetICell(cell, index);
            }
        }
    }
}
