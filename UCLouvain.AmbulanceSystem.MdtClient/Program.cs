using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UCLouvain.AmbulanceSystem.Core.Domain;
using UCLouvain.AmbulanceSystem.Core.Messages;
using UCLouvain.AmbulanceSystem.MDTClient;
using NLog;
using UCLouvain.AmbulanceSystem.Core.Utils;
using Itinero.LocalGeo;

namespace LASMDTClient
{
	class MainClass
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		static MapService mapService;

		public static void Main(string[] args)
		{
			
		}
	}
}
