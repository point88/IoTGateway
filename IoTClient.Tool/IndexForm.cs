using IoTClient.Common.Enums;
using IoTClient.Enums;
using IoTClient.Tool.Controls;
using IoTClient.Tool.Model;
using IoTServer.Common;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IoTClient.Tool
{
    public partial class IndexForm : Form
    {
        ModbusTcpControl modbusTcp;
        BACnetControl bacnet;
        ProfinetControl profinet;
        FinControl fin;
        public IndexForm()
        {

            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;

            modbusTcp = new ModbusTcpControl();
            modbusTcp.Dock = DockStyle.Fill;
            bacnet = new BACnetControl();
            bacnet.Dock = DockStyle.Fill;
            profinet = new ProfinetControl();
            profinet.Dock = DockStyle.Fill;
            fin = new FinControl();
            fin.Dock = DockStyle.Fill;

            DataPersist.LoadData();

            #region Initialize the last selected tab
            var tabName = GetTabName();
            if (!string.IsNullOrWhiteSpace(tabName))
            {
                foreach (TabPage item in tabContorl.TabPages)
                {
                    if (item.Name == tabName?.Trim())
                    {
                        tabContorl.SelectedTab = item;
                    }
                    SelectedTab(item);
                }
            }
            SelectedTab(tabContorl.SelectedTab);
            #endregion

            Task.Run(async () =>
            {
                await Task.Delay(1000 * 60 * 1);
                DataPersist.SaveData();
            });
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tab = (sender as TabControl).SelectedTab;
            SelectedTab(tab);
        }

        private void SelectedTab(TabPage tab)
        {
            Text = "IoTClient - " + tab.Name?.Trim();

            SaveTabName(tab.Name);
            if (tab.Controls.Count <= 0)
            {
                switch (tab.Name)
                {
                    case "ModbusTcp":
                        tab.Controls.Add(modbusTcp);
                        break;
                    case "BACnet":
                        tab.Controls.Add(bacnet);
                        break;
                    case "Profinet":
                        tab.Controls.Add(profinet);
                        break;
                    case "Fin":
                        tab.Controls.Add(fin);
                        break;
                }
            }
        }

        private void SaveTabName(string tagName)
        {
            var path = @"C:\IoTClient";
            var filePath = path + @"\TabName.Data";
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fileStream))
                {
                    sw.Write(tagName);
                }
            }
        }

        private string GetTabName()
        {
            var dataString = string.Empty;
            var path = @"C:\IoTClient";
            var filePath = path + @"\TabName.Data";
            if (File.Exists(filePath))
                dataString = File.ReadAllText(filePath);
            else
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.SetAttributes(path, FileAttributes.Hidden);
            }
            return dataString;
        }

        private void IndexForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DataPersist.SaveData();
        }

    }
}
