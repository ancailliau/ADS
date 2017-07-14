using System;
using LAS.MdtClient;
using LAS.Core.Utils;
using Itinero;
using Itinero.LocalGeo;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NLog;
using LAS.Core.Messages;

namespace LAS.Simulator
{
	public class SimulatedAmbulance
	{

		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		static readonly Random r = new Random();

		MDT mdt;
		MapService mapService;
		Dictionary<string, Coordinate> hospitals;
		Coordinate homeStation;


		bool mobilized = false;
		MobilizationMessage lastMob;
		ConcurrentQueue<Coordinate> currentRoute;

		/// <summary>
		/// The speed in meter/second.
		/// </summary>
		float speed = 13 * 10;

		List<Task> tasks;
		List<CancellationTokenSource> cancelSources;

		public SimulatedAmbulance(string identifier, 
		                          MapService mapService, Dictionary<string, Coordinate> hospitals,
		                          Coordinate homeStation)
		{
			this.mapService = mapService;
			this.hospitals = hospitals;
			this.homeStation = homeStation;

			tasks = new List<Task>();
			cancelSources = new List<CancellationTokenSource>();
			currentRoute = new ConcurrentQueue<Coordinate>();

			mdt = new MDT(identifier, homeStation.Latitude, homeStation.Longitude, () => SimulatedEnvironment.SendPosition(this));
			mdt.Mobilized += (sender, e) => Task.Run (() => OnMobilized(sender, e));
			mdt.MobilizationCancelled += (sender, e) => Task.Run(() => OnMobilizationCancelled(sender, e));

			mdt.SetAvailableAtStation();
			mdt.InTrafficJam(false);
		}

		void OnMobilizationCancelled(object sender, EventArgs e)
		{
			// Cancel all current tasks, we got something to do!
			foreach (var tt in cancelSources) {
				tt.Cancel();
			}
			cancelSources.Clear();

			// Stop the ambulance
			currentRoute = new ConcurrentQueue<Coordinate>();

			// Wait for completion of all the pending stuff!
			logger.Info("Wait for all tasks to complete.");
			try {
				Task.WaitAll(tasks.ToArray());
			} catch (AggregateException ee) {
				foreach (var v in ee.InnerExceptions)
					Console.WriteLine(ee.Message + " " + v.Message);
			}
			logger.Info("All tasks completed.");
			tasks.Clear();

			mobilized = false;
            
            var lastMessage = (MobilizationCancelled)mdt.Messages.Pop();            
            mdt.ConfirmCancellation(lastMessage.AllocationId);

			var cancelSource = new CancellationTokenSource();
			cancelSources.Add(cancelSource);
			var cancelToken = cancelSource.Token;
			BackToStation(cancelToken, new Coordinate (mdt.latitude, mdt.longitude));
		}

		void OnMobilized(object sender, EventArgs e)
		{
			var lastMessage = (MobilizationMessage)mdt.Messages.Pop();

			if (mobilized) {
				if (lastMessage.AllocationId != lastMob.AllocationId) {
					logger.Info("Ambulance {0} is already mobilized on mobilization {1}. Refuse mobilization.",
								this.mdt.ambulanceId, lastMob.AllocationId);
					mdt.RefuseMobilization(lastMessage.AllocationId);
				}
				return;
				// throw new NotImplementedException("Ambulance already mobilized");
			}

			var refuseMobilization = SimulatedEnvironment.RefuseMobilization(this);
			if (refuseMobilization) {
				logger.Info("Refuse mobilization.");
				mdt.RefuseMobilization(lastMessage.AllocationId);
				return;
			}

            mdt.AcceptMobilization(lastMessage.AllocationId);

			// Cancel all current tasks, we got something to do!
			foreach (var tt in cancelSources) {
				tt.Cancel();
			}
			cancelSources.Clear();

			// Stop the ambulance
			currentRoute = new ConcurrentQueue<Coordinate> ();

			// Wait for completion of all the pending stuff!
			logger.Info("Wait for all tasks to complete.");
			try {
				Task.WaitAll(tasks.ToArray ());
		    }
		    catch (AggregateException ee)
		    {
		        foreach (var v in ee.InnerExceptions)
		            Console.WriteLine(ee.Message + " " + v.Message);
		    }
			logger.Info("All tasks completed.");
			tasks.Clear();

			var cancelSource = new CancellationTokenSource();
			cancelSources.Add(cancelSource);
			var cancelToken = cancelSource.Token;

			var action = new Action(() => {
				mobilized = true;
				lastMob = lastMessage;
				logger.Info("Ambulance {0} mobilized on {1}",
							mdt.ambulanceId,
							lastMessage.AllocationId);

				// Compute route
				var route = mapService.Calculate(mdt.latitude,
												 mdt.longitude,
												 lastMessage.IncidentLatitude,
												 lastMessage.IncidentLongitude);

				//var coin = r.Next(0, 10);
				//if (coin >= 5) {
				//	logger.Info("Refuse mobilization");
				//	mobilized = false;
				//	mdt.RefuseMobilization(lastMessage.AllocationId);
				//	return;

				//} else {
				//	logger.Info("Accept mobilization");
				//}

				logger.Info("About to confirm leaving");
				Thread.Sleep(TimeSpan.FromSeconds(120 / 10));
				mdt.SetLeaving();
				if (cancelToken.IsCancellationRequested == true) {
					Console.WriteLine("Task was cancelled.");
					cancelToken.ThrowIfCancellationRequested();
				}

				// Move to incident 
				logger.Info("Ambulance {0} to incident", mdt.ambulanceId);
				SimulateMove(route, cancelToken);
				if (cancelToken.IsCancellationRequested == true) {
					Console.WriteLine("Task was cancelled.");
					cancelToken.ThrowIfCancellationRequested();
				}

				bool onScenePressed = false;
				if (SimulatedEnvironment.PressOnSceneButton(this)) {
					mdt.SetOnScene();
					onScenePressed = true;
				}

				// Resolve incident
				logger.Info("Ambulance {0} on scene", mdt.ambulanceId);
				var timeToWait = 15; // r.Next(15, 30);
				logger.Info("Ambulance {0} work {1}m on scene", mdt.ambulanceId, timeToWait);
				for (int i = 0; i < timeToWait; i++) {
					//if (!onScenePressed && SimulatedEnvironment.PressOnSceneButton(this)) {
					//	mdt.SetOnScene();
					//	onScenePressed = true;
					//}
					Thread.Sleep(1000);
				}

				if (cancelToken.IsCancellationRequested == true) {
					Console.WriteLine("Task was cancelled.");
					cancelToken.ThrowIfCancellationRequested();
				}

				// Move to hospital
				logger.Info("Ambulance {0} to hospital {1}", mdt.ambulanceId, lastMessage.HospitalIdentifier);
				var hospitalCoordinates = hospitals[lastMessage.HospitalIdentifier];
				route = mapService.Calculate(mdt.latitude, mdt.longitude,
											  hospitalCoordinates.Latitude,
											  hospitalCoordinates.Longitude);
				
				// mdt.SetToHospital();
				SimulateMove(route, cancelToken, () => mdt.SetToHospital());
				if (SimulatedEnvironment.PressAtHospitalButton(this)) {
					mdt.SetAtHospital();
				}
				Thread.Sleep(TimeSpan.FromSeconds(5 * 60 / 10));
				if (cancelToken.IsCancellationRequested == true) {
					Console.WriteLine("Task was cancelled.");
					cancelToken.ThrowIfCancellationRequested();
				}

				// Move back to station
				Thread.Sleep(TimeSpan.FromSeconds(1 * 60 / 10));
				BackToStation(cancelToken, hospitalCoordinates);
			});

			var t = Task.Factory.StartNew(action, cancelToken);
			tasks.Add(t);

		}

		void BackToStation(CancellationToken cancelToken, Coordinate startPosition)
		{
			logger.Info("Ambulance {0} back to station", mdt.ambulanceId);
			if (Coordinate.DistanceEstimateInMeter(startPosition,
													homeStation) > 250) {
				mdt.SetAvailableRadio();
				mobilized = false;

				var route = mapService.Calculate(mdt.latitude, mdt.longitude,
											 homeStation.Latitude,
											 homeStation.Longitude);
				SimulateMove(route, cancelToken);
			}
			if (SimulatedEnvironment.PressAtStationButton(this)) {
				mdt.SetAvailableAtStation();
			}
			mobilized = false;
			mdt.InTrafficJam(false);
			Thread.Sleep(TimeSpan.FromSeconds(5 * 60 / 10));
		}

		internal void SetUnavailable()
		{
			mdt.SetUnavailable();
		}

		void SimulateMove(Route route, CancellationToken cancelToken, Action onFirstMoveCompleted = null)
		{
			// Clear the current route
			currentRoute = new ConcurrentQueue<Coordinate>(route.Shape);

			var currentCoordinate = new Coordinate(mdt.latitude, mdt.longitude);


			// Are we in traffic jam ?
			// if (r.NextDouble() < .01) {
			//mdt.InTrafficJam(true);
			// }

			var inTrafficJam = false;
			var lastTrafficJamUpdate = DateTime.MinValue;
			var firstMove = true;

			Coordinate nextStop;
			while (currentRoute.TryDequeue (out nextStop)) {
				// Update traffic jam only every 60 seconds
				if (DateTime.Now - lastTrafficJamUpdate > TimeSpan.FromSeconds(60 / 10)) {
					var inCurrentTrafficJam = SimulatedEnvironment.InTrafficJam(this);

					//logger.Info($"inCurrentTrafficJam={inCurrentTrafficJam} inTrafficJam={inTrafficJam}");
					if (!inTrafficJam & inCurrentTrafficJam) {
						mdt.InTrafficJam(true);
					} else if (inTrafficJam & !inCurrentTrafficJam) {
						mdt.InTrafficJam(false);
					}
					lastTrafficJamUpdate = DateTime.Now;
					inTrafficJam = inCurrentTrafficJam;

				} else {
					//logger.Info("No need to update traffic jam every step.");
				}

				// Compute distance between the two points: 
				var d = Coordinate.DistanceEstimateInMeter(currentCoordinate, nextStop);

				// Estimate time to go to next step
				var timing = d / speed;

				if (inTrafficJam) {
					timing *= 2;
				}

				var waitTime = (int)(timing * 1000);
				currentCoordinate = nextStop;
				mdt.SetPosition(currentCoordinate.Latitude, currentCoordinate.Longitude);

				if (firstMove) {
					if (onFirstMoveCompleted != null)
						onFirstMoveCompleted();
					firstMove = false;
				}

				Thread.Sleep(waitTime);
				if (cancelToken.IsCancellationRequested) {
					logger.Info("Cancellation requested, abort the task!");
					cancelToken.ThrowIfCancellationRequested();
				} else {
					//logger.Info("Continue to move, no cancellation requested.");
				}
			}
		}
	}
}
