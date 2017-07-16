using System;
using System.IO;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using System.Diagnostics;
using System.Threading;
using UCLouvain.AmbulanceSystem.Server;
using UCLouvain.AmbulanceSystem.Core.Domain;
using Mono.Data.Sqlite;
using PetaPoco;

using System.Configuration;

using FluentMigrator.Runner.Announcers;
using System.Reflection;
using FluentMigrator.Runner.Initialization;
using FluentMigrator;
using FluentMigrator.Runner;

using NLog;
using UCLouvain.AmbulanceSystem.Server.Utils;
using UCLouvain.AmbulanceSystem.Core.Repository;
using UCLouvain.AmbulanceSystem.Core.Utils;
using FluentMigrator.Runner.Processors.Postgres;

namespace LAS
{
	class MainClass
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public class MigrationOptions : IMigrationProcessorOptions
		{
			public bool PreviewOnly { get; set; }
			public string ProviderSwitches { get; set; }
			public int Timeout { get; set; }
		}

		public static void MigrateToLatest(string connectionString)
		{
			var announcer = new TextWriterAnnouncer(s => logger.Info(s));
			var assembly = Assembly.GetExecutingAssembly();

			var migrationContext = new RunnerContext(announcer) {
				Namespace = "UCLouvain.AmbulanceSystem.Server.Migrations"
			};

			var options = new MigrationOptions { PreviewOnly = false, Timeout = 60 };
			// var factory = new MonoSQLiteProcessorFactory();
			var factory = new PostgresProcessorFactory();

			using (var processor = factory.Create(connectionString, announcer, options)) {
				var runner = new MigrationRunner(assembly, migrationContext, processor);
				runner.MigrateUp(true);
			}
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			try {
				StartServer();
			} catch (Exception e) {
				logger.Error("Server crash, restart the server");
				logger.Error(e.Message);
				logger.Error(e.StackTrace);
				Thread.Sleep(TimeSpan.FromSeconds(1));
				Main(args);
			}

			return;
		}

		static void StartServer()
		{
			var connectionString = ConfigurationManager.ConnectionStrings["postgres"].ConnectionString;

			//if (!File.Exists (@"database.sqlite")) {

			logger.Info("Updating the database");
			//SqliteConnection.CreateFile(@"database.sqlite");
			MigrateToLatest(connectionString);
			logger.Info("Database migrated");
			//}

			// var provider = new MonoSQLiteDatabaseProvider ();
			var provider = new PostgreSQLDatabaseProvider();

			var config = DatabaseConfiguration.Build()
										  .UsingConnectionString(connectionString)
										  .UsingProvider(provider)
										  .UsingDefaultMapper<ConventionMapper>();

			// connection.Open();

			//logger.Info (db.ExecuteScalar<int>("select count(*) from ambulances where ambulances.ambulanceId = 'A9';"));

			//var ambulanceRepository = new AmbulanceRepository(db);
			//var hospitalRepository = new HospitalRepository(db);
			//var incidentRepository = new IncidentRepository(db);

			var orchestrator = new Orchestrator(config);

			orchestrator.Start();
		}
	}
}
