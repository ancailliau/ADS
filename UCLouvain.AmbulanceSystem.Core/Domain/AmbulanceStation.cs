using System;

namespace UCLouvain.AmbulanceSystem.Core.Domain
{
    [PetaPoco.TableName ("ambulance_stations")]
    [PetaPoco.PrimaryKey ("stationId", AutoIncrement = false)]
    public class AmbulanceStation
    {
        [PetaPoco.Column("stationId")]
        public string StationId { get; set; }

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

        [PetaPoco.Column("printerId")]
        public string PrinterId {
            get;
            private set;
        }
        
        
        public AmbulanceStation()
        {

        }

        public AmbulanceStation(string identifier, string name, float latitude, float longitude, string printerId)
        {
            Name = name;
            StationId = identifier;
            Latitude = latitude;
            Longitude = longitude;
            PrinterId = printerId;
        }

        public void SetPrinterId(string printerId)
        {
            throw new NotImplementedException();
        }

        public void SetPosition(float latitude, float longitude)
        {
            throw new NotImplementedException();
        }
    }
}

