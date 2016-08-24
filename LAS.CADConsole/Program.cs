using System;
using LAS.Core.Messages;
using LAS.Core.Utils;

namespace LAS.CADConsole
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			var sender = new MessageSender();

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

			var m = new IncidentForm(incidentLat, incidentLong);
			sender.Send(m);
		}
	}
}
