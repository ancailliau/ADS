using System;
using System.Text;
using LAS.Core.Messages;
using RabbitMQ.Client;

namespace LAS.Core.Utils
{
    public class RabbitMQMessageSender
    {
        ConnectionFactory factory;
    
        public RabbitMQMessageSender()
        {
            factory = new ConnectionFactory() { HostName = "localhost" };
        }

        public bool Send(Message message, string queue_name)
        {
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue_name,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(Message.GetXML(message));

                channel.BasicPublish(exchange: "",
                                     routingKey: queue_name,
                                     basicProperties: null,
                                     body: body);
            }

            return true;
        }
    }
}
