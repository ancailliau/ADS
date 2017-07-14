using System;
using FluentMigrator;

namespace UCLouvain.AmbulanceSystem.Server.Migrations
{
	[Migration(3)]
	public class Migration3 : Migration
	{
		public Migration3()
		{
		}

		public override void Up()
		{
			Delete.PrimaryKey("allocation_pk").FromTable("allocations");

			Alter.Table("allocations")
			     .AddColumn("allocationId").AsInt32 ().PrimaryKey ().Identity ();
		}

		public override void Down()
		{
			throw new NotImplementedException();
		}
	}
}
