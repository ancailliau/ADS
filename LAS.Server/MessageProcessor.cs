using System;
using System.Collections.Concurrent;
using UCLouvain.AmbulanceSystem.Core.Messages;
using System.Threading;
using NLog;

namespace UCLouvain.AmbulanceSystem.Server
{
	public class MessageProcessor
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		BlockingCollection<Message> collection;

		Thread handle;

		Orchestrator orchestrator;

		public MessageProcessor(Orchestrator o)
		{
			orchestrator = o;
			collection = new BlockingCollection<Message>();

			handle = new Thread(Start);
			handle.Start();
		}

		public void AddToProcessingQueue(Message m)
		{
			collection.Add(m);
		}

		public void Start()
		{
			foreach (var m in collection.GetConsumingEnumerable()) {
				orchestrator.Process(m);
			}
		}
	}
}
