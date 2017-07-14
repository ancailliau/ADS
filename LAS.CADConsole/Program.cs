﻿using System;
using System.Net.Http;
using Newtonsoft.Json;
using UCLouvain.AmbulanceSystem.Core.Messages;
using UCLouvain.AmbulanceSystem.Core.Utils;
using System.Dynamic;
using Newtonsoft.Json.Linq;

namespace UCLouvain.AmbulanceSystem.CADConsole
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			var sender = new RabbitMQMessageSender();

			// bounds for the map
			var minlat = 50.645f;
			var minlon = 3.981f;
			var maxlat = 51.053f;
			var maxlon = 4.761f;

			// Ixelles / WB
			maxlat = 50.836502f;
			minlon = 4.358462f;
			minlat = 50.804618f; 
			maxlon = 4.413308f;

			// Smaller big brussels
			//minlat = 50.784339f;
			//minlon = 4.258908f;
			//maxlat = 50.896534f;
			//maxlon = 4.475030f;

			var random = new Random();

			var incidentLat = (float) (minlat + random.NextDouble () * (maxlat - minlat));
			var incidentLong = (float) (minlon + random.NextDouble() * (maxlon - minlon));


			var httpClient = new HttpClient();
            bool _stop = false;

            while (!_stop)
            {
                Console.Write("> ");
                var address = Console.ReadLine().Trim();

                if (address.Equals("quit") | address.Equals("exit"))
                {
                    _stop = true;
                    continue;
                }

                try
                {
                    var httpResult = httpClient.GetAsync("http://nominatim.openstreetmap.org/search?q=" + address + "&format=json&polygon=1&addressdetails=1");

                    var result = httpResult.Result.Content.ReadAsStringAsync().Result;
                    JArray parsed_result = JsonConvert.DeserializeObject<JArray>(result);

                    Console.WriteLine(parsed_result[0]["lat"]);
                    Console.WriteLine(parsed_result[0]["lon"]);

                    var m = new IncidentForm(incidentLat, incidentLong);
                    sender.Send(m, "incident_queue");

                } catch (Exception e) {
                    Console.WriteLine("An error occured ("+e.Message+")");
                }
            }
		}
	}
}
