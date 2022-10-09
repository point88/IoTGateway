using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MQTTnet.Client.Options;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;

namespace IoTClient.Tool.Common
{
    public class MqttManager
    {
        private IManagedMqttClient mqttClient;
        private string clientID;

        public MqttManager()
        {
            clientID = Guid.NewGuid().ToString();
        }

        public void publish() { }

        public void subscribe() { }

        public async Task<string> start(string host, string port) {
            string result = "";
            var factory = new MqttFactory();
            mqttClient = factory.CreateManagedMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                             .WithClientId(this.clientID?.Trim())
                             .WithTcpServer(host?.Trim(), int.Parse(port?.Trim()));
            //.WithCredentials(txt_UserName.Text, txt_Password.Text);

            var options = new ManagedMqttClientOptionsBuilder()
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                        .WithClientOptions(mqttClientOptions.Build())
                        .Build();

            await mqttClient.StartAsync(options);

            mqttClient.UseDisconnectedHandler(ex =>
            {
                result += "### Server disconnected ###\n";
            });


            mqttClient.UseApplicationMessageReceivedHandler(ex =>
            {
                result += "### Received the news ###\n";
                result += $"+ Topic = {ex.ApplicationMessage.Topic}\n";
                try
                {
                    result += $"+ Payload = {Encoding.UTF8.GetString(ex.ApplicationMessage.Payload)}\n";
                }
                catch { }
                result += $"+ QoS = {ex.ApplicationMessage.QualityOfServiceLevel}\n";
                result += $"+ Retain = {ex.ApplicationMessage.Retain}\n";
            });

            mqttClient.UseConnectedHandler(ex =>
            {
                result += "### Connected to service ###\n";
            });
            return result;
        }

        public async void close() {
            if (mqttClient != null)
            {
                if (mqttClient.IsStarted)
                    await mqttClient.StopAsync();
                mqttClient.Dispose();
            }
        }


    }
}
