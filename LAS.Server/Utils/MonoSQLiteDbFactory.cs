using System;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SQLite;

namespace LAS.Server.Utils
{
	public class MonoSQLiteDbFactory : ReflectionBasedDbFactory
	{
		public MonoSQLiteDbFactory()
			: base("Mono.Data.Sqlite", "Mono.Data.Sqlite.SqliteFactory")
		{
		}
	}

	
}
