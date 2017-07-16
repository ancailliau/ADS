using System;
using System.Data.Common;

namespace UCLouvain.AmbulanceSystem.Core.Utils
{
	public class MonoSQLiteDatabaseProvider : PetaPoco.SQLiteDatabaseProvider
	{
		public override DbProviderFactory GetFactory()
		{
			return GetFactory("Mono.Data.Sqlite.SqliteFactory, Mono.Data.Sqlite, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756\n");
		}

		public MonoSQLiteDatabaseProvider()
		{
		}
	}
}
