using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
	public class IncidentForm : Message 
	{
		public float Latitude {
			get;
			set;
		}

		public float Longitude {
			get;
			set;
		}

		public IncidentForm()
			: base()
		{
		}

		public IncidentForm(float latitude, float longitude)
			: base()
		{
			Latitude = latitude;
			Longitude = longitude;
		}

		public override string ToString()
		{
			return string.Format("[IncidentForm: Latitude={0}, Longitude={1}]", Latitude, Longitude);
		}
	}
}
