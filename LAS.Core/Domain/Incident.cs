using System;
namespace LAS.Core.Domain
{
	[PetaPoco.TableName ("incidents")]
	[PetaPoco.PrimaryKey ("incidentId", AutoIncrement = true)]
	public class Incident
	{
		[PetaPoco.Column ("incidentId")]
		public int IncidentId {
			get;
			set;
		}

		[PetaPoco.Column("latitude")]
		public float Latitude {
			get;
			set;
		}

		[PetaPoco.Column("longitude")]
		public float Longitude {
			get;
			set;
		}

		[PetaPoco.Column("resolved")]
		public bool Resolved {
			get;
			set;
		}

		[PetaPoco.Column("unreachable")]
		public bool Unreachable {
			get;
			set;
		}

		public Incident()
		{
		}

		public Incident(float latitude, float longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
		}

		public override string ToString()
		{
			return string.Format("[Incident: IncidentId={0}, Latitude={1}, Longitude={2}]", IncidentId, Latitude, Longitude);
		}
	}
}
