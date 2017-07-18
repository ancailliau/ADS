using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
	public class RegisterAmbulanceStation : Message
	{
		public string Identifier {
			get;
			set;
		}
        
        public string Name {
            get;
            set;
        }

		public string PrinterId {
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

		public RegisterAmbulanceStation()
			: base()
		{
		}

		public RegisterAmbulanceStation(string identifier, string name, string printer_id,
		                               float latitude, float longitude)
			: base ()
		{
			Identifier = identifier;
            Name = name;
			PrinterId = printer_id;
			Latitude = latitude;
			Longitude = longitude;
		}
        
        public override string ToString()
        {
            return string.Format("[RegisterAmbulanceStation: Identifier={0}, Name={1}, PrinterId={2}, Latitude={3}, Longitude={4}]", Identifier, Name, PrinterId, Latitude, Longitude);
        }
	}
}
