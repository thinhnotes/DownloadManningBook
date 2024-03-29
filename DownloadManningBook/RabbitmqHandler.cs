﻿using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadManningBook
{
    internal class RabbitmqHandler
    {
        private readonly ConnectionFactory factory;
        private readonly string queueName;

        public RabbitmqHandler(string hostName, string queueName)
        {
            factory = new ConnectionFactory() { HostName = hostName };
            this.queueName = queueName;
        }

        public void Publish(string message)
        {
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: body);
        }

        public void Consumer(LiveBookApiClient client)
        {
            using var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var messg = JsonConvert.DeserializeObject<QueueMessage>(message);
                    try
                    {
                        var paragraph = client.Unlock(messg.ShortName, messg.ParagraphId);
                        Console.WriteLine($"Processing Consumer {messg.ShortName} -- {messg.ParagraphId}");
                        SaveFileAsync(messg.OutputPath, paragraph);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error ex");
                        throw ex;
                    }

                };

                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
            }
        }

        void SaveFileAsync(string filePath, string content)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(filePath, content);
        }
    }
}
