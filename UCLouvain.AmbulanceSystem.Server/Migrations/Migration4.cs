using System;
using FluentMigrator;

namespace UCLouvain.AmbulanceSystem.Server.Migrations
{
	[Migration(4)]
	public class Migration4 : Migration
	{
		public Migration4()
		{
		}

		public override void Up()
		{
			Alter.Table("incidents")
			     .AddColumn("unreachable").AsBoolean().WithDefaultValue(false);

			Alter.Table("hospitals")
				 .AddColumn("closed").AsBoolean().WithDefaultValue(false);

			Alter.Table("ambulances")
			     .AddColumn("inTrafficJam").AsBoolean ().WithDefaultValue(false);

			Alter.Table("allocations")
				 .AddColumn("cancelConfirmed").AsBoolean().WithDefaultValue(false);

			Create.Table("configuration")
			      .WithColumn("key").AsString().PrimaryKey ()
			      .WithColumn("value").AsString();
			
		}

		public override void Down()
		{
			throw new NotImplementedException();
		}
	}
}
