using System;
using System.Threading;
using LAS.Core.Messages;

namespace LAS.MdtClient
{
	public class AVLS
	{

		/// <summary>
		/// The delay (in ms) between two position update messages sent to server. 
		/// </summary>
		static readonly TimeSpan AVLS_DELAY = TimeSpan.FromSeconds(6);

		Thread threadHandle;
		MDT mdt;

		Func<bool> testSend;

		public AVLS(MDT mdt, Func<bool> sendPosition)
		{
			this.mdt = mdt;
			testSend = sendPosition;
			threadHandle = new Thread(SendPosition);
			threadHandle.Start();
		}

		void SendPosition()
		{
			Thread.Sleep(AVLS_DELAY);
			while (true) {
				if (testSend()) {
					PositionMessage positionUpdate;
					lock (mdt.lockPosition) {
						positionUpdate = new PositionMessage(mdt.ambulanceId,
															 mdt.latitude,
															 mdt.longitude);
					}
					mdt.Send(positionUpdate);
				}

				Thread.Sleep(AVLS_DELAY);
			}
		}
	}
}
