using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
namespace Broker
{
    internal class Program
    {
        public static Configuration config = new Configuration();
        public static MongoClient client = new MongoClient();
        public static IMongoDatabase db;
        public static IMongoCollection<MQTT> collection;
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.Unicode;
                Console.WriteLine("Đọc file cấu hình: Broker MQTT and MongoDB");
                using (StreamReader r = new StreamReader("Config.json"))
                {
                    string json = r.ReadToEnd();
                    config = Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(json);
                }
                Console.WriteLine("Create Broker MQTT!");
                Console.WriteLine("Port: {0}", config.Port);
                Console.WriteLine("Username: {0}", config.Username);
                Console.WriteLine("Password: {0}", config.Password);
                Console.WriteLine("Create Connection to MongoDB!");
                db = client.GetDatabase(config.Databases);
                collection = db.GetCollection<MQTT>(config.Collection);
                Console.WriteLine("URL: {0}", config.MongoDBUrl);
                Console.WriteLine("Databases: {0}", config.Databases);
                Console.WriteLine("Collection: {0}", config.Collection);

                var options = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(config.Port).WithEncryptionSslProtocol(System.Security.Authentication.SslProtocols.None);
                var server = new MqttFactory().CreateMqttServer(options.Build());
                server.ValidatingConnectionAsync += arg => {
                    if (arg.ClientId == "" || arg.ClientId == "null")
                    {
                        arg.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                    }
                    if (arg.Username != config.Username)
                    {
                        arg.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                    }

                    if (arg.Password != config.Password)
                    {
                        arg.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                    }

                    return Task.CompletedTask;
                };
                server.InterceptingPublishAsync += Server_InterceptingPublishAsync;
                server.StartAsync();
                Console.ReadLine();
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            async Task Server_InterceptingPublishAsync(InterceptingPublishEventArgs arg)
            {
                var payload = arg.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(arg.ApplicationMessage?.Payload);
                await updateMQTT(arg.ApplicationMessage?.Topic, payload);
                Console.WriteLine("Topic = {0}\t, Payload = {1}\t,Retain = {2}\t, Qos = {3}",
                arg.ApplicationMessage?.Topic,
                payload,
                arg.ApplicationMessage.Retain,
                arg.ApplicationMessage.QualityOfServiceLevel
                );
            }

        }

        public static async Task updateMQTT(string topic,string value)
        {
            try
            {
                var filter = Builders<MQTT>.Filter.Eq("topic", topic);
                var update = Builders<MQTT>.Update.Set(u => u.value, value);
                MQTT mqtt = new MQTT { topic = topic, value = value };
                var result = await (collection.FindAsync(filter).Result.ToListAsync());
                if(result.Count > 0)
                {
                    collection.UpdateOneAsync(filter, update);
                }
                else
                {
                    collection.InsertOneAsync(mqtt);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
