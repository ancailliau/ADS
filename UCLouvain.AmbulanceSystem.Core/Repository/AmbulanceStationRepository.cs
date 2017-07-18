using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UCLouvain.AmbulanceSystem.Core.Domain;
using PetaPoco;
using System.Linq;
using NLog;

namespace UCLouvain.AmbulanceSystem.Core.Repository
{
    public class AmbulanceStationRepository
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        IDatabase db;

        TableInfo tableInfo;

        string TABLE_NAME
        {
            get
            {
                return tableInfo.TableName;
            }
        }

        string COLUMN_NAME(string propertyname)
        {
            return ColumnInfo.FromProperty(typeof(Ambulance).GetProperty(propertyname)).ColumnName;
        }

        public AmbulanceStationRepository(IDatabase conn)
        {
            this.db = conn;
            tableInfo = TableInfo.FromPoco(typeof(Ambulance));
        }

        public AmbulanceStation AddAmbulanceStation(string id, 
                                                    string name,
                                                    float lat,
                                                    float lon, 
                                                    string printerId)
        {
            var amb = new AmbulanceStation(id, name, lat, lon, printerId);
            db.Insert(amb);
            return amb;
        }

        public bool Contains(string identifier)
        {
                return db.ExecuteScalar<int>(
                    "select count(*) from ambulance_stations where \"stationId\" = @0",
                    identifier) > 0;
        }
        
        public AmbulanceStation Get(string identifier)
        {
                return db.Query<AmbulanceStation>("select * from ambulance_stations where \"ambulanceId\" = @0",
                                           identifier).SingleOrDefault();
        }

        public void Update(AmbulanceStation a)
        {
                db.Update(a);
        }
    }
}

