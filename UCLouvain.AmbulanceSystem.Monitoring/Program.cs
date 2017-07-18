using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Itinero.LocalGeo;
using MoreLinq;
using NLog;
using PetaPoco;
using UCLouvain.AmbulanceSystem.Core.Domain;
using UCLouvain.AmbulanceSystem.Core.Repository;
using UCLouvain.KAOSTools.Core;
using UCLouvain.KAOSTools.Monitoring;

namespace LAS.Monitoring
{
    public class MainClass
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static Database db;
        static IncidentRepository incidentRepository;
        static AmbulanceRepository ambulanceRepository;
        static AllocationRepository allocationRepository;
        static HospitalRepository hospitalRepository;
        static ConfigurationRepository configurationRepository;
        
		public static void Main(string[] args)
        {
            SetupDatabaseConnections();

            var mq = new MonitoringClient(GetMonitoredState, 
                                          "kaos_monitored_state_queue", 
                                          TimeSpan.FromSeconds(1));
            mq.Run();
        }

        private static void SetupDatabaseConnections()
        {
            var provider = new PostgreSQLDatabaseProvider();
            var connectionString = ConfigurationManager.ConnectionStrings["postgres"].ConnectionString;

            var config = DatabaseConfiguration.Build()
                                          .UsingConnectionString(connectionString)
                                          .UsingProvider(provider)
                                          .UsingDefaultMapper<ConventionMapper>();
            db = new Database(config);
            incidentRepository = new IncidentRepository(db);
            ambulanceRepository = new AmbulanceRepository(db);
            allocationRepository = new AllocationRepository(db);
            hospitalRepository = new HospitalRepository(db);
            configurationRepository = new ConfigurationRepository(db);
        }

        static Dictionary<string, bool> GetMonitoredState ()
        {
            var state = new Dictionary<string, bool>();
            
            int nIncident = 10;
            int nAmbulances = 15;

            var inc = incidentRepository.GetAllIncidents().ToArray();
            var amb = ambulanceRepository.GetAllAmbulances().ToArray();
            var allocations = allocationRepository.GetAllocations().ToArray();

            var a = new Ambulance[nAmbulances];
            for (int i = 0; i < nAmbulances; i++) {
                a[i] = amb.Single((arg) => arg.AmbulanceId == "A" + (i + 1));
            }

            var getAmbulance = new Func<string, Ambulance>((arg) => {
                int id = int.Parse(arg.Substring(1));
                return a[id - 1];
            });

            var incidents = new Incident[nIncident];
            for (int i = 0; i < nIncident; i++) {
                var incs = inc.Where(x => !x.Resolved & x.IncidentId % nIncident == i);
                incidents[i] = (incs.Count() > 0) ? incs.MaxBy(x => x.IncidentId) : null;
            }

            var getIncident = new Func<int, Incident>((arg) => {
                return incidents[arg % nIncident];
            });

            // TODO Fix FirstOrDefault to SingleOrDefault
            var alloc = new Allocation[nIncident];
            for (int i = 0; i < nIncident; i++) {
                alloc[i] = (incidents[i] != null) ?
                    allocations.FirstOrDefault(x => x.IncidentId == incidents[i].IncidentId & !x.Refused && !x.Cancelled)
                               : null;
            }
            var getIncAlloc = new Func<Incident, Allocation>((incident) => {
                return (incident != null) ?
                allocations.FirstOrDefault(x => x.IncidentId == incident.IncidentId & !x.Refused && !x.Cancelled)
                           : null;
            });

            // TODO Fix FirstOrDefault to SingleOrDefault
            var incList = new HashSet<int>(incidents.Where(x => x != null).Select(x => x.IncidentId));

            var ambAlloc = new Allocation[nAmbulances];
            for (int i = 0; i < nAmbulances; i++) {
                ambAlloc[i] = allocations.FirstOrDefault(x => x.AmbulanceId == a[i].AmbulanceId & !x.Refused && !x.Cancelled & incList.Contains(x.IncidentId));
            }
            var getAmbAlloc = new Func<Ambulance, Allocation>((Ambulance aa) => {
                return allocations.FirstOrDefault(x => x.AmbulanceId == aa.AmbulanceId & !x.Refused && !x.Cancelled & incList.Contains(x.IncidentId));
            });

            for (int i = 0; i < nIncident; i++) {
                state.Add("IncReported" + (i + 1), incidents[i] != null);
            }

            state.Add("IncAllocated", incidents.Any(ii => ii != null && allocations.Any(x => x.IncidentId == ii.IncidentId & !x.Refused & !x.Cancelled)));
            for (int i = 0; i < nIncident; i++) {
                state.Add("IncAllocated" + (i + 1), incidents[i] != null && allocations.Any(x => x.IncidentId == incidents[i].IncidentId & !x.Refused & !x.Cancelled));
            }

            state.Add("PendingIncident", inc.Any(x => !x.Resolved && !allocations.Any(y => y.IncidentId == x.IncidentId)));


            for (int i = 0; i < nIncident; i++) {
                state.Add("IncMobilized" + (i + 1), incidents[i] != null && allocations.Any(x => x.IncidentId == incidents[i].IncidentId
                                                                                         & !x.Refused & !x.Cancelled
                                                                                     & x.MobilizationConfirmedTimestamp != null));
            }

            state.Add("AmbAvailable", amb.Any(x => x.Status == AmbulanceStatus.AvailableAtStation |
                                           x.Status == AmbulanceStatus.AvailableRadio));

            var now = DateTime.Now;
            state.Add("AmbPositionKnown", amb.Any(x => (now - x.LastPositionUpdate) < TimeSpan.FromSeconds(1 * 60 / 10)));
            for (int i = 0; i < nAmbulances; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "PositionKnown", (now - a[i].LastPositionUpdate) < TimeSpan.FromSeconds(1 * 60 / 10));
            }

            for (int i = 0; i < nIncident; i++) {
                state.Add("IncPending" + (i + 1), 
                       incidents[i] != null && allocations.Any(x => x.IncidentId == incidents[i].IncidentId
                                                               & !x.Refused & !x.Cancelled & x.MobilizationConfirmedTimestamp == null));
            }

            for (int i = 0; i < nIncident; i++) {
                state.Add("MobilizationOrderRefused" + (i + 1), incidents[i] != null
                       && allocations.Where(x => x.IncidentId == incidents[i].IncidentId
                                            & !x.Cancelled
                                            && x.MobilizationReceivedTimestamp != null).All(x => x.Refused));
            }

            state.Add("AmbMobilizedAndLeaving",
                   amb.Any(ai => allocations.Any(x => x.AmbulanceId == ai.AmbulanceId && !x.Refused && !x.Cancelled) & ai.Status == AmbulanceStatus.Leaving));
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "MobilizedAndLeaving", allocations.Any(x => x.AmbulanceId == a[i].AmbulanceId && !x.Refused && !x.Cancelled) & a[i].Status == AmbulanceStatus.Leaving);
            }

            state.Add("AmbMobilized",
                   amb.Any(ai => incidents.Any(ii => ii != null && allocations.Any(x => x.AmbulanceId == ai.AmbulanceId & !x.Refused & !x.Cancelled & x.IncidentId == ii.IncidentId & x.MobilizationConfirmedTimestamp != null))
                                     ));
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "Mobilized", 
                       incidents.Any (ii => ii != null && allocations.Any(x => x.AmbulanceId == a[i].AmbulanceId & !x.Refused & !x.Cancelled & x.IncidentId == ii.IncidentId & x.MobilizationConfirmedTimestamp != null))
                                     );
            }

            state.Add("AmbAllocated",
                   amb.Any(ai => 
                           incidents.Any(
                               ii => ii != null && allocations.Any(
                                   x => x.AmbulanceId == ai.AmbulanceId & !x.Refused & !x.Cancelled & x.IncidentId == ii.IncidentId))));
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                bool allocated = incidents.Any(ii => ii != null && !ii.Resolved
                                               && allocations.Any(x => x.AmbulanceId == a[i].AmbulanceId & !x.Refused & !x.Cancelled 
                                                                  & x.IncidentId == ii.IncidentId));
                state.Add("AmbA" + id + "Allocated", allocated);
                //logger.Info("Amb A" + id + "Allocated: " + allocated + " and on road: " + (a[i].Status == AmbulanceStatus.AvailableRadio));
                
            }

            state.Add("AmbLeaving", amb.Any(ai => ai.Status == AmbulanceStatus.Leaving));
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "Leaving", a[i].Status == AmbulanceStatus.Leaving);
            }

            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "AvailableAtRadio", a[i].Status == AmbulanceStatus.AvailableRadio);
            }


            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "StuckInTraffic", a[i].InTrafficJam);
            }

            state.Add("AmbMobilizedAndToHospital", amb.Any(ai => {
                var allocAi = getAmbAlloc(ai);
                return allocAi != null
                    && allocAi.MobilizationConfirmedTimestamp != null
                    && ai.Status == AmbulanceStatus.ToHospital;
            }));
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "MobilizedAndToHospital", ambAlloc[i] != null
                       && ambAlloc[i].MobilizationConfirmedTimestamp != null
                       && a[i].Status == AmbulanceStatus.ToHospital);
            }
            //ms.Set("AmbA9MobilizedAndToHospital",  allocA9 != null && allocA9.MobilizationConfirmedTimestamp != null && a9.Status == AmbulanceStatus.ToHospital);
            //ms.Set("AmbA2MobilizedToHospital",     allocA2 != null && allocA2.MobilizationConfirmedTimestamp != null & a2.Status == AmbulanceStatus.ToHospital);
            //ms.Set("AmbA15MobilizedAndToHospital", allocA15 != null && allocA15.MobilizationConfirmedTimestamp != null & a15.Status == AmbulanceStatus.ToHospital);

            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbA" + id + "NotStuckInTrafficAndNotAmbA" + id + "AtHospital", !a[i].InTrafficJam & a[i].Status != AmbulanceStatus.AtHospital);
            }
            //ms.Set("AmbA9NotStuckInTrafficAndNotAmbA9AtHospital", !a9.InTrafficJam & a9.Status != AmbulanceStatus.AtHospital);
            //ms.Set("AmbA2NotStuckInTrafficAndNotAmbA2AtHospital", !a2.InTrafficJam & a2.Status != AmbulanceStatus.AtHospital);
            //ms.Set("AmbA15NotStuckInTrafficAndNotAmbA15AtHospital", !a15.InTrafficJam & a15.Status != AmbulanceStatus.AtHospital);

            state.Add("AmbulanceOnScene", incidents.Any(x => {
                var alloci = getIncAlloc(x);
                return alloci != null && getAmbulance(alloci.AmbulanceId).Status == AmbulanceStatus.OnScene;
            }));
            for (int i = 0; i < nIncident; i++) {
                var condition = alloc[i] != null && getAmbulance(alloc[i].AmbulanceId).Status == AmbulanceStatus.OnScene;
                state.Add("AmbulanceOnScene" + (i + 1), condition);
                //if (condition) {
                //  logger.Info("ambulance on scene: " + alloc[i].AmbulanceId + " - " + getAmbulance(alloc[i].AmbulanceId).AmbulanceId + " - " + incidents[i].Unreachable);
                //}
            }

            //ms.Set("AmbulanceOnScene1", alloc1 != null && getAmbulance(alloc1.AmbulanceId).Status == AmbulanceStatus.OnScene);
            //ms.Set("AmbulanceOnScene2", alloc2 != null &&getAmbulance(alloc2.AmbulanceId).Status == AmbulanceStatus.OnScene);
            //ms.Set("AmbulanceOnScene3", alloc3 != null &&getAmbulance(alloc3.AmbulanceId).Status == AmbulanceStatus.OnScene);

            for (int i = 0; i < nIncident; i++) {
                state.Add("DestinationUnreachable" + (i + 1), incidents[i] != null && incidents[i].Unreachable);
            }
            //ms.Set("DestinationUnreachable1", inc1 != null && !inc1.Reachable);
            //ms.Set("DestinationUnreachable2", inc2 != null && !inc2.Reachable);
            //ms.Set("DestinationUnreachable3", inc3 != null && !inc3.Reachable);

            var radius = 50;
            var onSceneLocation = false;
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                var allocinc = (ambAlloc[i] != null) ? getIncident(ambAlloc[i].IncidentId) : null;
                if (ambAlloc[i] != null && allocinc != null) {
                    var dist = Coordinate.DistanceEstimateInMeter(
                        new Coordinate(allocinc.Latitude, allocinc.Longitude),
                        new Coordinate(a[i].Latitude, a[i].Longitude)
                    );
                    //if (dist <= radius) {
                    //  logger.Info("Distance from incident: " + dist + " status: " + a[i].Status);
                    //}
                    state.Add("AmbulanceA"+id+"OnSceneLocation", dist <= radius);
                    onSceneLocation |= dist <= radius;
                } else {
                    state.Add("AmbulanceA"+id+"OnSceneLocation", false);
                }
            }
            state.Add("AmbulanceOnSceneLocation", onSceneLocation);

            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbulanceA"+id+"OnScene", a[i].Status == AmbulanceStatus.OnScene);
            }

            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbulanceA" + id + "ToHospital", a[i].Status == AmbulanceStatus.ToHospital);
            }

            radius = 10;
            onSceneLocation = false;
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                if (ambAlloc[i] != null) {
                    var inci = getIncident(ambAlloc[i].IncidentId);
                    if (inci != null && !inci.Resolved) {
                        var h = hospitalRepository.GetHospital(ambAlloc[i].HospitalId);
                        var dist = Coordinate.DistanceEstimateInMeter(new Coordinate(h.Latitude, h.Longitude),
                                                           new Coordinate(a[i].Latitude, a[i].Longitude));
                        state.Add("AmbulanceA" + id + "AtHospitalLocation", dist <= radius);
                        onSceneLocation |= dist <= radius;
                    } else {
                        state.Add("AmbulanceA" + id + "AtHospitalLocation", false);
                    }
                } else {
                    state.Add("AmbulanceA"+id+"AtHospitalLocation", false);
                }
            }
            state.Add("AmbulanceAtHospitalLocation", onSceneLocation);

            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbulanceA"+id+"AtHospital", a[i].Status == AmbulanceStatus.AtHospital);
            }
            // TODO
            onSceneLocation = false;
            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbulanceA"+id+"AtStationLocation", a[i].Status == AmbulanceStatus.AvailableAtStation);
                onSceneLocation |= a[i].Status == AmbulanceStatus.AvailableAtStation;
            }
            state.Add("AmbulanceAtStationLocation", onSceneLocation);
            //ms.Set("AmbulanceA9AtStationLocation", a9.Status == AmbulanceStatus.AvailableAtStation);
            //ms.Set("AmbulanceA2AtStationLocation", a2.Status == AmbulanceStatus.AvailableAtStation);
            //ms.Set("AmbulanceA15AtStationLocation", a15.Status == AmbulanceStatus.AvailableAtStation);

            for (int i = 0; i < 15; i++) {
                int id = i + 1;
                state.Add("AmbulanceA"+id+"AtStation", a[i].Status == AmbulanceStatus.AvailableAtStation);
            }
            //ms.Set("AmbulanceA9AtStation", a9.Status == AmbulanceStatus.AvailableAtStation);
            //ms.Set("AmbulanceA2AtStation", a2.Status == AmbulanceStatus.AvailableAtStation);
            //ms.Set("AmbulanceA15AtStation", a15.Status == AmbulanceStatus.AvailableAtStation);

            var currentAllocator = configurationRepository.GetActiveAllocator();
            state.Add("DefaultAllocator", currentAllocator == "DefaultAmbulanceAllocator");
            state.Add("AmbulanceAtStationAllocator", currentAllocator == "AmbulanceAtStationAllocator");

            return state;
        }
	}
}
