using System;
using System.Collections.Generic;
using System.Threading;
using LAS.Core.Domain;
using LAS.Core.Messages;
using LAS.Core.Utils;
using NLog;

namespace LAS.MdtClient
{
	public class MDT
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public Stack<Message> Messages {
			get;
			set;
		}	

		public string ambulanceId {
			get;
			private set;
		}

		public float latitude { get; private set; }
		public float longitude { get; private set; }

		public object lockPosition { get; private set; }

		public delegate void OnMobilized(object sender, EventArgs e);
		public event OnMobilized Mobilized;

		public delegate void OnMobilizationCancelled(object sender, EventArgs e);
		public event OnMobilizationCancelled MobilizationCancelled;

		AVLS avls;
		MDTServerListener listener;
		MessageSender sender;

		public MDT(string identifier, float latitude, float longitude, Func<bool> sendPosition)
		{
			this.ambulanceId = identifier;
			this.latitude = latitude;
			this.longitude = longitude;

			lockPosition = new object();
			sender = new MessageSender();
			Messages = new Stack<Message>();

			avls = new AVLS(this, sendPosition);
			listener = new MDTServerListener(this);

			Register();
		}

		void Register()
		{
			var message = new RegisterAmbulanceMessage(ambulanceId, 
			                                           listener.Port,
			                                           latitude,
			                                           longitude);
			Send(message);
		}

		public void SetPosition(float latitude, float longitude)
		{
			lock (lockPosition) {
				this.latitude = latitude;
				this.longitude = longitude;
			}
		}

		public void SetLeaving()
		{
			var message = new AmbulanceStatusMessage(ambulanceId, AmbulanceStatus.Leaving);
			Send(message);
		}

		public void SetOnScene()
		{
			var message = new AmbulanceStatusMessage(ambulanceId, AmbulanceStatus.OnScene);
			Send(message);
		}

		public void SetToHospital()
		{
			var message = new AmbulanceStatusMessage(ambulanceId, AmbulanceStatus.ToHospital);
			Send(message);
		}

		public void SetAtHospital()
		{
			var message = new AmbulanceStatusMessage(ambulanceId, AmbulanceStatus.AtHospital);
			Send(message);
		}

		public void SetAvailableRadio()
		{
			var message = new AmbulanceStatusMessage(ambulanceId, AmbulanceStatus.AvailableRadio);
			Send(message);
		}

		public void SetAvailableAtStation()
		{
			var message = new AmbulanceStatusMessage(ambulanceId, AmbulanceStatus.AvailableAtStation);
			Send(message);
		}

		public void SetUnavailable()
		{
			var message = new AmbulanceStatusMessage(ambulanceId, AmbulanceStatus.Unavailable);
			Send(message);
		}

		public void RefuseMobilization(int incidentIdentifier)
		{
			var message = new MobilizationOrderRefusal(ambulanceId, incidentIdentifier);
			Send(message);
		}

		public void InTrafficJam(bool inTrafficJam)
		{
			Message message;
			if (inTrafficJam) {
				message = new InTrafficJamMessage(ambulanceId);
			} else {
				message = new NotInTrafficJamMessage(ambulanceId);
			}
			Send(message);
		}

		public void DestinationUnreachable(int allocationId)
		{
			Send(new DestinationUnreachableMessage(allocationId));
		}

		public void Display(MobilizationMessage m)
		{
			logger.Info("Display message: " + m);
			Messages.Push(m);
			if (Mobilized != null)
				Mobilized(this, EventArgs.Empty);
		}

		public void Display(MobilizationCancelled m)
		{
			logger.Info("Display message: " + m);
			Messages.Push(m);
			if (MobilizationCancelled != null)
				MobilizationCancelled(this, EventArgs.Empty);
		}

		internal void Send(Message m)
		{
			logger.Info("Sending ({0}): {1}", ambulanceId, m.ToString ());
			sender.Send(m);
		}
	}
}
