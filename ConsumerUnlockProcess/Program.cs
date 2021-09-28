using DownloadManningBook;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;

namespace ConsumerUnlockProcess
{
    class Program
    {
        private static readonly string rabbitmqUrl = Environment.GetEnvironmentVariable("RABBITMQ_URL");
        private static readonly string bookUrl = Environment.GetEnvironmentVariable("BOOK_URL");

        static void Main(string[] args)
        {
            if (bookUrl == null)
            {
                throw new Exception("Please input the bookName");
            }

            if (rabbitmqUrl == null)
            {
                throw new Exception("Please input the rabbitmqUrl");
            }
            Console.WriteLine($"Init rabbitmqUrl {rabbitmqUrl} ");

            Console.WriteLine($"Init book with Url {bookUrl}");
            string proxy = null;
            if (args.Length == 1)
            {
                proxy = args[0];
            }
            var client = new LiveBookApiClient(bookUrl, proxy);

            string queueName = "processing";
            var factory = new ConnectionFactory() { HostName = rabbitmqUrl };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var count = 0;
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var messg = JsonConvert.DeserializeObject<QueueMessage>(message);
                    if (File.Exists(messg.OutputPath))
                    {
                        channel.BasicAck(ea.DeliveryTag, false);
                        count++;
                        return;
                    }
                    try
                    {
                        var paragraph = client.Unlock(messg.ShortName, messg.ParagraphId);
                        Console.WriteLine($"Processing Consumer {messg.ShortName} -- {messg.ParagraphId} -- {count}");
                        SaveFileAsync(messg.OutputPath, paragraph);
                        channel.BasicAck(ea.DeliveryTag, false);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error {ex}");
                        channel.BasicNack(ea.DeliveryTag, false, true);
                        Environment.Exit(1);
                    }
                };
                channel.BasicConsume(queue: queueName,
                                     autoAck: false,
                                     consumer: consumer);

                Console.WriteLine($"Pause to another process consumer {Environment.MachineName}");
                while (true)
                {

                }
            }
        }

        static void SaveFileAsync(string filePath, string content)
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
