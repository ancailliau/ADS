using System;
using System.Collections.Generic;
using UCLouvain.AmbulanceSystem.Core.Domain;
using NLog;
using PetaPoco;

namespace UCLouvain.AmbulanceSystem.Core.Repository
{
	public class ConfigurationRepository
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		IDatabase db;

		public ConfigurationRepository(IDatabase db)
		{
			this.db = db;
		}

		public string GetActiveAllocator()
		{
			return db.ExecuteScalar<string>("select value from configuration where key = 'allocator' limit 1;");
		}

		public void UpdateActiveAllocator(string s)
		{
			db.BeginTransaction();
			db.Execute("delete from configuration where key = 'allocator';");
			db.Execute("insert into configuration(key,value) values ('allocator', @0)", s);
			db.CompleteTransaction();
		}

	}
}
