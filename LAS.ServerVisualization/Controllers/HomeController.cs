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
	public class HomeController : Controller
	{
		AmbulanceRepository ambulanceRepository;
		HospitalRepository hospitalRepository;
		IncidentRepository incidentRepository;
		AllocationRepository allocationRepository;

		public HomeController()
		{
			// var provider = new MonoSQLiteDatabaseProvider();
			var provider = new PostgreSQLDatabaseProvider();
			var connectionString = ConfigurationManager.ConnectionStrings["postgres"].ConnectionString;
			var config = DatabaseConfiguration.Build()
										  .UsingConnectionString(connectionString)
										  .UsingProvider(provider)
										  .UsingDefaultMapper<ConventionMapper>();
			var db = new Database(config);

			// connection.Open();

			//logger.Info (db.ExecuteScalar<int>("select count(*) from ambulances where ambulances.ambulanceId = 'A9';"));

			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);
		}

		public ActionResult Index()
		{
			var mvcName = typeof(Controller).Assembly.GetName();
			var isMono = Type.GetType("Mono.Runtime") != null;

			ViewData["Version"] = mvcName.Version.Major + "." + mvcName.Version.Minor;
			ViewData["Runtime"] = isMono ? "Mono" : ".NET";

			return View();
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
			// var js = JsonConvert.SerializeObject(a);
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
