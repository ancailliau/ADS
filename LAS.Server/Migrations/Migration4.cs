using System;
using FluentMigrator;

namespace LAS.Server.Migrations
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
			
		}

		public override void Down()
		{
			throw new NotImplementedException();
		}
	}
}
