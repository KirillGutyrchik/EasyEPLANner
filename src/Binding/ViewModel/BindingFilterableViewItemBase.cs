using EasyEPlanner.Devices.ViewModel;
using EasyEPlanner.Devices.ViewModel.ViewInterface;
using IO.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.Binding.ViewModel
{
    public abstract class BindingFilterableViewItemBase : IFilterableViewItem,
        IExpandable, IViewItem, IHasDevicesIcon, IBindingCheckable
    {
        private readonly List<IViewItem> items = [];
        private CheckState checkState = CheckState.Unchecked;

        protected BindingFilterableViewItemBase(IBindingViewModel context,
            BindingFilterableViewItemBase parent)
        {
            Context = context;
            Parent = parent;
        }

        public IBindingViewModel Context { get; }

        public BindingFilterableViewItemBase Parent { get; }

        public BindingFilterableViewItemBase ParentItem => Parent;

        public IEnumerable<IViewItem> Items => items;

        public bool Expanded { get; set; }

        public abstract string Name { get; protected set; }

        public virtual string Description => string.Empty;

        public virtual DevicesIcon Icon => DevicesIcon.None;

        public bool? Filtered { get; private set; }

        protected bool ThisOrParentsContains { get; set; }

        public CheckState CheckState => checkState;

        public virtual bool CanCheck =>
            Context?.CheckBoxesEnabled == true;

        public void AddChild(IViewItem child) => items.Add(child);

        public void AddChildren(IEnumerable<IViewItem> children) =>
            items.AddRange(children);

        public void ClearChildren() => items.Clear();

        public void SetChildren(IEnumerable<IViewItem> children)
        {
            items.Clear();
            items.AddRange(children);
        }

        public void SetCheckStateInternal(CheckState state) =>
            checkState = state;

        public bool Filter(string searchString, bool hideEmptyItems)
        {
            if (Filtered.HasValue)
                return Filtered.Value;

            if (string.IsNullOrEmpty(searchString))
            {
                Filtered = true;
                return true;
            }

            if (Contains(searchString))
            {
                if (!Context.SearchContext.FoundItems.Contains(this))
                    Context.SearchContext.FoundItems.Add(this);
                ThisOrParentsContains = true;
                Filtered = true;
            }

            ThisOrParentsContains |= Parent?.ThisOrParentsContains ?? false;

            var childsPassedFilter = false;
            foreach (var item in items.OfType<IFilterableViewItem>())
                childsPassedFilter |= item.Filter(searchString, hideEmptyItems);

            Filtered = childsPassedFilter || ThisOrParentsContains;
            return Filtered.Value;
        }

        public void ResetFilter()
        {
            Filtered = null;
            ThisOrParentsContains = false;
            foreach (var item in items.OfType<IFilterableViewItem>())
                item.ResetFilter();
        }

        public virtual bool Contains(string value) =>
            BindingSearch.Contains(GetSearchableText(), value);

        public virtual string GetSearchableText() =>
            $"{Name} {Description}".Trim();
    }
}
