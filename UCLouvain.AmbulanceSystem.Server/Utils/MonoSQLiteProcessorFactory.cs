using System;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators.SQLite;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SQLite;

namespace UCLouvain.AmbulanceSystem.Server.Utils
{

	public class MonoSQLiteProcessorFactory : MigrationProcessorFactory
	{
		public override IMigrationProcessor Create(string connectionString, 
		                                           IAnnouncer announcer, 
		                                           IMigrationProcessorOptions options)
		{
			var factory = new MonoSQLiteDbFactory();
			var connection = factory.CreateConnection(connectionString);
			return new MonoSQLiteProcessor(connection, 
			                               new SQLiteGenerator(), 
			                               announcer, 
			                               options, 
			                               factory);
		}
	}
}
