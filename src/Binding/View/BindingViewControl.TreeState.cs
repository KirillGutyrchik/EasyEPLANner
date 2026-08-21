using EasyEPlanner.Binding.ViewModel;
using IO.ViewModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace EasyEPlanner.Binding.View
{
    public partial class BindingViewControl
    {
        private sealed class BindingTreeViewState
        {
            public int TopItemIndex { get; set; } = -1;

            public Point ScrollPosition { get; set; }

            public HashSet<string> ExpandedKeys { get; set; }

            public string SelectedKey { get; set; }
        }

        private BindingTreeViewState SaveTreeViewState()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var expandedObject in bindingTree.ExpandedObjects)
            {
                var key = BindingTreeViewKeys.GetViewItemKey(expandedObject);
                if (key is not null)
                    expandedKeys.Add(key);
            }

            return new BindingTreeViewState
            {
                TopItemIndex = bindingTree.TopItemIndex,
                ScrollPosition = bindingTree.LowLevelScrollPosition,
                ExpandedKeys = expandedKeys,
                SelectedKey = BindingTreeViewKeys.GetViewItemKey(bindingTree.SelectedObject),
            };
        }

        private void RestoreTreeViewState(BindingTreeViewState state)
        {
            if (state is null)
                return;

            bindingTree.BeginUpdate();
            try
            {
                RestoreExpandedByKeys(DataContext.Roots.Cast<IExpandable>(),
                    state.ExpandedKeys);

                if (!string.IsNullOrEmpty(state.SelectedKey))
                {
                    var selected = FindViewItemByKey(state.SelectedKey);
                    if (selected is not null)
                        bindingTree.SelectedObject = selected;
                }
            }
            finally
            {
                bindingTree.EndUpdate();
            }

            if (state.TopItemIndex >= 0 && state.TopItemIndex < bindingTree.GetItemCount())
                bindingTree.TopItemIndex = state.TopItemIndex;
            else
                bindingTree.LowLevelScroll(state.ScrollPosition.X, state.ScrollPosition.Y);
        }

        private void RestoreExpandedByKeys(
            IEnumerable<IExpandable> items,
            HashSet<string> expandedKeys)
        {
            foreach (var item in items)
            {
                if (item is object obj)
                {
                    var key = BindingTreeViewKeys.GetViewItemKey(obj);
                    if (key is not null && expandedKeys.Contains(key) &&
                        bindingTree.CanExpand(item))
                    {
                        bindingTree.Expand(item);
                        item.Expanded = true;
                    }
                }

                if (item.Items is not null)
                    RestoreExpandedByKeys(item.Items.OfType<IExpandable>(), expandedKeys);
            }
        }

        private object FindViewItemByKey(string key)
        {
            foreach (var root in DataContext.Roots.OfType<IExpandable>())
            {
                var found = FindViewItemByKey(root, key);
                if (found is not null)
                    return found;
            }

            return null;
        }

        private object FindViewItemByKey(IExpandable item, string key)
        {
            if (item is object obj &&
                string.Equals(BindingTreeViewKeys.GetViewItemKey(obj), key,
                    StringComparison.Ordinal))
            {
                return obj;
            }

            if (item.Items is null)
                return null;

            foreach (var child in item.Items.OfType<IExpandable>())
            {
                var found = FindViewItemByKey(child, key);
                if (found is not null)
                    return found;
            }

            return null;
        }

        private static IEnumerable<BindingChannelItem> CollectChannelItems(
            IEnumerable<IExpandable> items)
        {
            foreach (var item in items)
            {
                if (item is BindingChannelItem channelItem)
                    yield return channelItem;

                if (item.Items is not null)
                {
                    foreach (var child in CollectChannelItems(
                        item.Items.OfType<IExpandable>()))
                        yield return child;
                }
            }
        }
    }
}
