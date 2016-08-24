using System;
using FluentMigrator;

namespace LAS.Server.Migrations
{
	[Migration(2)]
	public class Migration2 : Migration
	{
		public Migration2()
		{
		}

		public override void Up()
		{
			Alter.Table("allocations")
			     .AddColumn("cancelled").AsBoolean().WithDefaultValue(false)
			     .AddColumn("refused").AsBoolean ().WithDefaultValue(false);
		}

		public override void Down()
		{
			Delete.Column("cancelled").FromTable("allocations");
			Delete.Column("refused").FromTable("allocations");
		}
	}
}
