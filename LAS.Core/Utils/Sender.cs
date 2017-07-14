using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UCLouvain.AmbulanceSystem.Core.Messages;
using NLog;

namespace UCLouvain.AmbulanceSystem.Core.Utils
{
	public class StateObject
	{
		public Socket workSocket = null;
		public const int BufferSize = 1024;
		public byte[] buffer = new byte[BufferSize];
		public StringBuilder sb = new StringBuilder();
	}

	public class MessageSender
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		ManualResetEvent connectDone = new ManualResetEvent(false);
		ManualResetEvent sendDone 	= new ManualResetEvent(false);
		ManualResetEvent receiveDone = new ManualResetEvent(false);

		bool connected = false;

		String response = string.Empty;

		object lockHandle;

		public MessageSender()
		{
			lockHandle = new object();
		}

		public bool Send (Message message, int port = 9999, Action sentCallback = null, Action confirmationCallback = null)
		{
			lock (lockHandle) {
				connected = false;
				connectDone = new ManualResetEvent(false);
				sendDone = new ManualResetEvent(false);
				receiveDone = new ManualResetEvent(false);
				response = string.Empty;

				try {
					var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
					var ipAddress = ipHostInfo.AddressList[0];
					var remoteEP = new IPEndPoint(ipAddress, port);

					var sender = new Socket(AddressFamily.InterNetwork,
						SocketType.Stream, ProtocolType.Tcp);

					sender.BeginConnect(remoteEP,
					new AsyncCallback(ConnectCallback), sender);
					connectDone.WaitOne();
					if (!connected) {
						return false;
					}

					Send(sender, Message.GetXML(message));
					sendDone.WaitOne();
					if (!connected) {
						return false;
					}
					if (sentCallback != null) {
						sentCallback();
					}

					//logger.Info("Wait for ACK");

					Receive(sender);
					receiveDone.WaitOne();
					if (!connected) {
						return false;
					}
					if (confirmationCallback != null) {
						confirmationCallback();
					}

					//logger.Info("Received: " + response);

					//sender.Shutdown(SocketShutdown.Both);
					sender.Close();
					return true;

				} catch (Exception e) {
					logger.Info("Caught exception while sending message " + message.ToString ());
					logger.Error(e.Message);
					logger.Error(e.StackTrace);

					return false;
				}
			}
		}

		void ConnectCallback(IAsyncResult ar)
		{
			try {
				var client = (Socket)ar.AsyncState;
				client.EndConnect(ar);
				connected = true;
				//logger.Info("Socket connected to {0}", client.RemoteEndPoint);

			} catch (Exception e) {
				logger.Info("error in ConnectCallback: " + e.Message);
				connected = false;

			} finally {
				connectDone.Set();
			}
		}

		void Send(Socket client, string data)
		{
			byte[] byteData = Encoding.ASCII.GetBytes(data + "<EOF>");
			client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
		}

		void SendCallback(IAsyncResult ar)
		{
			try {
				var client = (Socket)ar.AsyncState;
				int bytesSent = client.EndSend(ar);
				//logger.Info("Sent {0} bytes to server.", bytesSent);

			} catch (Exception e) {
				logger.Error(e.ToString());
				connected = false;
			} finally {
				sendDone.Set();
			}
		}

		void Receive(Socket client)
		{
			try {
				var state = new StateObject();
				state.workSocket = client;
				client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

			} catch (Exception e) {
				logger.Error(e.ToString());
				connected = false;
			}
		}

		void ReceiveCallback(IAsyncResult ar)
		{
			try {
				var state = (StateObject)ar.AsyncState;
				var client = state.workSocket;

				int bytesRead = client.EndReceive(ar);

				if (bytesRead > 0) {
					state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
					client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

				} else {
					if (state.sb.Length > 1) {
						response = state.sb.ToString();
					}
					receiveDone.Set();
				}
			} catch (Exception e) {
				logger.Error (e.ToString());
				connected = false;
				receiveDone.Set();
			}
		}
	}
}
