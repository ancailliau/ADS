using System;
using System.Text;
using UCLouvain.AmbulanceSystem.Core.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;

namespace UCLouvain.AmbulanceSystem.Server
{
    public class RabbitMQListeningServer
    {
        string _queue_name;

        bool _stop = false;

        MessageProcessor _processor;

        public RabbitMQListeningServer(string queue_name, MessageProcessor processor)
        {
            _queue_name = queue_name;
            _processor = processor;
        }

        public void Stop()
        {
            _stop = true;
        }

        public void Run()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: _queue_name,
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var text_message = Encoding.UTF8.GetString(body);
                        var message = Message.FromXML(text_message);
                        _processor.AddToProcessingQueue(message);
                    };
                    
                    channel.BasicConsume(queue: _queue_name,
                                         noAck: true,
                                         consumer: consumer);

                    while (!_stop)
                    {
                        Thread.Sleep(100);
                    }

                }
            }
        }
    }
}
