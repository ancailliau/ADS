using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using LAS.Core.Utils;
using PetaPoco;
using System.Configuration;
using LAS.Core.Repository;
using Newtonsoft.Json;

namespace LAS.ServerVisualization.Controllers
{
	public class ApiController : Controller
	{
		AmbulanceRepository ambulanceRepository;
		HospitalRepository hospitalRepository;
		IncidentRepository incidentRepository;
		AllocationRepository allocationRepository;

		public ApiController()
		{
			var provider = new PostgreSQLDatabaseProvider();
			var connectionString = ConfigurationManager.ConnectionStrings["postgres"].ConnectionString;
			var config = DatabaseConfiguration.Build()
										  .UsingConnectionString(connectionString)
										  .UsingProvider(provider)
										  .UsingDefaultMapper<ConventionMapper>();
			var db = new Database(config);

			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);
		}

		public JsonResult Ambulances()
		{
			var a = ambulanceRepository.GetAllAmbulances();
			return Json(a, JsonRequestBehavior.AllowGet);
		}

		public JsonResult Incidents()
		{
			var a = incidentRepository.GetAllIncidents();
			return Json(a, JsonRequestBehavior.AllowGet);
		}

		public JsonResult Allocations()
		{
			var a = allocationRepository.GetAllocations();
			return Json(a, JsonRequestBehavior.AllowGet);
		}

		protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
		{
			return new JsonNetResult {
				Data = data,
				ContentType = contentType,
				ContentEncoding = contentEncoding,
				JsonRequestBehavior = behavior
			};
		}
	}
}
