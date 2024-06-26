﻿using IoTClient.Clients.Modbus;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client;
using MQTTnet;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace IoTClient.Tool.Controls
{
    public partial class ModbusTcpControl : UserControl
    {
        private IModbusClient modbus_client;
        private IManagedMqttClient mqttClient, mqttClient2, mqttClient3, mqttClient4, mqttClient5;
        private string clientID, clientID2, clientID3, clientID4, clientID5;
        Thread registerThread1, registerThread2, registerThread3, registerThread4, registerThread5;
        string topic1, topic2, topic3, topic4, topic5;

        public ModbusTcpControl()
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

            server_connect.Enabled = true;
            server_disconnect.Enabled = false;

            mqtt_connect.Enabled = true;
            mqtt_disconnect.Enabled = false;

            topic1 = topic2 = topic3 = topic4 = topic5 = "";
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

        private void write(string address, int datatype, string value)
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

                switch (datatype)
                {
                    case 0:
                        if (value?.Trim() == "0")
                            result = modbus_client.Write(address, false);
                        else if (value?.Trim() == "1")
                            result = modbus_client.Write(address, true);
                        break;
                    case 1:
                        result = modbus_client.Write(address, byte.Parse(value?.Trim()));
                        break;
                    case 2:
                        result = modbus_client.Write(address, short.Parse(value?.Trim()));
                        break;
                    case 3:
                        result = modbus_client.Write(address, ushort.Parse(value?.Trim()));
                        break;
                    case 4:
                        result = modbus_client.Write(address, int.Parse(value?.Trim()));
                        break;
                    case 5:
                        result = modbus_client.Write(address, uint.Parse(value?.Trim()));
                        break;
                    case 6:
                        result = modbus_client.Write(address, long.Parse(value?.Trim()));
                        break;
                    case 7:
                        result = modbus_client.Write(address, ulong.Parse(value?.Trim()));
                        break;
                    case 8:
                        result = modbus_client.Write(address, float.Parse(value?.Trim()));
                        break;
                    case 9:
                        result = modbus_client.Write(address, double.Parse(value?.Trim()));
                        break;
                }

                if (result.IsSucceed)
                    AppendText($"[write {address?.Trim()} success]：{value?.Trim()} OK");
                else
                    AppendText($"[write {address?.Trim()} faile]");

            }
            catch (Exception ex)
            {
                AppendText($"Write failed : {ex.Message}");
            }
        }
        private void read(string address, int datatype, int register_index)
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

                    switch (datatype)
                    {
                        case 0:
                            result = modbus_client.ReadCoil(address);
                            break;
                        case 1:
                            result = modbus_client.ReadDiscrete(address);
                            break;
                        case 2:
                            result = modbus_client.ReadInt16(address);
                            break;
                        case 3:
                            result = modbus_client.ReadUInt16(address);
                            break;
                        case 4:
                            result = modbus_client.ReadInt32(address);
                            break;
                        case 5:
                            result = modbus_client.ReadUInt32(address);
                            break;
                        case 6:
                            result = modbus_client.ReadInt64(address);
                            break;
                        case 7:
                            result = modbus_client.ReadUInt64(address);
                            break;
                        case 8:
                            result = modbus_client.ReadFloat(address);
                            break;
                        case 9:
                            result = modbus_client.ReadDouble(address);
                            break;
                    }

                    if (result.IsSucceed)
                    {
                        AppendText($"[read {address?.Trim()} success]：{result.Value}");
                        mqtt_async_publish(topic, ("" + result.Value), client);
                    }
                    else
                    {
                        AppendText($"[read {address?.Trim()} failed]");
                    }
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
            } else {
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

        private async  void but_mqtt_server_disconnect_click(object sender, EventArgs e) {
            mqtt_connect.Text = "Connect";
            mqtt_connect.Enabled = true;
            mqtt_disconnect.Enabled = false;

            await mqttClient.StopAsync();
            await mqttClient2.StopAsync();
            await mqttClient3.StopAsync();
            await mqttClient4.StopAsync();
            await mqttClient5.StopAsync();
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
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim())).WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions2 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID2?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim())).WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions3 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID3?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim())).WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions4 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID4?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim())).WithCredentials(txt_UserName.Text, txt_Password.Text);
                var mqttClientOptions5 = new MqttClientOptionsBuilder()
                                 .WithClientId(this.clientID5?.Trim())
                                 .WithTcpServer(mqtt_host_box.Text?.Trim(), int.Parse(mqtt_port_box.Text?.Trim())).WithCredentials(txt_UserName.Text, txt_Password.Text);

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
                    mqtt_disconnect.Enabled = true;
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
                    mqtt_disconnect.Enabled = true;
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
                    mqtt_disconnect.Enabled = true;
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
                    mqtt_disconnect.Enabled = true;
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
                    mqtt_disconnect.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                AppendText($"MQTT Server Connection Failed : {ex.Message}");
                mqtt_connect.Enabled = true;
                mqtt_connect.Text = "Connect";
            }
        }

        private void but_server_disconnect_click(object sender, EventArgs e) {
            modbus_client?.Close();
            server_connect.Text = "Connect";
            server_connect.Enabled = true;
            server_disconnect.Enabled = false;
            AppendText($"Fin Server Connection Closed");
        }
        private void but_server_connect_click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    server_connect.Text = "Connecting...";
                    server_connect.Enabled = false;
                    modbus_client?.Close();
                    modbus_client = new ModbusTcpClient(ip_address_box.Text?.Trim(), int.Parse(port_box.Text?.Trim()));

                    var result = modbus_client.Open();

                    if (!result.IsSucceed)
                    {
                        AppendText($"Server Open Failed : Server is not running or network problem.");
                        server_connect.Text = "Connect";
                        server_connect.Enabled = true;
                    }
                    else
                    {
                        AppendText($"Connection Success \t\t\t\t time ：{result.TimeConsuming}ms");
                        server_connect.Text = "Connected";
                        server_disconnect.Enabled = true;
                    }
                    
                }
                catch (Exception ex)
                {
                    AppendText($"Connection Failed : {ex.Message}");
                    server_connect.Text = "Connect";
                    server_connect.Enabled = true;
                    server_disconnect.Enabled = false;
                }
            });
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

                    switch (datatype)
                    {
                        case 0:
                            result = modbus_client.ReadCoil(address);
                            break;
                        case 1:
                            result = modbus_client.ReadDiscrete(address);
                            break;
                        case 2:
                            result = modbus_client.ReadInt16(address);
                            break;
                        case 3:
                            result = modbus_client.ReadUInt16(address);
                            break;
                        case 4:
                            result = modbus_client.ReadInt32(address);
                            break;
                        case 5:
                            result = modbus_client.ReadUInt32(address);
                            break;
                        case 6:
                            result = modbus_client.ReadInt64(address);
                            break;
                        case 7:
                            result = modbus_client.ReadUInt64(address);
                            break;
                        case 8:
                            result = modbus_client.ReadFloat(address);
                            break;
                        case 9:
                            result = modbus_client.ReadDouble(address);
                            break;
                    }

                    if (result.IsSucceed)
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
