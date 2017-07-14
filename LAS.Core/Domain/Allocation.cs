using System;
namespace UCLouvain.AmbulanceSystem.Core.Domain
{
	[PetaPoco.TableName("allocations")]
	[PetaPoco.PrimaryKey("allocationId", AutoIncrement = true)]
	public class Allocation
	{
		[PetaPoco.Column("allocationId")]
		public int AllocationId {
			get;
			set;
		}

		[PetaPoco.Column("incidentId")]
		public int IncidentId {
			get;
			set;
		}

		[PetaPoco.Column("ambulanceId")]
		public string AmbulanceId {
			get;
			set;
		}

		[PetaPoco.Column("hospitalId")]
		public string HospitalId {
			get;
			set;
		}

		[PetaPoco.Column("allocationTimestamp")]
		public DateTime? AllocationTimestamp {
			get;
			set;
		}

		[PetaPoco.Column("mobilizationSentTimestamp")]
		public DateTime? MobilizationSentTimestamp {
			get;
			set;
		}

		[PetaPoco.Column("mobilizationReceivedTimestamp")]
		public DateTime? MobilizationReceivedTimestamp {
			get;
			set;
		}

		[PetaPoco.Column("mobilizationConfirmedTimestamp")]
		public DateTime? MobilizationConfirmedTimestamp {
			get;
			set;
		}

		[PetaPoco.Column("refused")]
		public bool Refused { get; set; }

		[PetaPoco.Column("cancelled")]
		public bool Cancelled { get; set; }

		[PetaPoco.Column("cancelConfirmed")]
		public bool CancelConfirmed {
			get;
			set;
		}
	}
}
