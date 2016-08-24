using System;
using System.Data;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SQLite;

namespace LAS.Server.Utils
{

	public class MonoSQLiteProcessor : SQLiteProcessor
	{
		public MonoSQLiteProcessor(IDbConnection connection, 
		                           IMigrationGenerator generator, 
		                           IAnnouncer announcer, 
		                           IMigrationProcessorOptions options, 
		                           IDbFactory factory)
			: base (connection, generator, announcer, options, factory)
        {
		}
	}
	
}
