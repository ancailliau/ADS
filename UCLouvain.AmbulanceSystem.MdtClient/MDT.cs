﻿using System;
using System.Collections.Generic;
using System.Threading;
using UCLouvain.AmbulanceSystem.Core.Domain;
using UCLouvain.AmbulanceSystem.Core.Messages;
using UCLouvain.AmbulanceSystem.Core.Utils;
using NLog;

namespace UCLouvain.AmbulanceSystem.MDTClient
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
        //MDTServerListener listener;
        MDTServerRabbitMQListener listener;
		RabbitMQMessageSender sender;

		public MDT(string identifier, float latitude, float longitude, Func<bool> sendPosition)
		{
			this.ambulanceId = identifier;
			this.latitude = latitude;
			this.longitude = longitude;

			lockPosition = new object();
			sender = new RabbitMQMessageSender();
			Messages = new Stack<Message>();

			avls = new AVLS(this, sendPosition);
			listener = new MDTServerRabbitMQListener("mdt_" + ambulanceId, this);
            var ts = new ThreadStart(listener.Run);
            var t = new Thread(ts);
            t.Start();

			Register();
		}

		void Register()
		{
			var message = new RegisterAmbulanceMessage(ambulanceId, 
			                                           "mdt_" + ambulanceId,
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

        public void AcceptMobilization(int allocationId)
        {
            var message = new MobilizationConfirmation(ambulanceId, allocationId);
            Send(message);
        }

        public void ConfirmCancellation (int incidentIdentifier)
        {
            var message = new CancellationConfirmation (ambulanceId, incidentIdentifier);
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
			sender.Send(m, "internal_comm_queue");
		}
	}
}
