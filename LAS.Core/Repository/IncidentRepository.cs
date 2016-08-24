using System;
using System.Collections;
using System.Collections.Generic;
using LAS.Core.Domain;
using LAS.Core.Messages;
using NLog;
using PetaPoco;

namespace LAS.Core.Repository
{
	public class IncidentRepository
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		IDatabase db;

		public IncidentRepository(IDatabase db)
		{
			this.db = db;
		}

		public IEnumerable<Allocation> GetOpenAllocation(Int64 incidentId)
		{
			return db.Query<Allocation>("where \"incidentId\" = @0 " +
			                                          "AND \"refused\" = FALSE " +
			                                          "AND \"cancelled\" = FALSE " +
			                                      "AND \"mobilizationConfirmedTimestamp\" IS NULL", incidentId);
		}

		public int CountOpenIncidents()
		{
			return db.ExecuteScalar<int>("SELECT COUNT(*) FROM incidents WHERE resolved = FALSE");
		}

		public Incident AddIncident (float latitude, float longitude)
		{
				var incident = new Incident(latitude, longitude);
				var res = db.Insert(incident);
				incident.IncidentId = (int)res;
				return incident;
		}

		public int Allocate(Int64 incidentId, string ambulanceId, string hospitalId)
		{
			return db.ExecuteScalar<int>(
				@"INSERT INTO ""allocations"" (""incidentId"",""ambulanceId"",""hospitalId"",""allocationTimestamp"") 
				  VALUES (@0,@1,@2,@3) RETURNING ""allocationId"";",
				incidentId, ambulanceId, hospitalId, DateTime.Now
			);
		}

		public IEnumerable<Incident> GetAllIncidents()
		{
				return db.Query<Incident>("");
		}

		public IEnumerable<Incident> UnallocatedIncident()
		{
				return db.Fetch<Incident>("SELECT * FROM incidents " +
										  "WHERE NOT EXISTS (SELECT * FROM allocations " +
										   "                  WHERE allocations.\"incidentId\" = incidents.\"incidentId\" " +
			                              "                         AND allocations.\"refused\" = FALSE" +
			                              "                         AND allocations.\"cancelled\" = FALSE) " +
			                              "AND incidents.resolved = FALSE");
		}

		public void MarkUnreachable(long incidentId)
		{
			db.Execute(
				@"UPDATE ""incidents"" SET ""unreachable"" = TRUE WHERE ""incidentId"" = @0;",
				incidentId
			);
		}

		public IEnumerable<Incident> GetAllocatedIncidents(string ambulanceIdentifier)
		{
			return db.Fetch<Incident>("select * from \"incidents\" " +
							   "left join \"allocations\" on \"allocations\".\"incidentId\" = \"incidents\".\"incidentId\" " +
							   "where \"allocations\".\"ambulanceId\" = @0 " +
									   "and \"incidents\".\"resolved\" = FALSE", ambulanceIdentifier);

		}

		public void CloseAllocatedIncidents(string ambulanceIdentifier)
		{
			db.Execute(
				@"UPDATE ""incidents"" 
				  SET ""resolved"" = TRUE 
				  WHERE EXISTS (SELECT * FROM ""allocations"" 
              	                WHERE ""allocations"".""incidentId"" = ""incidents"".""incidentId""
                                AND ""allocations"".""ambulanceId"" = @0
								AND ""allocations"".""refused"" = FALSE
                                AND ""allocations"".""cancelled"" = FALSE)", 
				ambulanceIdentifier);

		}



		public void ResolveUnreachableIncidents()
		{
			db.Execute(@"UPDATE ""incidents"" SET ""resolved"" = TRUE WHERE ""unreachable"" = TRUE");
		}
	}
}

