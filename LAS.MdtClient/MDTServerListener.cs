using System;
using NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using LAS.Core.Messages;
using System.Threading.Tasks;

namespace LAS.MdtClient
{
	public class MDTServerListener
	{
		string data;
		byte[] buffer;
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		static readonly Random r = new Random();

		public int Port { get; private set; }
		const int BUFFER_SIZE = 1024;

		MDT mdt;

		Thread threadHandle;

		public MDTServerListener(MDT mdt)
		{
			this.mdt = mdt;
			Port = r.Next (10000, 11000);
			threadHandle = new Thread(Run);
			threadHandle.Start();
		}

		void Run()
		{
			var ipHost = Dns.GetHostEntry(Dns.GetHostName());
			var ipAddress = ipHost.AddressList[0];
			var localEndPoint = new IPEndPoint(ipAddress, Port);

			var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			try {
				listener.Bind(localEndPoint);
				listener.Listen(10);

				while (true) {
					logger.Info("MDT Client listen to server...");
					var handler = listener.Accept();
					data = null;

					while (true) {
						buffer = new byte[BUFFER_SIZE];
						int bytesRec = handler.Receive(buffer);
						data += Encoding.ASCII.GetString(buffer, 0, bytesRec);
						if (data.IndexOf("<EOF>", StringComparison.Ordinal) > -1) {
							break;
						}
					}

					var message = Message.FromXML(data.Remove(data.Length - 5));

					var ack = new MessageAck(message.MessageId);
					var ack_str = Message.GetXML(ack) + "<EOF>";

					byte[] msg = Encoding.ASCII.GetBytes(ack_str);
					handler.Send(msg);

					handler.Shutdown(SocketShutdown.Both);
					handler.Close();

					if (message is MobilizationMessage) {
						mdt.Display((MobilizationMessage)message);
					} else if (message is MobilizationCancelled) {
						mdt.Display((MobilizationCancelled)message);
					} else {
						throw new NotImplementedException();
					}
				}

			} catch (Exception e) {
				logger.Error(e.ToString());
			}
		}

		void Process(Message m)
		{
			if (m is MobilizationMessage) {
				mdt.Display((MobilizationMessage) m);
			}
		}

	}
}
