using System;
namespace LAS.Core.Messages
{
	public class RegisterAmbulanceMessage : Message
	{
		public string Identifier {
			get;
			set;
		}

		public int ListeningPort {
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

		public RegisterAmbulanceMessage()
			: base()
		{
		}

		public RegisterAmbulanceMessage(string identifier, int port,
		                               float latitude, float longitude)
			: base ()
		{
			Identifier = identifier;
			ListeningPort = port;
			Latitude = latitude;
			Longitude = longitude;
		}

		public override string ToString()
		{
			return string.Format("[RegisterAmbulanceMessage: Identifier={0}, ListeningPort={1}, Latitude={2}, Longitude={3}]", Identifier, ListeningPort, Latitude, Longitude);
		}
	}
}
