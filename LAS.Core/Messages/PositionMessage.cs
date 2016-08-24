using System;
namespace LAS.Core.Messages
{
	public class PositionMessage : Message
	{
		public string AmbulanceIdentifier {
			get;
			set;
		}

		public float Latitude {
			get;
			set;
		}

		public float Longitude {
			get;
			set;
		}

		public PositionMessage()
		{

		}

		public PositionMessage(string ambulanceIdentifier, float latitude, float longitude) : base()
		{
			AmbulanceIdentifier = ambulanceIdentifier;
			Latitude = latitude;
			Longitude = longitude;
		}

		public override string ToString()
		{
			return string.Format("[PositionMessage: AmbulanceIdentifier={0}, Latitude={1}, Longitude={2}]", AmbulanceIdentifier, Latitude, Longitude);
		}
	}
}
