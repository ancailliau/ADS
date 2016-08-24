using System;
using NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LAS.Core.Messages;
using System.Threading;
using LAS.Server;

namespace LAS
{
	public class TCPListeningServer
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		bool Running = false;

		const int BUFFER_SIZE = 1024;

		int listening_port;

		byte[] buffer;
		string data;

		Orchestrator p;

		MessageProcessor processor;

		public TCPListeningServer(Orchestrator p, int port = 9999)
		{
			listening_port = port;
			this.p = p;
			processor = new MessageProcessor(p);
		}

		public void Run()
		{
			Running = true;

			var ipHost = Dns.GetHostEntry(Dns.GetHostName());
			var ipAddress = ipHost.AddressList[0];
			var localEndPoint = new IPEndPoint(ipAddress, listening_port);

			var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			try {
				listener.Bind(localEndPoint);
				listener.Listen(10);

				while (Running) {
					// logger.Info("Waiting for a message...");
					var handler = listener.Accept();
					data = null;

					while (true) {
						buffer = new byte [BUFFER_SIZE];
						int bytesRec = handler.Receive(buffer);
						data += Encoding.ASCII.GetString(buffer, 0, bytesRec);
						if (data.IndexOf("<EOF>", StringComparison.Ordinal) > -1) {
							break;
						}
					}

					//logger.Info("Text received: " + data.Remove(data.Length - 5));

					var message = Message.FromXML(data.Remove (data.Length - 5));
					// logger.Info("Message type: " + message.GetType ());
					// logger.Info("Message ID: " + message.MessageId);
					// logger.Info("Message {0} received ({1})", message.MessageId, message.GetType ());

					var ack = new MessageAck(message.MessageId);
					var ack_str = Message.GetXML(ack) + "<EOF>";

					byte[] msg = Encoding.ASCII.GetBytes(ack_str);
					handler.Send(msg);
						
					processor.AddToProcessingQueue(message);

					handler.Shutdown(SocketShutdown.Both);
					handler.Close();
				}

			} catch (Exception e) {
				logger.Error (e.ToString());
			}
		}

		public void Stop()
		{
			Running = false;
		}

	}
}
