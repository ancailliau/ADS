using System;
using System.Collections;
using System.Collections.Generic;
using LAS.Core.Domain;
using PetaPoco;

namespace LAS.Core.Repository
{
	public class HospitalRepository
	{
		IDatabase db;

		public HospitalRepository (IDatabase db)
		{
			this.db = db;
		}

		public IEnumerable<Hospital> GetOpenHospitals()
		{
			return db.Query<Hospital>(@"WHERE ""closed"" = FALSE");
		}

		public void Close(string hospitalId)
		{
			db.Execute(@"UPDATE ""hospitals"" SET ""closed"" = TRUE WHERE ""hospitalId"" = @0", hospitalId);
		}

		public Hospital GetHospital(string hospitalId)
		{
			return db.Single<Hospital>(@"WHERE ""hospitalId"" = @0", hospitalId);
		}
	}
}

