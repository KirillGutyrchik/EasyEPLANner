﻿using Eplan.EplApi.DataModel.Graphics;
using Eplan.EplApi.HEServices;
using EplanDevice;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyEPlanner.ProjectImportICP
{
    /// <summary>
    /// Диалог для модификации базы каналов
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ModifyChannelsDBDialog : Form
    {
        private readonly string projectName;

        public ModifyChannelsDBDialog(string projectName)
        {
            this.projectName = projectName;

            InitializeComponent();
        }


        private sealed class BoolComboBox
        {
            public bool Value { get; set; }
            public string Display { get; set; }
        }
        
        private void ModifyChannelsDBDialog_Load(object sender, EventArgs e)
        {
            var YN = new List<BoolComboBox>()
            {
                new BoolComboBox() { Value = true, Display = "Да" },
                new BoolComboBox() { Value = false, Display = "Нет" },
            };

            CombineTagCmbBx.DropDownStyle = ComboBoxStyle.DropDownList;
            CombineTagCmbBx.DataSource = YN;
            CombineTagCmbBx.DisplayMember = nameof(BoolComboBox.Display);
            CombineTagCmbBx.ValueMember = nameof(BoolComboBox.Value);
            CombineTagCmbBx.SelectedValue = true;

            var Formats = new List<BoolComboBox>()
            {
                new BoolComboBox() { Value = true, Display = "По индексам" },
                new BoolComboBox() { Value = false, Display = "По именам" },
            };

            FormatTagCmbBx.DropDownStyle = ComboBoxStyle.DropDownList;
            FormatTagCmbBx.DataSource = Formats;
            FormatTagCmbBx.DisplayMember = nameof(BoolComboBox.Display);
            FormatTagCmbBx.ValueMember = nameof(BoolComboBox.Value);
            FormatTagCmbBx.SelectedValue = true;
        }

        private void SrcChbasePathBttn_Click(object sender, EventArgs e)
        {
            var open = new OpenFileDialog()
            {
                Title = "Исходная база каналов (ICP-CON)",
                Filter = "*.cdbx|*.cdbx",
                Multiselect = false,
            };

            if (open.ShowDialog() == DialogResult.Cancel)
                return;

            SrcChbasePathTextBox.Text = open.FileName;
        }

        private void ChbasePathBttn_Click(object sender, EventArgs e)
        {
            var openFileDlg = new SaveFileDialog()
            {
                Title = "База каналов",
                Filter = "База каналов (*.cdbx)|*.cdbx",
                FileName = $"{projectName}",
                DefaultExt = "cdbx",
                InitialDirectory = Path.GetDirectoryName(DstChbasePathTextBox.Text),
            };

            if (openFileDlg.ShowDialog() == DialogResult.Cancel)
                return;

            DstChbasePathTextBox.Text = openFileDlg.FileName;
        }


        private void ModifyBttn_Click(object sender, EventArgs e)
        {
            var srcPath = SrcChbasePathTextBox.Text;
            var dstPath = DstChbasePathTextBox.Text;

            var needExport = !File.Exists(dstPath);

            if (!File.Exists(srcPath))
                return;

            // Экспорт новой базы каналов, если ее не существует
            if (needExport)
                ExportChbase(dstPath);

            // Чтение баз каналов
            var srcChbase = ReadFile(srcPath);
            var dstChbase = ReadFile(dstPath);

            Logs.Clear();
            Logs.Show();
            
            // Получение индекса драйвера старой базы каналов
            var srcDriverID = ChannelBaseTransformer.GetDriverID(srcChbase);

            // Смещение индексов новой базы каналов, для записи старых индексов для избежания повторов
            dstChbase = ChannelBaseTransformer.ShiftID(dstChbase);

            // Выключение всех тегов базы каналов, нужные будут включены
            dstChbase = ChannelBaseTransformer.DisableAllChannels(dstChbase);

            // Модификация базы каналов
            var modifiedDstChbase = ChannelBaseTransformer.ModifyID(dstChbase, srcChbase, GetDevicesNames());

            // Изменение ID драйвера и всех каналов
            modifiedDstChbase = ChannelBaseTransformer.ModifyDriverID(modifiedDstChbase, srcDriverID);

            // Проверка совпадения индексов
            ChannelBaseTransformer.CheckChbaseID(modifiedDstChbase);

            using (var writer = new StreamWriter(dstPath, false, Encoding.UTF8))
                writer.Write(modifiedDstChbase);

            Logs.SetProgress(100);
            Logs.EnableButtons();

            Close();
        }

        /// <summary>
        /// Экспорт новой базы каналов
        /// </summary>
        private void ExportChbase(string path)
        {
            var reporter = new XMLReporter();
            reporter.AwaitSaveAsCDBX(
                path,
                (bool)CombineTagCmbBx.SelectedValue,
                (bool)FormatTagCmbBx.SelectedValue,
                true);
        }

        /// <summary>
        /// Прочитать файл базы каналов
        /// </summary>
        private static string ReadFile(string path)
        {
            using (var reader = new StreamReader(path, EncodingDetector.DetectFileEncoding(path), true))
               return reader.ReadToEnd();
        }

        /// <summary>
        /// Получить названия устройств (новое - старое)
        /// </summary>
        private static IEnumerable<(string Name, string WagoName)> GetDevicesNames()
        {
            var apiHelper = new ApiHelper();
            var deviceNames = DeviceManager.GetInstance().Devices.Select(d =>
            {
                var wagoName = apiHelper.GetSupplementaryFieldValue(d.EplanObjectFunction, 10);

                if (wagoName == string.Empty)
                {
                    wagoName = ChannelBaseTransformer.ToWagoDevice(d);
                    if (wagoName != string.Empty)
                        Logs.AddMessage($"Старое название тега для устройства {d.Name} не указано, используется название: {wagoName} \n");
                }

                return (d.Name, wagoName);
            });
            return deviceNames;
        }

        private void DstChbasePathTextBox_TextChanged(object sender, EventArgs e)
        {
            var fileExists = File.Exists(DstChbasePathTextBox.Text);

            CombineTagCmbBx.Enabled = !fileExists;
            FormatTagCmbBx.Enabled = !fileExists;
        }

        private void CancelBttn_Click(object sender, EventArgs e) => Close();
    }
}
