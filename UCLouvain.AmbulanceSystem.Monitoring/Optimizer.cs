using System;
using NLog;
using Simulator;
using KAOSTools.MetaModel;
using System.Linq;
using System.Collections.Generic;
using UncertaintySimulation;
using System.Diagnostics;

namespace LAS.Monitoring
{
	public class Optimizer
	{
		GoalMonitor monitor;

		public KAOSModel model;

		Dictionary<Goal, ObstructionSuperset> obstructions;

		double bestScore = 0;
		int minCost;
		IEnumerable<Resolution> bestSelection;

		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		static bool optimizationInProgress = false;

		public Optimizer(GoalMonitor monitor, KAOSModel model)
		{
			this.monitor = monitor;
			this.model = model;
		}

		public IEnumerable<Resolution> Optimize()
		{
			logger.Info("Optimizer called");
			if (optimizationInProgress) {
				logger.Info("Optimization in progress");
				return null;
			}

			if (!monitor.Ready)
				return Enumerable.Empty<Resolution>();

			optimizationInProgress = true;

			var start = Stopwatch.StartNew();

			var resolutions = model.Resolutions();

			minCost = int.MaxValue;
			ComputeMinCost(resolutions.First(), resolutions.Skip(1), Enumerable.Empty<Resolution>());
			logger.Info("Min cost: " + minCost);

			bestScore = 0;
			bestSelection = Enumerable.Empty<Resolution>();

			ComputeScore(resolutions.First(), resolutions.Skip(1), Enumerable.Empty<Resolution>());

			logger.Info("Best Selection: {0} for {{{1}}}", bestScore, string.Join(",", bestSelection.Select(x => x.ResolvingGoal().Identifier)));

			start.Stop();
			logger.Info("Time for optimization: {0} ms ", start.ElapsedMilliseconds);

			optimizationInProgress = false;

			return bestSelection;
		}

		void ComputeObstructionSets()
		{
			obstructions = new Dictionary<Goal, ObstructionSuperset>();

			var roots = model.Goals(x => x.CustomData.ContainsKey("log_cps")).ToArray ();
			foreach (var root in roots) {
				var obstructionSuperset = root.GetObstructionSuperset();
				obstructions.Add(root, obstructionSuperset);
			}
		}

		void ComputeCPS()
		{
			var w1 = Stopwatch.StartNew();
			ComputeObstructionSets();
			w1.Stop();

			var w2 = Stopwatch.StartNew();
			var w3 = new Stopwatch();
			foreach (var kv in obstructions) {
				var sampleVector = new Dictionary<int, double>();
				foreach (var o in kv.Value.mapping) {
					var candidate = monitor.kaosElementMonitor.Where(a => a.Key.Identifier == o.Key.Identifier);
					if (candidate.Count() == 1) {
						var lmon = candidate.First().Value;
						if (lmon.Max != null) {
							w3.Start();
							var mean = lmon.Max.Mean;
							w3.Stop();
							sampleVector.Add(o.Value, mean);
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
				kv.Key.CPS = p;
			}
			w2.Stop();

			//logger.Info("Number of obstructions: " + obstructions.Count());
			//logger.Info("Time to compute CPS {0}ms + {1}ms", w1.ElapsedMilliseconds, w2.ElapsedMilliseconds);
			//logger.Info("Time for computing max: {0}ms", w3.ElapsedMilliseconds);
		}

		void ComputeScore(Resolution r, IEnumerable<Resolution> pending, IEnumerable<Resolution> active)
		{
			if (pending.Count() == 0) {
				ComputeCurrentScore(active);
				ResolutionIntegrationHelper.Integrate(r);
				ComputeCurrentScore(active.Union(new[] { r }));
				ResolutionIntegrationHelper.Desintegrate(r);
				return;
			}

			var next = pending.First();
			ComputeScore(next, pending.Skip(1), active);
			ResolutionIntegrationHelper.Integrate(r);
			ComputeScore(next, pending.Skip(1), active.Union(new[] { r }));
			ResolutionIntegrationHelper.Desintegrate(r);
		}

		void ComputeCurrentScore(IEnumerable<Resolution> active)
		{
			if (active.Count() > minCost) {
				logger.Info("Ignore solution, costing more than necessary");
				return;
			}
			if (ConstraintsSatisfied()) {
				ComputeCPS();

				//var score = model.Goals(x => {
				//	if (x.CustomData.ContainsKey("log_cps") && bool.Parse(x.CustomData["log_cps"])) {
				//		return true;
				//	}
				//	return false;
				//}).Aggregate<Goal, double>(1d, (double arg1, Goal arg2) => {
				//	return arg1 * arg2.CPS;
				//});

				var score = model.Goals(x => x.CustomData.ContainsKey("log_cps")).Sum(x => x.CPS);
				logger.Info("Score: {0} for {{{1}}}", score, string.Join(",", active.Select(x => x.ResolvingGoal().Identifier)));
				if (score > bestScore) {
					bestScore = score;
					bestSelection = active;
				}
			}
		}

		void ComputeMinCost(Resolution r, IEnumerable<Resolution> pending, IEnumerable<Resolution> active)
		{
			if (pending.Count() == 0) {
				ComputeCurrentMinCost(active);
				ResolutionIntegrationHelper.Integrate(r);
				ComputeCurrentMinCost(active.Union(new[] { r }));
				ResolutionIntegrationHelper.Desintegrate(r);
				return;
			}

			var next = pending.First();
			ComputeMinCost(next, pending.Skip(1), active);
			ResolutionIntegrationHelper.Integrate(r);
			ComputeMinCost(next, pending.Skip(1), active.Union(new[] { r }));
			ResolutionIntegrationHelper.Desintegrate(r);
		}

		void ComputeCurrentMinCost(IEnumerable<Resolution> active)
		{
			if (ConstraintsSatisfied()) {
				ComputeCPS();

				if (model.Goals(x => x.CustomData.ContainsKey("log_cps")).All(g => g.CPS > g.RDS)) {
					var cost = active.Count();
					if (minCost > cost) {
						minCost = cost;
					}
				}
			}
		}

		bool ConstraintsSatisfied()
		{
			foreach (var c in model.Elements.OfType<Constraint>()) {
				var result = new List<bool>();
				foreach (var s in c.Conflict) {
					result.Add(model.RootGoals().Any(x => IsGoalActive(s, x)));
				}
				if (result.Count(x => x) > 1) {
					return false;
				}
			}
			return true;
		}

		bool IsGoalActive(string goalIdentifier, Goal root)
		{
			if (root.Replacements().Any()) {
				return root.Replacements().Any(x => IsGoalActive(goalIdentifier, x.ResolvingGoal()));
			}

			if (root.Identifier == goalIdentifier) {
				return true;
			}

			foreach (var s in root.Refinements()) {
				if (s.ParentGoalIdentifier == goalIdentifier) {
					return true;
				}
				foreach (var ss in s.SubGoals()) {
					if (IsGoalActive(goalIdentifier, ss)) {
						return true;
					}
				}
			}

			foreach (var e in root.Exceptions()) {
				if (IsGoalActive(goalIdentifier, e.ResolvingGoal())) {
					return true;
				}
			}

			return false;
		}
	}
}

