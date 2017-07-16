using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using KAOSTools.MetaModel;
using LAS.Core.Utils;
using LtlSharp;
using LtlSharp.Monitoring;
using MoreLinq;
using NLog;
using PetaPoco;
using Simulator;
using UncertaintySimulation;
using LAS.Core.Repository;
using System.Configuration;
using LAS.Core.Domain;
using Itinero.LocalGeo;
using LAS.Core.Messages;

namespace LAS.Monitoring
{
	public class MainClass
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		static KAOSModel model;
		static Dictionary<KAOSMetaModelElement, ObstructionSuperset> obstructions;
		static GoalMonitor monitor;

		static Database db;
		static IncidentRepository incidentRepository;
		static AmbulanceRepository ambulanceRepository;
		static AllocationRepository allocationRepository;
		static HospitalRepository hospitalRepository;
		static ConfigurationRepository configurationRepository;

		static CSVGoalExportProcessor csvExport;

		public static void Main(string[] args)
		{
			//DeployAmbulanceAtStationAllocator();
			//Thread.Sleep(TimeSpan.FromSeconds(10));

			Console.WriteLine("Hello World!");
			var monitoringDelay = TimeSpan.FromSeconds(1);

			logger.Info("Connecting to database");
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
			logger.Info("Connected to database");

			logger.Info("Building KAOS model.");
			var filename = "./Models/simple.kaos";
			var parser = new KAOSTools.Parsing.ModelBuilder();
			model = parser.Parse(File.ReadAllText(filename), filename);
			var model2 = parser.Parse(File.ReadAllText(filename), filename);

			ActiveResolutions = Enumerable.Empty<Resolution>();

			var declarations = parser.Declarations;
			logger.Info("(done)");

			logger.Info("Configuring monitors.");
			// Configure all the monitors (for all obstacles and domain properties).
			KAOSMetaModelElement[] goals = model.Goals().ToArray();
			KAOSMetaModelElement[] obstacles = model.LeafObstacles().ToArray();
			var projection = new HashSet<string>(GetAllPredicates(goals));
			monitor = new GoalMonitor(model, goals.Union(obstacles), projection, HandleFunc,
			                          // new TimedStateInformationStorage(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(120)),
			                          monitoringDelay);
			logger.Info("(done)");

			foreach (var p in model.Predicates()) {
				Console.WriteLine(p.FriendlyName);
			}

			// What goals and obstacles should appear in LOG
			cpsGoals = model.Goals(x => x.CustomData.ContainsKey("log_cps"));
			cpsObstacles = model.Obstacles(x => x.CustomData.ContainsKey("log_cps"));

			// Initialize obstruction sets
			obstructionLock = new object();
			ComputeObstructionSets();

			Console.WriteLine("Waiting ...");
			Console.ReadKey();

			logger.Info("Launching monitors");
			monitor.Run(false);

			var goalMonitorProcessor = new GoalMonitorProcessor(monitor);
			csvExport = new CSVGoalExportProcessor("experiment-goal.csv", "experiment-obstacle.csv");
			// goalMonitorProcessor.AddProcessor(csvExport, monitoringDelay);

			new Timer((state) => UpdateCPS(), null, monitoringDelay, monitoringDelay);
			new Timer((state) => MonitorStep(), null, monitoringDelay, monitoringDelay);
			Thread.Sleep(TimeSpan.FromSeconds(5));
			logger.Info("Launching processors");
			//new Timer((state) => LogStatistic(), null, monitoringDelay, monitoringDelay);
			new Timer((state) => LogCSV(), null, monitoringDelay, monitoringDelay);

			// Configure optimization process.
			optimizer = new Optimizer(monitor, model2);
			new Timer((state) => Optimize(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));

			while (true);
		}

		static IStateInformationStorage HandleFunc(KAOSMetaModelElement arg)
		{
			if (arg is Obstacle) {
				if (arg.Identifier == "mobilization_on_road") {
					return new TimedStateInformationStorage(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));

				} else if (arg.Identifier == "mobilization_on_road_when_na") {
					return new TimedStateInformationStorage(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));
				}
			}
			return new InfiniteStateInformationStorage();
		}

		static Optimizer optimizer;
		static IEnumerable<Resolution> ActiveResolutions;

		static void DeployReallocator()
		{
			logger.Info("Deploy reallocator");
		}

		static void RemoveReallocator()
		{
			logger.Info("Remove reallocator");
		}

		static MessageSender sender = new MessageSender();

		public static void DeployAmbulanceAtStationAllocator()
		{
			logger.Info("Deploy AmbulanceAtStationAllocator");
			var m = new DeployAllocatorMessage("AmbulanceAtStationAllocator");
			sender.Send(m);
		}

		public static void RemoveAmbulanceAtStationAllocator()
		{
			logger.Info("Deploy DefaultAmbulanceAllocator");
			var m = new DeployAllocatorMessage("DefaultAmbulanceAllocator");
			sender.Send(m);
		}

		static DateTime startTime = DateTime.Now;

		static void Optimize()
		{
			logger.Info("Optimize :-)");
			var goal = model.Goal(x => x.Identifier == "avoid_ambulance_mobilized_on_road");
			if (DateTime.Now - startTime > TimeSpan.FromMinutes(25)) {
				optimizer.model.Goal(x => x.Identifier == goal.Identifier).RDS = .8;
				if (goal.CPS > .8) {
					return;
				}
			} else {
				optimizer.model.Goal(x => x.Identifier == goal.Identifier).RDS = .5;
				if (goal.CPS > .5) {
					return;
				}
			}

			var countermeasures = optimizer.Optimize();
			if (countermeasures == null)
				return;

			logger.Info("---- Selected CM");
			foreach (var item in countermeasures) {
				logger.Info(item.ResolvingGoal().FriendlyName);
			}
			logger.Info("---");

			Type simType = typeof(LAS.Monitoring.MainClass);

			// Adapt the software
			foreach (var cm in ActiveResolutions.Except(countermeasures)) {
				if (cm.ResolvingGoal().CustomData.ContainsKey("onremove")) {
					var method = cm.ResolvingGoal().CustomData["onremove"];
					var mi = simType.GetMethod(method);
					if (mi != null) {
						mi.Invoke(null, new object[] { });
					} else {
						logger.Warn("Cannot find the method " + method);
					}
				} else {
					logger.Info("Miss onremove for " + cm.ResolvingGoal().FriendlyName);
				}
			}
			foreach (var cm in countermeasures.Except(ActiveResolutions)) {
				if (cm.ResolvingGoal().CustomData.ContainsKey("ondeploy")) {
					var method = cm.ResolvingGoal().CustomData["ondeploy"];
					var mi = simType.GetMethod(method);
					if (mi != null) {
						mi.Invoke(null, new object[] { });
					} else {
						logger.Warn("Cannot find the method " + method);
					}
				} else {
					logger.Info("Miss ondeploy for " + cm.ResolvingGoal().FriendlyName);
				}
			}

			// Update the requirement model
			logger.Info("Countermeasures to withold:");
			foreach (var cm in ActiveResolutions.Except(countermeasures)) {
				logger.Info("- {0}", cm.ResolvingGoal().FriendlyName);

				var resolutionToWithold = model.Elements.OfType<Resolution>()
											  .Single(x => x.ResolvingGoalIdentifier == cm.ResolvingGoalIdentifier
													  & x.ObstacleIdentifier == cm.ObstacleIdentifier);

				ResolutionIntegrationHelper.Desintegrate(resolutionToWithold);

			}
			logger.Info("Countermeasures to deploy: ");
			foreach (var cm in countermeasures.Except(ActiveResolutions)) {
				logger.Info("- {0}", cm.ResolvingGoal().FriendlyName);

				var resolutionToDeploy = model.Elements.OfType<Resolution>()
											  .Single(x => x.ResolvingGoalIdentifier == cm.ResolvingGoalIdentifier
													  & x.ObstacleIdentifier == cm.ObstacleIdentifier);

				ResolutionIntegrationHelper.Integrate(resolutionToDeploy);
			}

			ActiveResolutions = countermeasures;

			// Update the obstruction sets
			ComputeObstructionSets();
			UpdateCPS();
			logger.Info("New configuration deployed");

		}

		static IEnumerable<Goal> cpsGoals;
		static IEnumerable<Obstacle> cpsObstacles;

		static void LogCSV()
		{
			csvExport.Process(monitor, cpsGoals, cpsObstacles);
		}

		static void LogStatistic()
		{
			var maxLen = monitor.kaosElementMonitor.Max(x => x.Key.FriendlyName.Length);
			HashSet<int> hashes = new HashSet<int>();

			var sb = new StringBuilder();
			sb.AppendLine("Goal statisfaction statistics:");
			sb.AppendFormat("+-{0}-+----------------------------------------+----------------------------------------+\n", new string('-', maxLen));
			sb.AppendFormat("| {0} | min                                    + max                                    +\n", new string(' ', maxLen));
			sb.AppendFormat("| {0} +------+-------------+-------------+-----+------+-------------+-------------+-----+\n", "Name".PadRight(maxLen));
			sb.AppendFormat("| {0} | mean | conf. int   | id          | #   | mean | conf. int   | id          | #   |\n", new string(' ', maxLen));
			sb.AppendFormat("+-{0}-+------+-------------+-------------+-----+------+-------------+-------------+-----+\n", new string('-', maxLen));
			foreach (var m in monitor.kaosElementMonitor) {
				if (m.Value.Max != null & m.Value.Min != null) {
					sb.AppendFormat("| {0,-" + maxLen + "} " +
									"| {1:0.00} | [{2:0.00},{3:0.00}] | {4,-11} | {5,3} " +
									"| {6:0.00} | [{7:0.00},{8:0.00}] | {9,-11} | {10,3} |\n",
									m.Key.FriendlyName,
									m.Value.Min.Mean,
									m.Value.Min.Mean - 1.64 * m.Value.Min.StdDev,
									m.Value.Min.Mean + 1.64 * m.Value.Min.StdDev,
									m.Value.Min.Hash,
									m.Value.Min.Negative + m.Value.Min.Positive,
									m.Value.Max.Mean,
									m.Value.Max.Mean - 1.64 * m.Value.Max.StdDev,
									m.Value.Max.Mean + 1.64 * m.Value.Max.StdDev,
									m.Value.Max.Hash,
									m.Value.Max.Negative + m.Value.Max.Positive);
					hashes.Add(m.Value.Min.Hash);
					hashes.Add(m.Value.Max.Hash);
				} else {
					sb.AppendFormat("| {0,-" + maxLen + "} | {1,-67} |\n",
									m.Key.FriendlyName,
									"N.A.");
				}
			}
			sb.AppendFormat("+-{0}-+------+-------------+-------------+-----+------+-------------+-------------+-----+\n", new string('-', maxLen));

			//if (monitor.MonitoredStates.Count > 0) {
			//	var mLen = monitor.MonitoredStates.Max(x => x.Value.state.Max(y => y.Key.Name.Length));
			//	sb.AppendFormat("+-------------+-{0}-+\n", new string('-', mLen + 12));
			//	foreach (var hash in hashes) {
			//		var subArray = new List<string>();
			//		subArray.Add(string.Format("+-{0}-+-------+", new string('-', mLen)));
			//		foreach (var prop in monitor.MonitoredStates[hash].state) {
			//			subArray.Add(string.Format("| {0,-" + mLen + "} | {1,-5} |", prop.Key.Name, prop.Value));
			//		}
			//		subArray.Add(string.Format("+-{0}-+-------+", new string('-', mLen)));

			//		sb.AppendFormat("| {0} | {1} |\n", hash, subArray[0]);
			//		for (int i = 1; i < subArray.Count; i++) {
			//			sb.AppendFormat("|             | {0} |\n", subArray[i]);
			//		}
			//	}
			//	sb.AppendFormat("+-------------+-{0}-+\n", new string('-', mLen + 12));
			//}

			logger.Info(sb.ToString());
		}

		static object obstructionLock;

		static void ComputeObstructionSets()
		{
			lock (obstructionLock) {
				logger.Info("Obstruction sets ----");
				obstructions = new Dictionary<KAOSMetaModelElement, ObstructionSuperset>();
				// Get the obstruction set
				var goals = cpsGoals;
				foreach (var goal in goals) {
					logger.Info("Obstruction set for " + goal.FriendlyName);
					ObstructionSuperset obstructionSuperset;
					if (goal.Replacements().Count() > 0) {
						obstructionSuperset = goal.GetObstructionSuperset(false);
					} else {
						obstructionSuperset = goal.GetObstructionSuperset(true);
					}
					obstructions.Add(goal, obstructionSuperset);
					//foreach (var kv in obstructionSuperset.mapping) {
					//	logger.Info("{0} => {1}", kv.Key.FriendlyName, kv.Value);
					//}
				}

				foreach (var obstacle in cpsObstacles) {
					logger.Info("Obstruction set for " + obstacle.FriendlyName);
					ObstructionSuperset obstructionSuperset = obstacle.GetObstructionSuperset();
					obstructions.Add(obstacle, obstructionSuperset);
					//foreach (var kv in obstructionSuperset.mapping) {
					//	logger.Info("{0} => {1}", kv.Key.FriendlyName, kv.Value);
					//}
				}
				logger.Info("---- obstruction sets");
			}
		}

		static void UpdateCPS()
		{
			lock (obstructionLock) {
				foreach (var kv in obstructions) {
					var sampleVector = new Dictionary<int, double>();
					foreach (var o in kv.Value.mapping) {
						var candidate = monitor.kaosElementMonitor.Where(a => a.Key.Identifier == o.Key.Identifier);
						if (candidate.Count() == 1) {
							var lmon = candidate.First().Value;
							if (lmon.Max != null) {
								sampleVector.Add(o.Value, lmon.Max.Mean);
							} else {
								sampleVector.Add(o.Value, 0); // no data = 1
								//logger.Warn("No data available for " + kv.Key.FriendlyName);
							}
						} else {
							//logger.Error("No key {0} in monitors", o.Key.FriendlyName);
							sampleVector.Add(o.Value, ((Obstacle)o.Key).EPS);
						}
					}

					var p = 1 - kv.Value.GetProbability(sampleVector);
					if (kv.Key is Goal) {
						((Goal)kv.Key).CPS = p;
					} else if (kv.Key is Obstacle) {
						((Obstacle)kv.Key).CPS = 1-p;
					}
				}
			}
		}

		static void MonitorStep()
		{
			var ms = GetMonitoredState();
			//logger.Info("Size of the monitored state " + ms.state.Count);

			//var sb = new StringBuilder();
			//var maxLen = ms.state.Max(x => x.Key.Name.Length);
			//sb.AppendLine("Monitor Step:");
			//sb.AppendFormat(" +-{0}-+-------+\n", new string('-', maxLen));
			//foreach (var kv in ms.state) {
			//	sb.AppendFormat(" | {0,-" + maxLen + "} | {1,-5} |\n", kv.Key.Name, kv.Value);
			//}
			//sb.AppendFormat(" +-{0}-+-------+", new string('-', maxLen));
			//logger.Info(sb.ToString());

			monitor.MonitorStep(ms, DateTime.Now);
		}

		static HashSet<string> GetAllPredicates(KAOSMetaModelElement[] goals)
		{
			var predicates = goals.SelectMany(x => {
				if (x is Goal && ((Goal)x).FormalSpec != null) {
					return ((Goal)x).FormalSpec.PredicateReferences;

				} else if (x is Obstacle && ((Obstacle)x).FormalSpec != null) {
					return ((Obstacle)x).FormalSpec.PredicateReferences;

				} else {
					return Enumerable.Empty<PredicateReference>();
				}
			});

			return predicates.Select(x => x.Predicate.Name).ToHashSet();
		}

		static MonitoredState GetMonitoredState()
		{
			int nIncident = 10;

			var inc = incidentRepository.GetAllIncidents().ToArray();
			var amb = ambulanceRepository.GetAllAmbulances().ToArray();
			var allocations = allocationRepository.GetAllocations().ToArray();

			var a = new Ambulance[15];
			for (int i = 0; i < 15; i++) {
				a[i] = amb.Single((arg) => arg.AmbulanceId == "A" + (i + 1));
			}

			var getAmbulance = new Func<string, Ambulance>((arg) => {
				int id = int.Parse(arg.Substring(1));
				return a[id - 1];
			});

			var ms = new MonitoredState();

			var incidents = new Incident[nIncident];
			for (int i = 0; i < nIncident; i++) {
				var incs = inc.Where(x => !x.Resolved & x.IncidentId % nIncident == i);
				incidents[i] = (incs.Count() > 0) ? incs.MaxBy(x => x.IncidentId) : null;
			}

			var getIncident = new Func<int, Incident>((arg) => {
				return incidents[arg % nIncident];
			});

			//for (int i = 0; i < nIncident; i++) {
			//	logger.Info("incident " + i + " = " + incidents[i]?.IncidentId);
			//}

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

			var ambAlloc = new Allocation[15];
			for (int i = 0; i < 15; i++) {
				ambAlloc[i] = allocations.FirstOrDefault(x => x.AmbulanceId == a[i].AmbulanceId & !x.Refused && !x.Cancelled & incList.Contains(x.IncidentId));
			}
			var getAmbAlloc = new Func<Ambulance, Allocation>((Ambulance aa) => {
				return allocations.FirstOrDefault(x => x.AmbulanceId == aa.AmbulanceId & !x.Refused && !x.Cancelled & incList.Contains(x.IncidentId));
			});

			for (int i = 0; i < nIncident; i++) {
				ms.Set("IncReported" + (i + 1), incidents[i] != null);
			}

			ms.Set("IncAllocated", incidents.Any(ii => ii != null && allocations.Any(x => x.IncidentId == ii.IncidentId & !x.Refused & !x.Cancelled)));
			for (int i = 0; i < nIncident; i++) {
				ms.Set("IncAllocated" + (i + 1), incidents[i] != null && allocations.Any(x => x.IncidentId == incidents[i].IncidentId & !x.Refused & !x.Cancelled));
			}

			ms.Set("PendingIncident", inc.Any(x => !x.Resolved && !allocations.Any(y => y.IncidentId == x.IncidentId)));


			for (int i = 0; i < nIncident; i++) {
				ms.Set("IncMobilized" + (i + 1), incidents[i] != null && allocations.Any(x => x.IncidentId == incidents[i].IncidentId
				                                                                         & !x.Refused & !x.Cancelled
																					 & x.MobilizationConfirmedTimestamp != null));
			}

			ms.Set("AmbAvailable", amb.Any(x => x.Status == Core.Domain.AmbulanceStatus.AvailableAtStation |
										   x.Status == Core.Domain.AmbulanceStatus.AvailableRadio));

			var now = DateTime.Now;
			ms.Set("AmbPositionKnown", amb.Any(x => (now - x.LastPositionUpdate) < TimeSpan.FromSeconds(1 * 60 / 10)));
			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "PositionKnown", (now - a[i].LastPositionUpdate) < TimeSpan.FromSeconds(1 * 60 / 10));
			}

			for (int i = 0; i < nIncident; i++) {
				ms.Set("IncPending" + (i + 1), 
				       incidents[i] != null && allocations.Any(x => x.IncidentId == incidents[i].IncidentId
				                                               & !x.Refused & !x.Cancelled & x.MobilizationConfirmedTimestamp == null));
			}

			for (int i = 0; i < nIncident; i++) {
				ms.Set("MobilizationOrderRefused" + (i + 1), incidents[i] != null
					   && allocations.Where(x => x.IncidentId == incidents[i].IncidentId
				                            & !x.Cancelled
											&& x.MobilizationReceivedTimestamp != null).All(x => x.Refused));
			}

			ms.Set("AmbMobilizedAndLeaving",
				   amb.Any(ai => allocations.Any(x => x.AmbulanceId == ai.AmbulanceId && !x.Refused && !x.Cancelled) & ai.Status == AmbulanceStatus.Leaving));
			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "MobilizedAndLeaving", allocations.Any(x => x.AmbulanceId == a[i].AmbulanceId && !x.Refused && !x.Cancelled) & a[i].Status == AmbulanceStatus.Leaving);
			}

			ms.Set("AmbMobilized",
			       amb.Any(ai => incidents.Any(ii => ii != null && allocations.Any(x => x.AmbulanceId == ai.AmbulanceId & !x.Refused & !x.Cancelled & x.IncidentId == ii.IncidentId & x.MobilizationConfirmedTimestamp != null))
									 ));
			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "Mobilized", 
				       incidents.Any (ii => ii != null && allocations.Any(x => x.AmbulanceId == a[i].AmbulanceId & !x.Refused & !x.Cancelled & x.IncidentId == ii.IncidentId & x.MobilizationConfirmedTimestamp != null))
				                     );
			}

			ms.Set("AmbAllocated",
			       amb.Any(ai => 
			               incidents.Any(
				               ii => ii != null && allocations.Any(
					               x => x.AmbulanceId == ai.AmbulanceId & !x.Refused & !x.Cancelled & x.IncidentId == ii.IncidentId))));
			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				bool allocated = incidents.Any(ii => ii != null && !ii.Resolved
				                               && allocations.Any(x => x.AmbulanceId == a[i].AmbulanceId & !x.Refused & !x.Cancelled 
				                                                  & x.IncidentId == ii.IncidentId));
				ms.Set("AmbA" + id + "Allocated", allocated);
				//logger.Info("Amb A" + id + "Allocated: " + allocated + " and on road: " + (a[i].Status == AmbulanceStatus.AvailableRadio));
				
			}

			ms.Set("AmbLeaving", amb.Any(ai => ai.Status == AmbulanceStatus.Leaving));
			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "Leaving", a[i].Status == AmbulanceStatus.Leaving);
			}

			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "AvailableAtRadio", a[i].Status == AmbulanceStatus.AvailableRadio);
			}


			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "StuckInTraffic", a[i].InTrafficJam);
			}

			ms.Set("AmbMobilizedAndToHospital", amb.Any(ai => {
				var allocAi = getAmbAlloc(ai);
				return allocAi != null
					&& allocAi.MobilizationConfirmedTimestamp != null
					&& ai.Status == AmbulanceStatus.ToHospital;
			}));
			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "MobilizedAndToHospital", ambAlloc[i] != null
					   && ambAlloc[i].MobilizationConfirmedTimestamp != null
					   && a[i].Status == AmbulanceStatus.ToHospital);
			}
			//ms.Set("AmbA9MobilizedAndToHospital",  allocA9 != null && allocA9.MobilizationConfirmedTimestamp != null && a9.Status == AmbulanceStatus.ToHospital);
			//ms.Set("AmbA2MobilizedToHospital",     allocA2 != null && allocA2.MobilizationConfirmedTimestamp != null & a2.Status == AmbulanceStatus.ToHospital);
			//ms.Set("AmbA15MobilizedAndToHospital", allocA15 != null && allocA15.MobilizationConfirmedTimestamp != null & a15.Status == AmbulanceStatus.ToHospital);

			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbA" + id + "NotStuckInTrafficAndNotAmbA" + id + "AtHospital", !a[i].InTrafficJam & a[i].Status != AmbulanceStatus.AtHospital);
			}
			//ms.Set("AmbA9NotStuckInTrafficAndNotAmbA9AtHospital", !a9.InTrafficJam & a9.Status != AmbulanceStatus.AtHospital);
			//ms.Set("AmbA2NotStuckInTrafficAndNotAmbA2AtHospital", !a2.InTrafficJam & a2.Status != AmbulanceStatus.AtHospital);
			//ms.Set("AmbA15NotStuckInTrafficAndNotAmbA15AtHospital", !a15.InTrafficJam & a15.Status != AmbulanceStatus.AtHospital);

			ms.Set("AmbulanceOnScene", incidents.Any(x => {
				var alloci = getIncAlloc(x);
				return alloci != null && getAmbulance(alloci.AmbulanceId).Status == AmbulanceStatus.OnScene;
			}));
			for (int i = 0; i < nIncident; i++) {
				var condition = alloc[i] != null && getAmbulance(alloc[i].AmbulanceId).Status == AmbulanceStatus.OnScene;
				ms.Set("AmbulanceOnScene" + (i + 1), condition);
				//if (condition) {
				//	logger.Info("ambulance on scene: " + alloc[i].AmbulanceId + " - " + getAmbulance(alloc[i].AmbulanceId).AmbulanceId + " - " + incidents[i].Unreachable);
				//}
			}

			//ms.Set("AmbulanceOnScene1", alloc1 != null && getAmbulance(alloc1.AmbulanceId).Status == AmbulanceStatus.OnScene);
			//ms.Set("AmbulanceOnScene2", alloc2 != null &&getAmbulance(alloc2.AmbulanceId).Status == AmbulanceStatus.OnScene);
			//ms.Set("AmbulanceOnScene3", alloc3 != null &&getAmbulance(alloc3.AmbulanceId).Status == AmbulanceStatus.OnScene);

			for (int i = 0; i < nIncident; i++) {
				ms.Set("DestinationUnreachable" + (i + 1), incidents[i] != null && incidents[i].Unreachable);
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
					//	logger.Info("Distance from incident: " + dist + " status: " + a[i].Status);
					//}
					ms.Set("AmbulanceA"+id+"OnSceneLocation", dist <= radius);
					onSceneLocation |= dist <= radius;
				} else {
					ms.Set("AmbulanceA"+id+"OnSceneLocation", false);
				}
			}
			ms.Set("AmbulanceOnSceneLocation", onSceneLocation);

			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbulanceA"+id+"OnScene", a[i].Status == AmbulanceStatus.OnScene);
			}

			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbulanceA" + id + "ToHospital", a[i].Status == AmbulanceStatus.ToHospital);
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
						ms.Set("AmbulanceA" + id + "AtHospitalLocation", dist <= radius);
						onSceneLocation |= dist <= radius;
					} else {
						ms.Set("AmbulanceA" + id + "AtHospitalLocation", false);
					}
				} else {
					ms.Set("AmbulanceA"+id+"AtHospitalLocation", false);
				}
			}
			ms.Set("AmbulanceAtHospitalLocation", onSceneLocation);

			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbulanceA"+id+"AtHospital", a[i].Status == AmbulanceStatus.AtHospital);
			}
			// TODO
			onSceneLocation = false;
			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbulanceA"+id+"AtStationLocation", a[i].Status == AmbulanceStatus.AvailableAtStation);
				onSceneLocation |= a[i].Status == AmbulanceStatus.AvailableAtStation;
			}
			ms.Set("AmbulanceAtStationLocation", onSceneLocation);
			//ms.Set("AmbulanceA9AtStationLocation", a9.Status == AmbulanceStatus.AvailableAtStation);
			//ms.Set("AmbulanceA2AtStationLocation", a2.Status == AmbulanceStatus.AvailableAtStation);
			//ms.Set("AmbulanceA15AtStationLocation", a15.Status == AmbulanceStatus.AvailableAtStation);

			for (int i = 0; i < 15; i++) {
				int id = i + 1;
				ms.Set("AmbulanceA"+id+"AtStation", a[i].Status == AmbulanceStatus.AvailableAtStation);
			}
			//ms.Set("AmbulanceA9AtStation", a9.Status == AmbulanceStatus.AvailableAtStation);
			//ms.Set("AmbulanceA2AtStation", a2.Status == AmbulanceStatus.AvailableAtStation);
			//ms.Set("AmbulanceA15AtStation", a15.Status == AmbulanceStatus.AvailableAtStation);

			var currentAllocator = configurationRepository.GetActiveAllocator();
			ms.Set("DefaultAllocator", currentAllocator == "DefaultAmbulanceAllocator");
			ms.Set("AmbulanceAtStationAllocator", currentAllocator == "AmbulanceAtStationAllocator");

			return ms;
		}
	}
}
