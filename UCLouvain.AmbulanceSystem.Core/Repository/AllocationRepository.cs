using System;
using System.Collections.Generic;
using UCLouvain.AmbulanceSystem.Core.Domain;
using NLog;
using PetaPoco;

namespace UCLouvain.AmbulanceSystem.Core.Repository
{
	public class AllocationRepository
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		IDatabase db;

		public AllocationRepository(IDatabase db)
		{
			this.db = db;
		}

		public void SetMobilizationSent(Allocation allocation)
		{
			db.Execute("update allocations set \"mobilizationSentTimestamp\" = @0 " +
					   "where \"allocationId\" = @1",
					   DateTime.Now,
			           allocation.AllocationId);
		}

        public void SetMobilizationReceived(int allocationId)
        {
            db.Execute("update allocations set \"mobilizationReceivedTimestamp\" = @0 " +
                       "where \"allocationId\" = @1",
                       DateTime.Now,
                       allocationId);

        }

		public void SetMobilizationReceived(Allocation allocation)
		{
			db.Execute("update allocations set \"mobilizationReceivedTimestamp\" = @0 " +
					   "where \"allocationId\" = @1",
					   DateTime.Now,
					   allocation.AllocationId);

		}

		public void RefuseMobilization(int allocationId)
		{
			db.Execute("UPDATE allocations SET \"refused\" = TRUE " +
					   "WHERE \"allocationId\" = @0",
					   allocationId);
		}

		public void SetMobilizationConfirmation(string ambulanceId)
		{
			db.Execute(
				@"UPDATE ""allocations"" SET ""mobilizationConfirmedTimestamp"" = @0 
				  WHERE EXISTS (SELECT * FROM ""ambulances""
			                    WHERE ""ambulanceId"" = @1 
                                      AND ""ambulances"".""ambulanceId"" = ""allocations"".""ambulanceId"")",
				DateTime.Now, ambulanceId
			);
		}

		public void ConfirmCancel(int allocationId)
		{
			db.Execute(
				@"UPDATE ""allocations"" SET ""cancelConfirmed"" = TRUE 
				  WHERE ""allocationId"" = @0",
				allocationId
			);
		}

		public IEnumerable<Allocation> MobilizationToCancel()
		{
			return db.Fetch<Allocation>(
				@"SELECT * FROM ""allocations""
				  WHERE ""allocations"".""cancelled"" = TRUE
                        AND ""allocations"".""cancelConfirmed"" = FALSE;"
			);
		}

		public IEnumerable<Allocation> GetAllocations()
		{
			return db.Query<Allocation>("");

		}

		public IEnumerable<Allocation> GetOpenAllocations(string ambulanceIdentifier)
		{
			return db.Fetch<Allocation>("select * from \"allocations\" left join \"incidents\" on \"allocations\".\"incidentId\" = \"incidents\".\"incidentId\" " +
										"where \"incidents\".\"resolved\" = FALSE and \"ambulanceId\" = @0", ambulanceIdentifier);

		}

		public Allocation GetAllocationForIncident(long incidentId)
		{
			return db.Single<Allocation>(
				@"SELECT * FROM ""allocations"" 
					WHERE ""incidentId"" = @0 
					AND ""allocations"".""cancelled"" = FALSE
					AND ""allocations"".""refused"" = FALSE;",
				incidentId
			);
		}

		public IEnumerable<Incident> UnmobilizedAllocations()
		{
			return db.Fetch<Incident>("SELECT * FROM incidents " +
									  "WHERE EXISTS (SELECT * FROM allocations " +
									  "              WHERE allocations.\"incidentId\" = incidents.\"incidentId\"" +
									  "                    AND allocations.\"mobilizationConfirmedTimestamp\" IS NULL " +
									  "                    AND allocations.\"refused\" = FALSE" +
									  "                    AND allocations.\"cancelled\" = FALSE)");
		}

		public void MarkUnreachable(int allocationId)
		{
			db.Execute(
				@"UPDATE ""incidents"" SET ""unreachable"" = TRUE 
				  WHERE EXISTS (
					SELECT * FROM ""allocations"" 
					WHERE ""incidents"".""incidentId"" = ""allocations"".""incidentId"" 
						  AND ""allocationId"" = @0)",
				allocationId
			);
		}

		public void CancelAllocation(int allocationId, bool confirmed)
		{
			db.Execute(
				@"UPDATE allocations 
				  SET ""cancelled"" = TRUE,
				      ""cancelConfirmed"" = @1
				  WHERE ""allocationId"" = @0", 
				allocationId, confirmed
			);
		}

		public void CancelAllOpenAllocation(string ambulanceId, bool confirmed)
		{
			db.Execute(
				@"UPDATE allocations 
				  SET ""cancelled"" = TRUE,
				      ""cancelConfirmed"" = @1
				  WHERE ""ambulanceId"" = @0 
                        AND ""cancelled"" = FALSE 
                        AND ""refused"" = FALSE
                        AND EXISTS (SELECT * FROM incidents 
                                    WHERE incidents.resolved = FALSE
                                          AND ""allocations"".""incidentId"" = ""incidents"".""incidentId"")",
				ambulanceId, confirmed
			);
		}

		public IEnumerable<Incident> OpenIncidentsWithAmbulanceInTrafficJam()
		{
			return db.Fetch<Incident>(
				@"SELECT * FROM ""incidents"" 
				  LEFT JOIN ""allocations"" ON ""incidents"".""incidentId"" = ""allocations"".""incidentId""
				  LEFT JOIN ""ambulances"" ON ""ambulances"".""ambulanceId"" = ""allocations"".""ambulanceId""
				  WHERE ""ambulances"".""inTrafficJam"" = TRUE
                        AND ""ambulances"".""status"" IN (@0)
                        AND ""allocations"".""cancelled"" = FALSE
                        AND ""allocations"".""refused"" = FALSE
						AND ""incidents"".""resolved"" = FALSE;",
				new int[] { (int) AmbulanceStatus.Leaving }
			);
		}

}
}
