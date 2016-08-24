using System;
using FluentMigrator;
namespace LAS.Server.Migrations
{
	[Migration(1)]
	public class Migration1 : Migration
	{
		public override void Up()
		{
			Create.Table("ambulances")
				  .WithColumn("ambulanceId").AsString().PrimaryKey()
				  .WithColumn("longitude").AsFloat()
				  .WithColumn("latitude").AsFloat()
			      .WithColumn("lastPositionUpdate").AsDateTime()
				  .WithColumn("status").AsInt16()
				  .WithColumn("port").AsInt16();

			Create.Table("hospitals")
				  .WithColumn("hospitalId").AsString().PrimaryKey()
				  .WithColumn("latitude").AsFloat()
				  .WithColumn("longitude").AsFloat()
				  .WithColumn("name").AsString();

			Create.Table("incidents")
			      .WithColumn("incidentId").AsInt32().PrimaryKey().Identity ()
				  .WithColumn("latitude").AsFloat()
				  .WithColumn("longitude").AsFloat()
			      .WithColumn("resolved").AsBoolean ().WithDefaultValue (false);

			Create.Table("allocations")
				  .WithColumn("ambulanceId").AsString()
				  .WithColumn("incidentId").AsInt32()
			      .WithColumn("hospitalId").AsString()
			      .WithColumn("allocationTimestamp").AsDateTime().Nullable ()
				  .WithColumn("mobilizationSentTimestamp").AsDateTime().Nullable()
				  .WithColumn("mobilizationReceivedTimestamp").AsDateTime().Nullable()
				  .WithColumn("mobilizationConfirmedTimestamp").AsDateTime().Nullable();

			Insert.IntoTable("hospitals")
				.Row(new { hospitalId = "HOP1", name = "Centre Hospitalier Universitaire Saint-Pierre", latitude = 50.834957f, longitude = 4.348430f })
				.Row(new { hospitalId = "HOP2", name = "Clinique Edith Cavell", latitude = 50.814250f, longitude = 4.357530f })
				.Row(new { hospitalId = "HOP3", name = "Clinique Saint-Jean Site Botanique", latitude = 50.853853f, longitude = 4.360663f })
				.Row(new { hospitalId = "HOP4", name = "Clinique Saint-Luc", latitude = 50.852575f, longitude = 4.452851f })
				.Row(new { hospitalId = "HOP5", name = "Clinique Sainte-Anne Saint-Remi", latitude = 50.845607f, longitude = 4.317075f })
				.Row(new { hospitalId = "HOP6", name = "Cliniques de l’Europe – site Ste-Elisabeth", latitude = 50.804648f, longitude = 4.367769f })
				.Row(new { hospitalId = "HOP7", name = "Hôpital d'Etterbeek-Ixelles", latitude = 50.825022f, longitude = 4.379507f })
				.Row(new { hospitalId = "HOP8", name = "Hôpital Erasme", latitude = 50.813997f, longitude = 4.259632f })
				//.Row(new { hospitalId = "HOP8", name = "Hôpital Erasme", latitude = 50.8128775f, longitude = 4.2649523f })
				.Row(new { hospitalId = "HOP9", name = "Hôpital Militaire Reine Astrid", latitude = 50.905819f, longitude = 4.390159f })
				//.Row(new { hospitalId = "HOP10", name = "Hôpital Universitaire Des Enfants Reine Fabiola", latitude = 50.8885403f, longitude = 4.3288842f })
				//.Row(new { hospitalId = "HOP11", name = "Institut Jules Bordet", latitude = 50.833261f, longitude = 4.347095f })
				//.Row(new { hospitalId = "HOP12", name = "UZ Brussel", latitude = 50.8872628f, longitude = 4.3089256f })
				.Row(new { hospitalId = "HOP13", name = "Cliniques de l’Europe – site St-Michel", latitude = 50.842471f, longitude = 4.399073f });

			Create.PrimaryKey("allocation_pk")
				  .OnTable("allocations")
			      .Columns(new string[] { "ambulanceId", "incidentId" });

			Create.ForeignKey("allocation_ambulance_fk")
				  .FromTable("allocations").ForeignColumn("ambulanceId")
				  .ToTable("ambulances").PrimaryColumn("ambulanceId");
			      
			Create.ForeignKey("allocation_incident_fk")
				  .FromTable("allocations").ForeignColumn("incidentId")
				  .ToTable("incidents").PrimaryColumn("incidentId");

			Create.ForeignKey("allocation_hospital_fk")
				  .FromTable("allocations").ForeignColumn("hospitalId")
				  .ToTable("hospitals").PrimaryColumn("hospitalId");
		}

		public override void Down()
		{
			Delete.Table("ambulances");
			Delete.Table("hospitals");
			Delete.Table("incidents");
			Delete.Table("allocations");
		}
	}
}
