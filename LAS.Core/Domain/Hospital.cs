using System;

namespace UCLouvain.AmbulanceSystem.Core.Domain
{
	[PetaPoco.TableName ("hospitals")]
	[PetaPoco.PrimaryKey ("hospitalId", AutoIncrement = false)]
	public class Hospital
	{
		[PetaPoco.Column("hospitalId")]
		public string HospitalId { get; set; }

		[PetaPoco.Column("name")]
		public string Name { get; set; }

		[PetaPoco.Column("latitude")]
		public float Latitude {
			get;
			private set;
		}

		[PetaPoco.Column("longitude")]
		public float Longitude {
			get;
			private set;
		}

		[PetaPoco.Column("closed")]
		public bool Closed {
			get;
			set;
		}

		public Hospital()
		{

		}

		public Hospital(string identifier, string name, float latitude, float longitude)
		{
			Name = name;
			HospitalId = identifier;
			Latitude = latitude;
			Longitude = longitude;
		}
	}
}

