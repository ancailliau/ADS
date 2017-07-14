using System;
using System.IO;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using NLog;
using OsmSharp.Osm.PBF.Streams;
using Itinero.Exceptions;

namespace UCLouvain.AmbulanceSystem.Core.Utils
{
	public class MapService
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		RouterDb routerDb;
		Router router;

		public MapService()
		{
			logger.Info("Loading map...");


			if (File.Exists(@"./Maps/brussels_belgium.cached")) {
				logger.Info("Loading from cache");
				using (var inputStream = new FileInfo(@"./Maps/brussels_belgium.cached").OpenRead()) {
					routerDb = RouterDb.Deserialize (inputStream);
				}
				logger.Info("Loaded");
			} else {
				logger.Info("Building map cache");
				routerDb = new RouterDb();
				using (var inputStream = new FileInfo(@"./Maps/brussels_belgium.osm.pbf").OpenRead()) {
					routerDb.LoadOsmData(inputStream, Vehicle.Car);

					using (var outputStream = new FileInfo(@"./Maps/brussels_belgium.cached").OpenWrite()) {
						routerDb.Serialize(outputStream);
					}
				}
				logger.Info("Cached built");
			}


			//using (var stream = new FileInfo(@"./Maps/brussels_belgium.osm.pbf").OpenRead()) {
			//	routerDb.LoadOsmData (stream, Vehicle.Car);
			//}

			router = new Router(routerDb);
			logger.Info("Map Service initialized.");
		}

		public RouterPoint ResolvePoint(float latitude1, float longitude1)
		{
			return router.Resolve(Vehicle.Car.Fastest(), latitude1, longitude1, 250f);
		}

		public Route Calculate(float latitude1, float longitude1, float latitude2, float longitude2)
		{
			try {
				var p1 = ResolvePoint (latitude1, longitude1);
				var p2 = ResolvePoint (latitude2, longitude2);
				return Calculate(p1, p2);

			} catch (ResolveFailedException e) {
				logger.Error("Could not resolve point: {0}", e.Message);
				throw e;

			}
		}

		public Route Calculate(RouterPoint p1, RouterPoint p2)
		{
			try {
				return router.Calculate(Vehicle.Car.Fastest(), p1, p2);

			} catch (RouteNotFoundException e) {
				logger.Error("Route not found from: {0} to {1}", p1, p2);
				throw e;
			}
		}
	}
}
