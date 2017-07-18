using System;
using FluentMigrator;

namespace UCLouvain.AmbulanceSystem.Server.Migrations
{
	[Migration(5)]
	public class Migration5 : Migration
	{
		public Migration5()
		{
		}

		public override void Up()
		{
			Create.Table("ambulance_stations")
			      .WithColumn("stationId").AsString().PrimaryKey()
			      .WithColumn("name").AsString()
                  .WithColumn("latitude").AsFloat()
                  .WithColumn("longitude").AsFloat()
                  .WithColumn("printerId").AsString();
			
		}

		public override void Down()
		{
            Delete.Table("ambulance_stations");
		}
	}
}
