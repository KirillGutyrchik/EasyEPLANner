using EasyEPlanner.Binding.ViewModel;
using EasyEPlanner.Devices.ViewModel;
using Editor;
using EplanDevice;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using TechObject;

namespace EasyEPlanner.Binding.Tests
{
    public class BindingViewModelTest
    {
        [TearDown]
        public void TearDown()
        {
            ResetDeviceManager();
        }

        [Test]
        public void ShowSignalBinding_DefaultGroupingIsTypeThenObject()
        {
            var context = new BindingViewModel(null);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(BindingMode.SignalBinding, context.Mode);
                Assert.AreEqual(DevicesGroupingMode.TypeThenObject, context.GroupingMode);
                Assert.IsTrue(context.GroupingToggleVisible);
                Assert.IsFalse(context.CheckBoxesEnabled);
                Assert.AreEqual("Устройства проекта (0)", context.Root.Name);
            });
        }

        [Test]
        public void SignalTree_TypeThenObject_GroupsByTypeAndObject()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);

            var root = (BindingRoot)context.Root;
            var typeNode = root.Items.OfType<BindingTypeGroupNode>().Single();
            Assert.AreEqual("DO (1)", typeNode.Name);

            var objectNode = typeNode.Items.OfType<BindingObjectGroupNode>().Single();
            Assert.AreEqual("TANK2 (1)", objectNode.Name);

            var deviceNode = objectNode.Items.OfType<BindingDeviceNode>().Single();
            Assert.AreSame(device, deviceNode.Device);
            Assert.IsNotEmpty(deviceNode.Items.OfType<BindingChannelItem>());
        }

        [Test]
        public void SignalTree_ObjectThenType_GroupsByObjectAndType()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            context.GroupingMode = DevicesGroupingMode.ObjectThenType;
            context.RebuildTree();

            var root = (BindingRoot)context.Root;
            var objectNode = root.Items.OfType<BindingObjectGroupNode>().Single();
            var typeNode = objectNode.Items.OfType<BindingTypeGroupNode>().Single();
            var deviceNode = typeNode.Items.OfType<BindingDeviceNode>().Single();

            Assert.AreEqual("TANK2 (1)", objectNode.Name);
            Assert.AreEqual("DO (1)", typeNode.Name);
            Assert.AreSame(device, deviceNode.Device);
        }

        [Test]
        public void EditorDevicesTree_AlwaysObjectThenType_WithoutChannels()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);

            context.ShowEditorBinding(item, rebuildTree: true);

            Assert.AreEqual(BindingMode.ObjectBinding, context.Mode);
            Assert.AreEqual(BindingContentKind.Devices, context.ContentKind);
            Assert.IsTrue(context.CheckBoxesEnabled);
            Assert.IsFalse(context.GroupingToggleVisible);

            var root = (BindingRoot)context.Root;
            var objectNode = root.Items.OfType<BindingObjectGroupNode>().Single();
            var typeNode = objectNode.Items.OfType<BindingTypeGroupNode>().Single();
            var deviceNode = typeNode.Items.OfType<BindingDeviceNode>().Single();

            Assert.AreSame(device, deviceNode.Device);
            Assert.IsEmpty(deviceNode.Items.OfType<BindingChannelItem>());
        }

        [Test]
        public void EditorDevicesTree_FiltersByDeviceType()
        {
            var doDevice = CreateTankDoDevice();
            var aiDevice = CreateTankAiDevice();
            var context = CreateContextWithDevices(doDevice, aiDevice);
            var item = MockDevListItem(new[] { DeviceType.AI }, displayParameters: false,
                checkedValue: string.Empty);

            context.ShowEditorBinding(item, rebuildTree: true);

            var devices = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Select(d => d.Device)
                .ToArray();

            CollectionAssert.AreEqual(new[] { aiDevice }, devices);
        }

        [Test]
        public void ObjectGroupPriority_PreferredEplanNameFirstThenBoundDevices()
        {
            var preferred = new BindingObjectGroupNode(new BindingViewModel(null),
                null, "TANK1", "TANK", 1, "TANK1");
            var bound = new BindingObjectGroupNode(new BindingViewModel(null),
                null, "OTHER2", "OTHER", 2, "OTHER2");
            bound.AddChild(new BindingDeviceNode(bound.Context, bound,
                CreateOtherDoDevice(), "DO1"));
            var rest = new BindingObjectGroupNode(new BindingViewModel(null),
                null, "ZZZ1", "ZZZ", 1, "ZZZ1");

            Assert.AreEqual(0, BindingTreeBuilder.GetObjectGroupPriority(
                preferred, "TANK", 1, new HashSet<string> { "OTHER2DO1" }));
            Assert.AreEqual(2, BindingTreeBuilder.GetObjectGroupPriority(
                bound, "TANK", 1, new HashSet<string> { "OTHER2DO1" }));
            Assert.AreEqual(3, BindingTreeBuilder.GetObjectGroupPriority(
                rest, "TANK", 1, new HashSet<string> { "OTHER2DO1" }));
        }

        [Test]
        public void Checkboxes_CascadeToChildrenAndIndeterminateParents()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);
            context.ShowEditorBinding(item, rebuildTree: true);

            var root = (BindingRoot)context.Root;
            var objectNode = root.Items.OfType<BindingObjectGroupNode>().Single();
            context.SetItemCheckState(objectNode, CheckState.Checked);

            var deviceNode = BindingCheckHelper.Enumerate(objectNode)
                .OfType<BindingDeviceNode>().Single();
            Assert.AreEqual(CheckState.Checked, deviceNode.CheckState);
            Assert.AreEqual(CheckState.Checked, objectNode.CheckState);

            context.SetItemCheckState(deviceNode, CheckState.Unchecked);
            Assert.AreEqual(CheckState.Unchecked, deviceNode.CheckState);
            Assert.AreEqual(CheckState.Unchecked, objectNode.CheckState);
        }

        [Test]
        public void ApplyCheckedValues_MarksBoundDevice()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: device.Name);

            context.ShowEditorBinding(item, rebuildTree: true);

            var deviceNode = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Single();
            Assert.AreEqual(CheckState.Checked, deviceNode.CheckState);
            Assert.AreEqual(CheckState.Checked, deviceNode.ParentItem.CheckState);
        }

        [Test]
        public void CollectDevicesAndParameters_ReturnsCheckedDeviceNames()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);
            context.ShowEditorBinding(item, rebuildTree: true);

            var deviceNode = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Single();
            context.SetItemCheckState(deviceNode, CheckState.Checked);

            Assert.AreEqual(device.Name,
                BindingSelectionCollector.CollectDevicesAndParameters(context.Roots));
        }

        [Test]
        public void CollectRestrictions_BuildsObjectToModesDictionary()
        {
            var context = new BindingViewModel(null);
            var root = BindingTreeBuilder.BuildEmpty(context, "root");
            var tech = new BindingTechObjectNode(context, root, null, 3, "TANK1");
            var mode = new BindingModeNode(context, tech, new Mode("Операция 1", getN => 1, null), 3, 2);
            root.AddChild(tech);
            tech.AddChild(mode);
            mode.SetCheckStateInternal(CheckState.Checked);

            var dict = BindingSelectionCollector.CollectRestrictions(new IBindingRoot[] { root });
            CollectionAssert.AreEqual(new[] { 2 }, dict[3]);
        }

        [Test]
        public void CollectAttachedObjects_UsesTechObjectNumbers()
        {
            var context = new BindingViewModel(null);
            var root = BindingTreeBuilder.BuildEmpty(context, "root");
            var tech = new BindingTechObjectNode(context, root, null, 7, "TANK1");
            root.AddChild(tech);
            tech.SetCheckStateInternal(CheckState.Checked);

            var dict = BindingSelectionCollector.CollectAttachedObjects(new IBindingRoot[] { root });
            CollectionAssert.AreEqual(new[] { 7 }, dict.Keys.ToArray());
        }

        [Test]
        public void Filter_FindsDeviceByEplanName()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var root = (BindingRoot)context.Root;
            root.ResetFilter();

            Assert.IsTrue(root.Filter("+TANK2-DO1", hideEmptyItems: false));
            Assert.IsTrue(context.SearchContext.FoundItems
                .OfType<BindingDeviceNode>().Any());
        }

        [Test]
        public void ResolveContentKind_MapsEditorItems()
        {
            Assert.AreEqual(BindingContentKind.None,
                BindingViewModel.ResolveContentKind(null));

            var devices = MockDevListItem(new[] { DeviceType.V }, displayParameters: false,
                checkedValue: string.Empty);
            Assert.AreEqual(BindingContentKind.Devices,
                BindingViewModel.ResolveContentKind(devices));

            var parameters = MockDevListItem(new DeviceType[0], displayParameters: true,
                checkedValue: string.Empty);
            Assert.AreEqual(BindingContentKind.Parameters,
                BindingViewModel.ResolveContentKind(parameters));

            var both = MockDevListItem(new[] { DeviceType.V }, displayParameters: true,
                checkedValue: string.Empty);
            Assert.AreEqual(BindingContentKind.DevicesAndParameters,
                BindingViewModel.ResolveContentKind(both));
        }

        [Test]
        public void ParseBoundNames_SplitsBySpace()
        {
            CollectionAssert.AreEquivalent(
                new[] { "V1", "V2" },
                BindingViewModel.ParseBoundNames(" V1  V2 "));
        }

        [Test]
        public void MatchesTypeFilter_NullTypesMeansAll()
        {
            var device = CreateTankDoDevice();
            Assert.IsTrue(BindingTreeBuilder.MatchesTypeFilter(device, null, null));
            Assert.IsFalse(BindingTreeBuilder.MatchesTypeFilter(device,
                new[] { DeviceType.AI }, null));
        }

        private static ITreeViewItem MockDevListItem(
            DeviceType[] types,
            bool displayParameters,
            string checkedValue)
        {
            var mock = new Mock<ITreeViewItem>();
            mock.Setup(m => m.IsUseDevList).Returns(true);
            mock.Setup(m => m.EditText).Returns(new[] { "", checkedValue });
            DeviceType[] typesOut = types;
            DeviceSubType[] subOut = null;
            bool displayOut = displayParameters;
            mock.Setup(m => m.GetDisplayObjects(out typesOut, out subOut, out displayOut));
            return mock.Object;
        }

        private static DO CreateTankDoDevice()
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");
            return device;
        }

        private static DO CreateOtherDoDevice()
        {
            var device = new DO("OTHER2DO1", "+OTHER2-DO1", "desc", 1, "OTHER", 2);
            device.SetSubType("DO");
            return device;
        }

        private static AI CreateTankAiDevice()
        {
            var device = new AI("TANK2AI1", "+TANK2-AI1", "desc", 1, "TANK", 2);
            device.SetSubType("AI");
            return device;
        }

        private static BindingViewModel CreateContextWithDevices(params IODevice[] devices)
        {
            var manager = DeviceManager.GetInstance();
            manager.Devices.Clear();
            foreach (var device in devices)
                manager.Devices.Add(device);

            return new BindingViewModel(manager);
        }

        private static void ResetDeviceManager()
        {
            var instance = typeof(DeviceManager).GetField("instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            instance.SetValue(null, null);
        }
    }
}
