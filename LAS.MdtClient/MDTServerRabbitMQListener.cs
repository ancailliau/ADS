using System;
using System.Text;
using System.Threading;
using LAS.Core.Messages;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LAS.MdtClient
{
    public class MDTServerRabbitMQListener
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        string _queue_name;

        bool _stop = false;

        MDT _mdt;

        public MDTServerRabbitMQListener(string queue_name, MDT mdt)
        {
            _queue_name = queue_name;
            _mdt = mdt;
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
                        logger.Info("Received: " + text_message);
                        var message = Message.FromXML(text_message);

                        if (message is MobilizationMessage)
                        {
                            _mdt.Display((MobilizationMessage)message);
                        }
                        else if (message is MobilizationCancelled)
                        {
                            _mdt.Display((MobilizationCancelled)message);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }

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
