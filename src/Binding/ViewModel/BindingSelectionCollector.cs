using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.Binding.ViewModel
{
    public static class BindingSelectionCollector
    {
        public static string CollectDevicesAndParameters(
            IEnumerable<IBindingRoot> roots)
        {
            var names = new List<string>();
            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var node in BindingCheckHelper.Enumerate(root))
                {
                    if (node.CheckState != CheckState.Checked)
                        continue;

                    switch (node)
                    {
                        case BindingDeviceNode device:
                            names.Add(device.Device.Name);
                            break;
                        case BindingParameterNode parameter:
                            names.Add(parameter.LuaName);
                            break;
                    }
                }
            }

            return string.Join(" ", names);
        }

        public static IDictionary<int, List<int>> CollectRestrictions(
            IEnumerable<IBindingRoot> roots)
        {
            var dict = new Dictionary<int, List<int>>();
            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var node in BindingCheckHelper.Enumerate(root))
                {
                    if (node.CheckState != CheckState.Checked)
                        continue;

                    if (node is not BindingModeNode mode)
                        continue;

                    if (!dict.TryGetValue(mode.ObjectNumber, out var modes))
                    {
                        modes = [];
                        dict[mode.ObjectNumber] = modes;
                    }

                    if (!modes.Contains(mode.ModeNumber))
                        modes.Add(mode.ModeNumber);
                }
            }

            foreach (var modes in dict.Values)
                modes.Sort();

            return dict;
        }

        public static IDictionary<int, List<int>> CollectAttachedObjects(
            IEnumerable<IBindingRoot> roots)
        {
            var dict = new Dictionary<int, List<int>>();
            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var node in BindingCheckHelper.Enumerate(root))
                {
                    if (node.CheckState != CheckState.Checked)
                        continue;

                    if (node is not BindingTechObjectNode techObject)
                        continue;

                    dict[techObject.ObjectNumber] = [];
                }
            }

            return dict;
        }
    }
}
