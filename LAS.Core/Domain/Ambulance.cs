using System;
using PetaPoco;
using System.Globalization;
using NLog;

namespace LAS.Core.Domain
{
	[PetaPoco.TableName("ambulances")]
	[PetaPoco.PrimaryKey("ambulanceId", AutoIncrement = false)]
	public class Ambulance
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		[PetaPoco.Column("ambulanceId")]
		public string AmbulanceId {
			get;
			set;
		}

		[PetaPoco.Column("longitude")]
		public float Longitude {
			get;
			set;
		}

		[PetaPoco.Column("latitude")]
		public float Latitude {
			get;
			set;
		}

		[PetaPoco.Column("lastPositionUpdate")]
		public DateTime LastPositionUpdate {
			get;
			set;
		}

		[PetaPoco.Column("status")]
		public AmbulanceStatus Status {
			get;
			set;
		}

		[PetaPoco.Column("port")]
		public string Port {
			get;
			set;
		}

		[Column("inTrafficJam")]
		public bool InTrafficJam {
			get;
			set;
		}

		public Ambulance()
		{
			
		}

		public Ambulance(string identifier)
		{
			AmbulanceId = identifier;
		}

		public void SetPosition(float latitude, float longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
			LastPositionUpdate = DateTime.Now;
		}

		public void SetPort(string listeningQueue)
		{
			Port = listeningQueue;
		}

		public void SetStatus(AmbulanceStatus status)
		{
			Status = status;
		}
	}
}

