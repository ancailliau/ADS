using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using LAS.Core.Domain;
using PetaPoco;
using System.Linq;
using NLog;

namespace LAS.Core.Repository
{
	public class AmbulanceRepository
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		IDatabase db;

		TableInfo tableInfo;

		string TABLE_NAME {
			get {
				return tableInfo.TableName;
			}
		}

		string COLUMN_NAME(string propertyname)
		{
			return ColumnInfo.FromProperty(typeof(Ambulance).GetProperty(propertyname)).ColumnName;
		}

		public IEnumerable<Ambulance> GetAvailableAmbulances()
		{
			return db.Fetch<Ambulance>(
				@"SELECT * FROM ""ambulances"" 
				  WHERE ""ambulances"".""status"" IN (@0) 
				  AND NOT EXISTS (SELECT * FROM ""allocations"" 
								  LEFT JOIN ""incidents"" ON ""allocations"".""incidentId"" = ""incidents"".""incidentId"" 
								  WHERE ""allocations"".""ambulanceId"" = ""ambulances"".""ambulanceId"" 
								    AND ""incidents"".""resolved"" = FALSE
                                    AND ""allocations"".""refused"" = FALSE
                                    AND ""allocations"".""cancelled"" = FALSE)",
				new int[] { (int)AmbulanceStatus.AvailableRadio, (int)AmbulanceStatus.AvailableAtStation }
			);
		}

		public IEnumerable<Ambulance> GetAvailableAmbulances(int incidentId)
		{
			return db.Fetch<Ambulance>(
				@"SELECT * FROM ""ambulances"" 
				  WHERE ""ambulances"".""status"" IN (@0) 
				  AND NOT EXISTS (SELECT * FROM ""allocations"" 
								  LEFT JOIN ""incidents"" ON ""allocations"".""incidentId"" = ""incidents"".""incidentId"" 
								  WHERE ""allocations"".""ambulanceId"" = ""ambulances"".""ambulanceId"" 
								    AND ""incidents"".""resolved"" = FALSE
                                    AND ""allocations"".""refused"" = FALSE
                                    AND ""allocations"".""cancelled"" = FALSE)
				  -- Do not allocate an ambulance twice on the same incident
				  AND NOT EXISTS (SELECT * FROM ""allocations"" 
								  WHERE ""incidentId"" = @1
								  	AND ""allocations"".""ambulanceId"" = ""ambulances"".""ambulanceId"")",
				new int[] { (int) AmbulanceStatus.AvailableRadio, (int) AmbulanceStatus.AvailableAtStation },
				incidentId
			);
		}

		public AmbulanceRepository(IDatabase conn)
		{
			this.db = conn;
			tableInfo = TableInfo.FromPoco(typeof(Ambulance));
		}

		public Ambulance AddAmbulance(string id, 
		                              float lat, 
		                              float lon, 
		                              AmbulanceStatus status)
		{
				var amb = new Ambulance(id) {
					Latitude = lat,
					Longitude = lon,
				LastPositionUpdate = DateTime.Now,
					Status = status
				};
				db.Insert(amb);
				return amb;
		}

		public Ambulance AddAmbulance(string identifier, float latitude, float longitude, AmbulanceStatus status, string listeningPort)
		{
				var amb = new Ambulance(identifier) {
					Latitude = latitude,
					Longitude = longitude,
					LastPositionUpdate = DateTime.Now,
					Status = status,
					Port = listeningPort
				};
				db.Insert(amb);
				return amb;
		}

		public IEnumerable<Ambulance> GetAllAmbulances()
		{
				return db.Query<Ambulance>("");
		}

		public bool Contains(string identifier)
		{
				return db.ExecuteScalar<int>(
					"select count(*) from ambulances where \"ambulanceId\" = @0",
					identifier) > 0;
		}

		public void MarkInTrafficJam(string ambulanceId, bool inTrafficJam)
		{
			db.Execute(@"UPDATE ""ambulances"" SET ""inTrafficJam"" = @1 WHERE ""ambulanceId"" = @0", ambulanceId, inTrafficJam);
		}

		public Ambulance Get(string ambulanceIdentifier)
		{
				return db.Query<Ambulance>("select * from ambulances where \"ambulanceId\" = @0",
										   ambulanceIdentifier).SingleOrDefault();
		}

		public void Update(Ambulance a)
		{
				db.Update(a);
		}

		public void UpdatePosition(string ambulanceId, float latitude, float longitude)
		{
			db.Execute(
				@"UPDATE ""ambulances"" 
				  SET 
				    ""latitude"" = @0, 
                    ""longitude"" = @1, 
                    ""lastPositionUpdate"" = @2
				  WHERE ""ambulanceId"" = @3",
				latitude, longitude, DateTime.Now, ambulanceId
			);
		}

		public void UpdateStatus(string ambulanceIdentifier, AmbulanceStatus status)
		{
				db.Update(
					TABLE_NAME,
					COLUMN_NAME(nameof(Ambulance.AmbulanceId)),
					new { ambulanceId = ambulanceIdentifier, status = status });
			
		}
	}
}

