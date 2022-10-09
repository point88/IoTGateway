﻿using IoTClient.Enums;
using IoTClient.Tool.Helper;
using IoTClient.Tool.Model;
using IoTServer.Servers.BACnet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO.BACnet;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Talk.Linq.Extensions;
using Talk.NPOI;

namespace IoTClient.Tool
{
    public partial class BACnetControl : UserControl
    {
        public BACnetControl()
        {
            InitializeComponent();
            txt_msgList.ScrollBars = ScrollBars.Vertical;
            Size = new Size(880, 450);
        }
        private static List<BacNode> devicesList = new List<BacNode>();
        private BacnetClient Bacnet_client;
        //private BACnetServer Bacnet_server = new BACnetServer();

        private void BACnetControl_Load(object sender, EventArgs e)
        {
            but_export.Enabled = false;

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(GetIpList().ToArray());
            comboBox1.SelectedIndex = 0;
            toolTip1.SetToolTip(comboBox2, "write priority");
            toolTip1.SetToolTip(txt_address, "Fill in roll call or address");
            toolTip1.SetToolTip(button5, "Ctrl+click will release all priority values");

        }



        public List<string> GetIpList()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                       .Where(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up)
                       //.OrderByDescending(c => c.Speed)
                       .SelectMany(c => c.GetIPProperties().UnicastAddresses.Where(t => t.Address.AddressFamily == AddressFamily.InterNetwork).Select(t => t.Address.ToString()))
                       .OrderBy(t => t.StartsWith("192.168") ? 0 : 1).ThenBy(t => t)
                       .ToList();
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            button1_Click_1(null, null);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            txt_msgList.Text = string.Empty;
            button1.Enabled = false;
            button1.Text = "scanning...";
            comboBox1.Enabled = false;
            but_export.Enabled = false;
            devicesList = new List<BacNode>();
            listBox1.Items.Clear();
            Bacnet_client?.Dispose();
            Bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(47808, false, localEndpointIp: comboBox1.SelectedItem.ToString()));
            //Bacnet_client.WritePriority = 1;
            comboBox2.SelectedIndex = 15;
            Bacnet_client.OnIam -= new BacnetClient.IamHandler(handler_OnIam);
            Bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);
            Bacnet_client.Start();
            Bacnet_client.WhoIs();
            Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(100);
                    Log($"waiting to scan...[{9 - i}]");
                }
                if (listBox1.Items.Count == 1)
                    listBox1.SelectedIndex = 0;
                Scan();
                button1.Enabled = true;
                button1.Text = "rescan";
                comboBox1.Enabled = true;
                if (bacnetPropertyInfos.IsAny())
                    but_export.Enabled = true;
            });
        }

        private void handler_OnIam(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxAPDU, BacnetSegmentations segmentation, ushort vendorId)
        {
            BeginInvoke(new Action(() =>
            {
                lock (devicesList)
                {
                    foreach (BacNode bn in devicesList)
                        if (bn.GetAdd(deviceId) != null) return;   // Yes

                    devicesList.Add(new BacNode(adr, deviceId));   // add it 
                    listBox1.Items.Add(adr.ToString() + " " + deviceId);
                }
            }));
        }

        private List<BacnetPropertyInfo> bacnetPropertyInfos = new List<BacnetPropertyInfo>();

        private void Scan()
        {
            bacnetPropertyInfos = new List<BacnetPropertyInfo>();
            Log("Start scanning for devices...");
            foreach (var device in devicesList)
            {
                var deviceCount = GetDeviceArrayIndexCount(device) + 1;
                ScanPointsBatch(device, 20, deviceCount);
            }
            foreach (var device in devicesList)
            {
                LogEmpty();
                Log($"Start scanning properties,Address:{device.Address} DeviceId:{device.DeviceId}");
                ScanSubProperties(device);
                if (bacnetPropertyInfos.IsAny())
                    bacnetPropertyInfos.Add(new BacnetPropertyInfo());
            }
            Log("Scan complete");
        }

        public void ScanPointsBatch(BacNode device, uint deviceCount, uint count)
        {
            try
            {
                if (device == null) return;
                var pid = BacnetPropertyIds.PROP_OBJECT_LIST;
                var device_id = device.DeviceId;
                var bobj = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id);
                var adr = device.Address;
                if (adr == null) return;

                device.Properties.Clear();
                List<BacnetPropertyReference> rList = new List<BacnetPropertyReference>();
                for (uint i = 0; i < count; i++)
                {
                    rList.Add(new BacnetPropertyReference((uint)pid, i));
                    if ((i != 0 && i % deviceCount == 0) || i == count - 1)
                    {
                        IList<BacnetReadAccessResult> lstAccessRst = Bacnet_client.ReadPropertyMultipleRequest(adr, bobj, rList);
                        if (lstAccessRst?.Any() ?? false)
                        {
                            foreach (var aRst in lstAccessRst)
                            {
                                if (aRst.values == null) continue;
                                foreach (var bPValue in aRst.values)
                                {
                                    if (bPValue.value == null) continue;
                                    foreach (var bValue in bPValue.value)
                                    {
                                        var strBValue = "" + bValue.Value;

                                        var strs = strBValue.Split(':');
                                        if (strs.Length < 2) continue;
                                        var strType = strs[0];
                                        var strObjId = strs[1];
                                        var subNode = new BacProperty();
                                        BacnetObjectTypes otype;
                                        Enum.TryParse(strType, out otype);
                                        if (otype == BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS || otype == BacnetObjectTypes.OBJECT_DEVICE) continue;
                                        subNode.ObjectId = new BacnetObjectId(otype, Convert.ToUInt32(strObjId));
                                        
                                        device.Properties.Add(subNode);
                                    }
                                }
                            }
                        }
                        rList.Clear();
                    }
                }
            }
            catch (Exception exp)
            {
                Log("=== 【Err】" + exp.Message + " ===");
            }
        }

        public uint GetDeviceArrayIndexCount(BacNode device)
        {
            try
            {
                var adr = device.Address;
                if (adr == null) return 0;
                var bacnetValue = ReadScalarValue(adr,
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.DeviceId),
                    BacnetPropertyIds.PROP_OBJECT_LIST, 0, 0);
                var rst = Convert.ToUInt32(bacnetValue.Value);
                return rst;
            }
            catch (Exception ex)
            {
                Log("=== 【Err】" + ex.Message + " ===");
            }
            return 0;
        }

        private BacnetValue ReadScalarValue(BacnetAddress adr, BacnetObjectId oid,
            BacnetPropertyIds pid, byte invokeId = 0, uint arrayIndex = uint.MaxValue)
        {
            try
            {
                BacnetValue NoScalarValue = Bacnet_client.ReadPropertyRequest(adr, oid, pid, arrayIndex);
                return NoScalarValue;
            }
            catch (Exception ex)
            {
                Log("=== 【Err】" + ex.Message + " ===");
            }
            return new BacnetValue();
        }

        private void ScanSubProperties(BacNode device)
        {
            try
            {
                var adr = device.Address;
                if (adr == null) return;
                if (device.Properties == null) return;

                List<BacnetPropertyReference> rList = new List<BacnetPropertyReference>();
                rList.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_DESCRIPTION, uint.MaxValue));
                rList.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_REQUIRED, uint.MaxValue));
                rList.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_NAME, uint.MaxValue));
                rList.Add(new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, uint.MaxValue));

                List<BacnetReadAccessResult> lstAccessRst = new List<BacnetReadAccessResult>();
                var batchNumber = (int)numericUpDown1.Value;
                var batchCount = Math.Ceiling((float)device.Properties.Count / batchNumber);
                for (int i = 0; i < batchCount; i++)
                {
                    IList<BacnetReadAccessSpecification> properties = device.Properties.Skip(i * batchNumber).Take(batchNumber)
                        .Select(t => new BacnetReadAccessSpecification(t.ObjectId, rList)).ToList();

                    lstAccessRst.AddRange(Bacnet_client.ReadPropertyMultipleRequest(adr, properties));
                }

                if (lstAccessRst?.Any() ?? false)
                {
                    foreach (var aRst in lstAccessRst)
                    {
                        if (aRst.values == null) continue;
                        var subNode = device.Properties
                            .Where(t => t.ObjectId.Instance == aRst.objectIdentifier.Instance && t.ObjectId.Type == aRst.objectIdentifier.Type)
                            .FirstOrDefault();
                        foreach (var bPValue in aRst.values)
                        {
                            if (bPValue.value == null || bPValue.value.Count == 0) continue;
                            var pid = (BacnetPropertyIds)(bPValue.property.propertyIdentifier);
                            var bValue = bPValue.value.First();
                            var strBValue = "" + bValue.Value;
                            switch (pid)
                            {
                                case BacnetPropertyIds.PROP_DESCRIPTION:
                                    {
                                        subNode.Prop_Description = bValue.ToString()?.Trim();
                                    }
                                    break;
                                case BacnetPropertyIds.PROP_OBJECT_NAME:
                                    {
                                        subNode.Prop_Object_Name = bValue.ToString()?.Trim();
                                    }
                                    break;
                                case BacnetPropertyIds.PROP_PRESENT_VALUE:
                                    {
                                        subNode.Prop_Present_Value = bValue.Value;
                                        subNode.Prop_DataType = DataTypeConversion(aRst.objectIdentifier.Type);
                                    }
                                    break;
                            }
                        }
                        ShwoText(string.Format("address:{0,-6} value:{2,-8}  type:{3,-8}  roll call:{1}\t describe:{4} ",
                            $"{subNode.ObjectId.Instance}_{(int)subNode.ObjectId.Type}",
                            subNode.Prop_Object_Name,
                            subNode.Prop_Present_Value,
                            subNode.Prop_DataType,
                            subNode.Prop_Description));

                        bacnetPropertyInfos.Add(new BacnetPropertyInfo()
                        {
                            IpAddress = $"{device.Address}:{device.DeviceId}",
                            Address = $"{subNode.ObjectId.Instance}_{(int)subNode.ObjectId.Type}",
                            DataType = subNode.Prop_DataType.ToString(),
                            Value = subNode.Prop_Present_Value.ToString(),
                            PropName = subNode.Prop_Object_Name,
                            Describe = subNode.Prop_Description,

                            ObjectType = aRst.objectIdentifier.Type.ToString(),
                            ReadWrite = aRst.objectIdentifier.Type == BacnetObjectTypes.OBJECT_ANALOG_INPUT ? "read only" : ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log("=== 【Err】" + ex.Message + " ===");
            }
        }

        private void LogEmpty()
        {
            BeginInvoke(new Action(() =>
            {
                txt_msgList.AppendText($"\r\n");
            }));
        }

        private void Log(string str)
        {
            BeginInvoke(new Action(() =>
            {
                txt_msgList.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}]:{str} \r\n");
            }));
        }

        private void ShwoText(string str)
        {
            BeginInvoke(new Action(() =>
            {
                txt_msgList.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {str} \r\n");
            }));
        }

        private async void Read_ClickAsync(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                Log("=== Please select the device you want to operate in the device list on the left ===");
                return;
            }
            var ipAddress = listBox1.SelectedItem.ToString().Split(' ')[0];
            var deviceId = listBox1.SelectedItem.ToString().Split(' ')[1];
            BacNode bacnet = devicesList.Where(t => t.Address.ToString() == ipAddress && t.DeviceId.ToString() == deviceId).FirstOrDefault();

            var address = txt_address.Text?.Trim();
            var addressPart = address.Split('_');
            BacProperty rpop = null;

            if (addressPart.Length == 1)
            {
                rpop = bacnet?.Properties.Where(t => t.Prop_Object_Name == address).FirstOrDefault();
                //bacnet = devicesList.Where(t => t.Properties.Any(p => p.PROP_OBJECT_NAME == address)).FirstOrDefault();
            }
            else if (addressPart.Length == 2)
            {
                rpop = bacnet?.Properties
                    .Where(t => t.ObjectId.Instance == uint.Parse(addressPart[0]) && t.ObjectId.Type == (BacnetObjectTypes)int.Parse(addressPart[1]))
                    .FirstOrDefault();
                //bacnet = devicesList
                //    .Where(t => t.Properties.Any(p => p.ObjectId.Instance == uint.Parse(addressPart[0]) && p.ObjectId.Type == (BacnetObjectTypes)int.Parse(addressPart[1])))
                //    .FirstOrDefault();
            }
            else
            {
                Log("Please enter correct address");
                return;
            }

            if (rpop == null)
            {
                Log("No corresponding point found");
                return;
            }
            int retry = 0;
            tag_retry:
            IList<BacnetValue> NoScalarValue = Bacnet_client.ReadPropertyRequest(bacnet.Address, rpop.ObjectId, BacnetPropertyIds.PROP_PRESENT_VALUE);
            if (NoScalarValue?.Any() ?? false)
            {
                await Task.Delay(retry * 200);
                try
                {
                    var value = NoScalarValue[0].Value;
                    ShwoText(string.Format("[read successfully][{3}] point:{0,-15} value:{1,-10} type:{2}",
                        address,
                        value?.ToString(),
                        rpop?.Prop_DataType.ToString(),
                        retry));
                }
                catch (Exception ex)
                {
                    Log($"=== 【Err】read failed.[{retry}]{ex.Message}" + " ===");
                }
            }
            else
            {
                retry++;
                if (retry < 4) goto tag_retry;
                Log($"=== 【Err】read failed[{retry - 1}]" + " ===");
            }
        }

        private async void Write_ClickAsync(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                Log("=== Please select the device you want to operate in the device list on the left ===");
                return;
            }
            var ipAddress = listBox1.SelectedItem.ToString().Split(' ')[0];
            var deviceId = listBox1.SelectedItem.ToString().Split(' ')[1];
            BacNode bacnet = devicesList.Where(t => t.Address.ToString() == ipAddress && t.DeviceId.ToString() == deviceId).FirstOrDefault();

            var address = txt_address.Text?.Trim();
            var value = txt_value.Text?.Trim();
            var addressPart = address.Split('_');

            BacProperty rpop = null;

            if (addressPart.Length == 1)
            {
                rpop = bacnet?.Properties.Where(t => t.Prop_Object_Name == address).FirstOrDefault();
                //bacnet = devicesList.Where(t => t.Properties.Any(p => p.PROP_OBJECT_NAME == address)).FirstOrDefault();
            }
            else if (addressPart.Length == 2)
            {
                rpop = bacnet?.Properties
                    .Where(t => t.ObjectId.Instance == uint.Parse(addressPart[0]) && t.ObjectId.Type == (BacnetObjectTypes)int.Parse(addressPart[1]))
                    .FirstOrDefault();
                //bacnet = devicesList
                //    .Where(t => t.Properties.Any(p => p.ObjectId.Instance == uint.Parse(addressPart[0]) && p.ObjectId.Type == (BacnetObjectTypes)int.Parse(addressPart[1])))
                //    .FirstOrDefault();
            }
            else
            {
                Log("Please enter correct address");
                return;
            }

            if (rpop == null)
            {
                Log("No corresponding point found");
                return;
            }

            var writePriority = uint.Parse(comboBox2.SelectedItem.ToString());
            Bacnet_client.WritePriority = writePriority;
            List<BacnetValue> NoScalarValue = new List<BacnetValue>() { new BacnetValue(value.ToDataFormType(rpop.Prop_DataType)) };
            
            if (rpop.Prop_DataType == DataTypeEnum.Bool && (rpop.Prop_Present_Value?.ToString() == "1" || rpop.Prop_Present_Value?.ToString() == "0"))
            {
                var tempValue = value == "1" || value.ToLower() == "true" ? 1 : 0;
                NoScalarValue = new List<BacnetValue>() { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, tempValue) };
            }

            int retry = 0;
            tag_retry:
            try
            {
                await Task.Delay(retry * 200);
                Bacnet_client.WritePropertyRequest(bacnet.Address, rpop.ObjectId, BacnetPropertyIds.PROP_PRESENT_VALUE, NoScalarValue);
                ShwoText(string.Format("[write success][{2}] point:{0,-15} value:{1,-10} priority[{3}]", address, value, retry, writePriority));
            }
            catch (Exception ex)
            {
                if (rpop.Prop_DataType == DataTypeEnum.Bool && ex.Message.EndsWith("ERROR_CODE_INVALID_DATA_TYPE"))
                {
                    var tempValue = value == "1" || value.ToLower() == "true" ? 1 : 0;
                    BacnetValue[] newNoScalarValue = { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, tempValue) };
                    Bacnet_client.WritePropertyRequest(bacnet.Address, rpop.ObjectId, BacnetPropertyIds.PROP_PRESENT_VALUE, newNoScalarValue);
                    ShwoText(string.Format("[write success][e] point:{0,-15} value:{1,-10} priority[{3}]", address, tempValue, retry, writePriority));
                }
                else
                {
                    retry++;
                    if (retry < 4) goto tag_retry;
                    Log($"write failed[{retry - 1}]:{ex.Message}");
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            new BACnetServer().Start(comboBox1.SelectedItem.ToString());
            button1_Click_1(null, null);
        }

        private DataTypeEnum DataTypeConversion(BacnetObjectTypes bacnetObjectType)
        {
            DataTypeEnum type;
            switch (bacnetObjectType)
            {
                case BacnetObjectTypes.OBJECT_ANALOG_INPUT:
                case BacnetObjectTypes.OBJECT_ANALOG_OUTPUT:
                case BacnetObjectTypes.OBJECT_ANALOG_VALUE:
                    type = DataTypeEnum.Float;
                    break;
                case BacnetObjectTypes.OBJECT_BINARY_INPUT:
                case BacnetObjectTypes.OBJECT_BINARY_OUTPUT:
                case BacnetObjectTypes.OBJECT_BINARY_VALUE:
                    type = DataTypeEnum.Bool;
                    break;
                case BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT:
                case BacnetObjectTypes.OBJECT_MULTI_STATE_OUTPUT:
                case BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE:
                    type = DataTypeEnum.UInt32;
                    break;
                case BacnetObjectTypes.OBJECT_CHARACTERSTRING_VALUE:
                    type = DataTypeEnum.String;
                    break;
                default:
                    type = DataTypeEnum.None;
                    break;
            }
            return type;
        }

        private void but_export_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            
            sfd.Title = "Please select a file path to save";
            
            sfd.Filter = "Excel file|*.xls";
            
            sfd.FileName = $"BACnet_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    bacnetPropertyInfos?.ToExcel(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void comboBox2_TextChanged(object sender, EventArgs e)
        {
            Bacnet_client.WritePriority = uint.Parse(comboBox2.SelectedItem.ToString());
        }

        private async void button5_ClickAsync(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                Log("=== Please select the device you want to operate in the device list on the left ===");
                return;
            }
            var ipAddress = listBox1.SelectedItem.ToString().Split(' ')[0];
            var deviceId = listBox1.SelectedItem.ToString().Split(' ')[1];
            BacNode bacnet = devicesList.Where(t => t.Address.ToString() == ipAddress && t.DeviceId.ToString() == deviceId).FirstOrDefault();
            if ((ModifierKeys & Keys.Control) == Keys.Control && (ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (MessageBox.Show($"Confirm that you want to release the device[{bacnet.Address}]？", "warn", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    foreach (var objectId in bacnet?.Properties.Select(t => t.ObjectId))
                    {
                        for (uint i = 1; i <= 16; i++)
                        {
                            await PresentValueAsync(bacnet.Address, objectId, i);
                        }
                    }
                }
                return;
            }

            var address = txt_address.Text?.Trim();
            var value = txt_value.Text?.Trim();
            var addressPart = address.Split('_');
            BacProperty rpop = null;
            if (addressPart.Length == 1)
            {
                rpop = bacnet?.Properties.Where(t => t.Prop_Object_Name == address).FirstOrDefault();
            }
            else if (addressPart.Length == 2)
            {
                rpop = bacnet?.Properties
                    .Where(t => t.ObjectId.Instance == uint.Parse(addressPart[0]) && t.ObjectId.Type == (BacnetObjectTypes)int.Parse(addressPart[1]))
                    .FirstOrDefault();
            }
            else
            {
                Log("Please enter correct address");
                return;
            }
            if (rpop == null)
            {
                Log("No corresponding point found");
                return;
            }

            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                for (uint i = 1; i <= 16; i++)
                {
                    await PresentValueAsync(bacnet.Address, rpop.ObjectId, i);
                }
            }
            else
            {
                var writePriority = uint.Parse(comboBox2.SelectedItem.ToString());
                await PresentValueAsync(bacnet.Address, rpop.ObjectId, writePriority);
            }
        }

        private async Task PresentValueAsync(BacnetAddress bacnetAddress, BacnetObjectId objectId, uint writePriority)
        {
            List<BacnetValue> nullValue = new List<BacnetValue>() { new BacnetValue(null) };
            string address = $"{objectId.Instance}_{(uint)objectId.Type}";
            int retry = 0;
            tag_retry:
            try
            {
                await Task.Delay(retry * 200);
                Bacnet_client.WritePriority = writePriority;
                Bacnet_client.WritePropertyRequest(bacnetAddress, objectId, BacnetPropertyIds.PROP_PRESENT_VALUE, nullValue);
                ShwoText(string.Format("[release successfully][{0}] point:{1,-15} priority[{2}]", retry, address, writePriority));
            }
            catch (Exception ex)
            {
                retry++;
                if (retry < 2) goto tag_retry;          
                ShwoText(string.Format(":[release failed][{0}] point:{1,-15} priority[{2}] {3}", retry, address, writePriority, ex.Message));
            }
        }
    }
}
