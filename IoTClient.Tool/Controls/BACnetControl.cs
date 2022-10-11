using IoTClient.Common.Enums;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.BACnet;
using IoTClient.Tool.Model;
using Talk.Linq.Extensions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using IoTClient.Enums;

namespace IoTClient.Tool.Controls
{
    public partial class BACnetControl : UserControl
    {
        private static List<BacNode> devicesList = new List<BacNode>();
        private BacnetClient Bacnet_client;
        private List<BacnetPropertyInfo> bacnetPropertyInfos = new List<BacnetPropertyInfo>();
        private IManagedMqttClient mqttClient, mqttClient2, mqttClient3, mqttClient4, mqttClient5;
        private string clientID, clientID2 , clientID3 , clientID4 , clientID5;

        Thread registerThread1, registerThread2, registerThread3, registerThread4, registerThread5;
        string topic1, topic2, topic3, topic4, topic5;

        int priority_selectedIndex = 15;
        public BACnetControl()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            clientID = Guid.NewGuid().ToString();
            clientID2 = Guid.NewGuid().ToString();
            clientID3 = Guid.NewGuid().ToString();
            clientID4 = Guid.NewGuid().ToString();
            clientID5 = Guid.NewGuid().ToString();
            datatype_cb_1.SelectedIndex = 0;
            datatype_cb_2.SelectedIndex = 0;
            datatype_cb_3.SelectedIndex = 0;
            datatype_cb_4.SelectedIndex = 0;
            datatype_cb_5.SelectedIndex = 0;
            
            topic1 = topic2 = topic3 = topic4 = topic5 = "";
        }

        private void init_scan(object sender, EventArgs e)
        {
            server_connect.Text = "scanning...";
            server_connect.Enabled = false;
            device_cb.Enabled = false;
            devicesList = new List<BacNode>();
            device_cb.Items.Clear();
            Bacnet_client?.Dispose();
            Bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(47808, false));
            //Bacnet_client.WritePriority = 1;
            
            Bacnet_client.OnIam -= new BacnetClient.IamHandler(handler_OnIam);
            Bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);
            Bacnet_client.Start();
            Bacnet_client.WhoIs();

            
            Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(100);
                    AppendText($"waiting to scan...[{9 - i}]");
                }
                if (device_cb.Items.Count >= 1)
                    device_cb.SelectedIndex = 0;
                
                Scan();
                //server_connect.Enabled = true;
                server_connect.Text = "Connect";
                device_cb.Enabled = true;
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
                    device_cb.Items.Add(adr.ToString() + " " + deviceId);
                }
            }));
        }

        private void Scan()
        {
            bacnetPropertyInfos = new List<BacnetPropertyInfo>();
            AppendText("### Start scanning for devices... ###");
            foreach (var device in devicesList)
            {
                var deviceCount = GetDeviceArrayIndexCount(device) + 1;
                ScanPointsBatch(device, 20, deviceCount);
            }
            foreach (var device in devicesList)
            {
                AppendEmptyText();
                AppendText($"Start scanning properties,Address:{device.Address} DeviceId:{device.DeviceId}");
                ScanSubProperties(device);
                if (bacnetPropertyInfos.IsAny())
                    bacnetPropertyInfos.Add(new BacnetPropertyInfo());
            }
            AppendText("Scan complete");
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
                AppendText("=== 【Err】" + exp.Message + " ===");
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
                AppendText("=== 【Err】" + ex.Message + " ===");
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
                AppendText("=== 【Err】" + ex.Message + " ===");
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
                var batchNumber = 1;// (int)numericUpDown1.Value;
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
                            AppendText(string.Format("address:{0,-6} value:{2,-8}  type:{3,-8}  roll call:{1}\t describe:{4} ",
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
                AppendText("=== 【Err】" + ex.Message + " ===");
            }
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


        private async void mqtt_stop(object sender, EventArgs e)
        {
            if (mqttClient != null)
            {
                if (mqttClient.IsStarted)
                    await mqttClient.StopAsync();
                mqttClient.Dispose();
            }
            mqtt_connect.Enabled = true;
        }

        private async void write(string address, int datatype, string value)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                AppendText("Enter address");
                return;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                AppendText("Input value");
                return;
            }

            try
            {
                dynamic result = null;


                var ipAddress = device_cb.SelectedItem.ToString().Split(' ')[0];
                var deviceId = device_cb.SelectedItem.ToString().Split(' ')[1];
                BacNode bacnet = devicesList.Where(t => t.Address.ToString() == ipAddress && t.DeviceId.ToString() == deviceId).FirstOrDefault();

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
                    AppendText("Please enter correct address");
                    return;
                }

                if (rpop == null)
                {
                    AppendText("No corresponding point found");
                    return;
                }

                
                Bacnet_client.WritePriority = 15;
                BacnetValue NoScalarValue =  new BacnetValue(Convert.ToSingle(value)) ;

                if (rpop.Prop_DataType == DataTypeEnum.Bool && (rpop.Prop_Present_Value?.ToString() == "1" || rpop.Prop_Present_Value?.ToString() == "0"))
                {
                    var tempValue = value == "1" || value.ToLower() == "true" ? 1 : 0;
                    NoScalarValue = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, tempValue) ;
                }

                int retry = 0;
            tag_retry:
                try
                {
                    await Task.Delay(retry * 200);
                    Bacnet_client.WritePropertyRequest(bacnet.Address, rpop.ObjectId, BacnetPropertyIds.PROP_PRESENT_VALUE, NoScalarValue);
                    AppendText(string.Format("[write success][{2}] point:{0,-15} value:{1,-10} priority[{3}]", address, value, retry, 15));
                }
                catch (Exception ex)
                {
                    if (rpop.Prop_DataType == DataTypeEnum.Bool && ex.Message.EndsWith("ERROR_CODE_INVALID_DATA_TYPE"))
                    {
                        var tempValue = value == "1" || value.ToLower() == "true" ? 1 : 0;
                        BacnetValue[] newNoScalarValue = { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, tempValue) };
                        Bacnet_client.WritePropertyRequest(bacnet.Address, rpop.ObjectId, BacnetPropertyIds.PROP_PRESENT_VALUE, newNoScalarValue);
                        AppendText(string.Format("[write success][e] point:{0,-15} value:{1,-10} priority[{3}]", address, tempValue, retry, 15));
                    }
                    else
                    {
                        retry++;
                        if (retry < 4) goto tag_retry;
                        AppendText($"write failed[{retry - 1}]:{ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendText($"Write failed : {ex.Message}");
            }
        }
        private async void read(string address, int datatype, int register_index)
        {
            IManagedMqttClient client = null;
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                {
                    AppendText("Please enter address");
                    return;
                }
                dynamic result = null;
                float interval = 1;
                string topic = "";

                while (true)
                {
                    switch (register_index)
                    {
                        case 0:
                            client = mqttClient;
                            topic = mqtt_topic_box_1.Text?.Trim();
                            if (string.IsNullOrWhiteSpace(interval_1.Text))
                                interval = 1;
                            else
                                interval = float.Parse(interval_1.Text?.Trim());
                            break;
                        case 1:
                            client = mqttClient2;
                            topic = mqtt_topic_box_2.Text?.Trim();
                            if (string.IsNullOrWhiteSpace(interval_2.Text))
                                interval = 1;
                            else
                                interval = float.Parse(interval_2.Text?.Trim());
                            break;
                        case 2:
                            client = mqttClient3;
                            topic = mqtt_topic_box_3.Text?.Trim();
                            if (string.IsNullOrWhiteSpace(interval_3.Text))
                                interval = 1;
                            else
                                interval = float.Parse(interval_3.Text?.Trim());
                            break;
                        case 3:
                            client = mqttClient4;
                            topic = mqtt_topic_box_4.Text?.Trim();
                            if (string.IsNullOrWhiteSpace(interval_4.Text))
                                interval = 1;
                            else
                                interval = float.Parse(interval_4.Text?.Trim());
                            break;
                        case 4:
                            client = mqttClient5;
                            topic = mqtt_topic_box_5.Text?.Trim();
                            if (string.IsNullOrWhiteSpace(interval_5.Text))
                                interval = 1;
                            else
                                interval = float.Parse(interval_5.Text?.Trim());
                            break;
                    }



                    var ipAddress = device_cb.SelectedItem.ToString().Split(' ')[0];
                    var deviceId = device_cb.SelectedItem.ToString().Split(' ')[1];
                    BacNode bacnet = devicesList.Where(t => t.Address.ToString() == ipAddress && t.DeviceId.ToString() == deviceId).FirstOrDefault();

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
                        AppendText("Please enter correct address");
                        return;
                    }

                    if (rpop == null)
                    {
                        AppendText("No corresponding point found");
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
                            AppendText(string.Format("[read successfully][{3}] point:{0,-15} value:{1,-10} type:{2}",
                                address,
                                value?.ToString(),
                                rpop?.Prop_DataType.ToString(),
                                retry));
                        }
                        catch (Exception ex)
                        {
                            AppendText($"=== 【Err】read failed.[{retry}]{ex.Message}" + " ===");
                        }
                    }
                    else
                    {
                        retry++;
                        if (retry < 4) goto tag_retry;
                        AppendText($"=== 【Err】read failed[{retry - 1}]" + " ===");
                    }
                    result = NoScalarValue[0];

                    mqtt_async_publish(topic, ("" + result), client);
                    
                    Thread.Sleep((int)(1000 * interval));
                }
            }
            catch (Exception ex)
            {
                AppendText($"Read failed : {ex.Message}");
            }
        }
        private async void mqtt_async_publish(string topic, string payload, IManagedMqttClient client)
        {
            var result = await client.PublishAsync(topic, payload);
            AppendText($"topic:{topic} payload:{payload} {result.ReasonCode}");
        }
        private async void mqtt_async_subscribe(string address, int datatype, string topic, IManagedMqttClient client)
        {

            if (string.IsNullOrWhiteSpace(topic))
            {
                AppendText("### Please enter Topic ###");
                return;
            }

            await client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());

            AppendText("### Subscription ###");

            client.UseApplicationMessageReceivedHandler(ex => {
                AppendText("### Received the news ###");
                AppendText($"+ Topic = {ex.ApplicationMessage.Topic}");
                try
                {
                    write(address, datatype, Encoding.UTF8.GetString(ex.ApplicationMessage.Payload));
                    AppendText($"+ Payload = {Encoding.UTF8.GetString(ex.ApplicationMessage.Payload)}");
                }
                catch { }
                AppendText($"+ QoS = {ex.ApplicationMessage.QualityOfServiceLevel}");
                AppendText($"+ Retain = {ex.ApplicationMessage.Retain}");
            });

        }
        private async void mode_select_click_1(object sender, EventArgs e)
        {
            if (server_connect.Enabled == true || mqtt_connect.Enabled == true)
            {
                AppendText("### Please Connect Device and MQTT ###");
                return;
            }
            if (mode_cb_1.SelectedIndex == 0)
            {
                //Read
                interval_1.Enabled = true;
                await mqttClient.UnsubscribeAsync(new[] { topic1 });

                if (registerThread1 is null)
                {
                    registerThread1 = new Thread(() => read(address_box_1.Text?.Trim(), datatype_cb_1.SelectedIndex, 0));
                }
                else
                {
                    registerThread1.Abort();
                    registerThread1 = new Thread(() => read(address_box_1.Text?.Trim(), datatype_cb_1.SelectedIndex, 0));
                }
                registerThread1.Start();
            }
            else
            {
                //Write
                interval_1.Enabled = false;
                if (registerThread1 != null)
                {
                    registerThread1.Abort();
                }
                topic1 = mqtt_topic_box_1.Text?.Trim();
                mqtt_async_subscribe(address_box_1.Text?.Trim(), datatype_cb_1.SelectedIndex, mqtt_topic_box_1.Text?.Trim(), mqttClient);
            }
        }
        private async void mode_select_click_2(object sender, EventArgs e)
        {
            if (server_connect.Enabled == true || mqtt_connect.Enabled == true)
            {
                AppendText("### Please Connect Device and MQTT ###");
                return;
            }
            if (mode_cb_2.SelectedIndex == 0)
            {
                //Read
                interval_2.Enabled = true;
                await mqttClient2.UnsubscribeAsync(new[] { topic2 });

                if (registerThread2 is null)
                {
                    registerThread2 = new Thread(() => read(address_box_2.Text?.Trim(), datatype_cb_2.SelectedIndex, 1));
                }
                else
                {
                    registerThread2.Abort();
                    registerThread2 = new Thread(() => read(address_box_2.Text?.Trim(), datatype_cb_2.SelectedIndex, 1));
                }
                registerThread2.Start();
            }
            else
            {
                //Write
                interval_2.Enabled = false;
                if (registerThread2 != null)
                {
                    registerThread2.Abort();
                }
                topic2 = mqtt_topic_box_2.Text?.Trim();
                mqtt_async_subscribe(address_box_2.Text?.Trim(), datatype_cb_2.SelectedIndex, mqtt_topic_box_2.Text?.Trim(), mqttClient2);
            }
        }
        private async void mode_select_click_3(object sender, EventArgs e)
        {
            if (server_connect.Enabled == true || mqtt_connect.Enabled == true)
            {
                AppendText("### Please Connect Device and MQTT ###");
                return;
            }
            if (mode_cb_3.SelectedIndex == 0)
            {
                //Read
                interval_3.Enabled = true;
                await mqttClient3.UnsubscribeAsync(new[] { topic3 });

                if (registerThread3 is null)
                {
                    registerThread3 = new Thread(() => read(address_box_3.Text?.Trim(), datatype_cb_3.SelectedIndex, 2));
                }
                else
                {
                    registerThread3.Abort();
                    registerThread3 = new Thread(() => read(address_box_3.Text?.Trim(), datatype_cb_3.SelectedIndex, 2));
                }
                registerThread3.Start();
            }
            else
            {
                //Write
                interval_3.Enabled = false;
                if (registerThread3 != null)
                {
                    registerThread3.Abort();
                }
                topic3 = mqtt_topic_box_3.Text?.Trim();
                mqtt_async_subscribe(address_box_3.Text?.Trim(), datatype_cb_3.SelectedIndex, mqtt_topic_box_3.Text?.Trim(), mqttClient3);
            }
        }
        private async void mode_select_click_4(object sender, EventArgs e)
        {
            if (server_connect.Enabled == true || mqtt_connect.Enabled == true)
            {
                AppendText("### Please Connect Device and MQTT ###");
                return;
            }
            if (mode_cb_4.SelectedIndex == 0)
            {
                //Read
                interval_4.Enabled = true;
                await mqttClient4.UnsubscribeAsync(new[] { topic4 });

                if (registerThread4 is null)
                {
                    registerThread4 = new Thread(() => read(address_box_4.Text?.Trim(), datatype_cb_4.SelectedIndex, 3));
                }
                else
                {
                    registerThread4.Abort();
                    registerThread4 = new Thread(() => read(address_box_4.Text?.Trim(), datatype_cb_4.SelectedIndex, 3));
                }
                registerThread4.Start();
            }
            else
            {
                //Write
                interval_4.Enabled = false;
                if (registerThread4 != null)
                {
                    registerThread4.Abort();
                }
                topic4 = mqtt_topic_box_4.Text?.Trim();
                mqtt_async_subscribe(address_box_4.Text?.Trim(), datatype_cb_4.SelectedIndex, mqtt_topic_box_4.Text?.Trim(), mqttClient4);
            }
        }
        private async void mode_select_click_5(object sender, EventArgs e)
        {
            if (server_connect.Enabled == true || mqtt_connect.Enabled == true)
            {
                AppendText("### Please Connect Device and MQTT ###");
                return;
            }
            if (mode_cb_5.SelectedIndex == 0)
            {
                //Read
                interval_5.Enabled = true;
                await mqttClient5.UnsubscribeAsync(new[] { topic5 });

                if (registerThread5 is null)
                {
                    registerThread5 = new Thread(() => read(address_box_5.Text?.Trim(), datatype_cb_5.SelectedIndex, 4));
                }
                else
                {
                    registerThread5.Abort();
                    registerThread5 = new Thread(() => read(address_box_5.Text?.Trim(), datatype_cb_5.SelectedIndex, 4));
                }
                registerThread5.Start();
            }
            else
            {
                //Write
                interval_5.Enabled = false;
                if (registerThread5 != null)
                {
                    registerThread5.Abort();
                }
                topic5 = mqtt_topic_box_5.Text?.Trim();
                mqtt_async_subscribe(address_box_5.Text?.Trim(), datatype_cb_5.SelectedIndex, mqtt_topic_box_5.Text?.Trim(), mqttClient5);
            }
        }

        private async void but_mqtt_server_connect_click(object sender, EventArgs e)
        {
            try
            {
                mqtt_stop(null, null);
                var factory = new MqttFactory();
                mqttClient = factory.CreateManagedMqttClient();
                mqttClient2 = factory.CreateManagedMqttClient();
                mqttClient3 = factory.CreateManagedMqttClient();
                mqttClient4 = factory.CreateManagedMqttClient();
                mqttClient5 = factory.CreateManagedMqttClient();
                var mqttClientOptions = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim()));
                                 //.WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions2 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID2?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim()));
                                 //.WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions3 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID3?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim()));
                                 //.WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions4 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID4?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim()));
                                 //.WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions5 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID5?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim()));
                                 //.WithCredentials(txt_UserName.Text, txt_Password.Text);

                var options = new ManagedMqttClientOptionsBuilder()
                            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                            .WithClientOptions(mqttClientOptions.Build())
                            .Build();
                var options2 = new ManagedMqttClientOptionsBuilder()
                            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                            .WithClientOptions(mqttClientOptions2.Build())
                            .Build();
                var options3 = new ManagedMqttClientOptionsBuilder()
                            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                            .WithClientOptions(mqttClientOptions3.Build())
                            .Build();
                var options4 = new ManagedMqttClientOptionsBuilder()
                            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                            .WithClientOptions(mqttClientOptions4.Build())
                            .Build();
                var options5 = new ManagedMqttClientOptionsBuilder()
                            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                            .WithClientOptions(mqttClientOptions5.Build())
                            .Build();


                await mqttClient.StartAsync(options);
                await mqttClient2.StartAsync(options2);
                await mqttClient3.StartAsync(options3);
                await mqttClient4.StartAsync(options4);
                await mqttClient5.StartAsync(options5);

                mqttClient.UseDisconnectedHandler(ex =>
                {
                    AppendText("### Server disconnected ###");
                    mqtt_connect.Text = "Connect";
                    mqtt_connect.Enabled = true;
                }).UseConnectedHandler(ex =>
                {
                    AppendText("### Connected to service ###");
                    mqtt_connect.Text = "Connected";
                    mqtt_connect.Enabled = false;
                });

                mqttClient2.UseDisconnectedHandler(ex =>
                {
                    AppendText("### Server disconnected ###");
                    mqtt_connect.Text = "Connect";
                    mqtt_connect.Enabled = true;
                }).UseConnectedHandler(ex =>
                {
                    AppendText("### Connected to service ###");
                    mqtt_connect.Text = "Connected";
                    mqtt_connect.Enabled = false;
                });

                mqttClient3.UseDisconnectedHandler(ex =>
                {
                    AppendText("### Server disconnected ###");
                    mqtt_connect.Text = "Connect";
                    mqtt_connect.Enabled = true;
                }).UseConnectedHandler(ex =>
                {
                    AppendText("### Connected to service ###");
                    mqtt_connect.Text = "Connected";
                    mqtt_connect.Enabled = false;
                });

                mqttClient4.UseDisconnectedHandler(ex =>
                {
                    AppendText("### Server disconnected ###");
                    mqtt_connect.Text = "Connect";
                    mqtt_connect.Enabled = true;
                }).UseConnectedHandler(ex =>
                {
                    AppendText("### Connected to service ###");
                    mqtt_connect.Text = "Connected";
                    mqtt_connect.Enabled = false;
                });

                mqttClient5.UseDisconnectedHandler(ex =>
                {
                    AppendText("### Server disconnected ###");
                    mqtt_connect.Text = "Connect";
                    mqtt_connect.Enabled = true;
                }).UseConnectedHandler(ex =>
                {
                    AppendText("### Connected to service ###");
                    mqtt_connect.Text = "Connected";
                    mqtt_connect.Enabled = false;
                });
            }
            catch (Exception ex)
            {
                AppendText($"MQTT Server Connection Failed : {ex.Message}");
                mqtt_connect.Enabled = true;
                mqtt_connect.Text = "Connect";
            }
        }

        private void AppendText(string content)
        {
            txt_content.Invoke((Action)(() =>
            {
                txt_content.AppendText($"[{DateTime.Now.ToLongTimeString()}]{content}\r\n");
            }));
        }


        private void AppendEmptyText()
        {
            txt_content.Invoke((Action)(() =>
            {
                txt_content.AppendText($"\r\n");
            }));
        }

        private async void brokenline_Async(string address, int datatype)
        {

            try
            {
                var constant = new BrokenLineChart("LineChart-" + address);
                constant.Show();
                while (!constant.IsDisposed)
                {
                    dynamic result = null;


                    var ipAddress = device_cb.SelectedItem.ToString().Split(' ')[0];
                    var deviceId = device_cb.SelectedItem.ToString().Split(' ')[1];
                    BacNode bacnet = devicesList.Where(t => t.Address.ToString() == ipAddress && t.DeviceId.ToString() == deviceId).FirstOrDefault();

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
                        AppendText("Please enter correct address");
                        return;
                    }

                    if (rpop == null)
                    {
                        AppendText("No corresponding point found");
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
                            AppendText(string.Format("[read successfully][{3}] point:{0,-15} value:{1,-10} type:{2}",
                                address,
                                value?.ToString(),
                                rpop?.Prop_DataType.ToString(),
                                retry));
                        }
                        catch (Exception ex)
                        {
                            AppendText($"=== 【Err】read failed.[{retry}]{ex.Message}" + " ===");
                        }
                    }
                    else
                    {
                        retry++;
                        if (retry < 4) goto tag_retry;
                        AppendText($"=== 【Err】read failed[{retry - 1}]" + " ===");
                    }
                    result = NoScalarValue[0];


                    constant.AddData(result.Value);

                    await Task.Delay(800);
                }
            }
            catch (Exception ex)
            {
                AppendText($"Line chart update failed");
            }
        }

        private void linechart_show_1(object sender, EventArgs e)
        {
            brokenline_Async(address_box_1.Text, datatype_cb_1.SelectedIndex);
        }
        private void linechart_show_2(object sender, EventArgs e)
        {
            brokenline_Async(address_box_2.Text, datatype_cb_2.SelectedIndex);
        }
        private void linechart_show_3(object sender, EventArgs e)
        {
            brokenline_Async(address_box_3.Text, datatype_cb_3.SelectedIndex);
        }
        private void linechart_show_4(object sender, EventArgs e)
        {
            brokenline_Async(address_box_4.Text, datatype_cb_4.SelectedIndex);
        }
        private void linechart_show_5(object sender, EventArgs e)
        {
            brokenline_Async(address_box_5.Text, datatype_cb_5.SelectedIndex);
        }
    }
}
