using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
	public class DeployAllocatorMessage : Message
	{
		public string Allocator {
			get;
			set;
		}

		public DeployAllocatorMessage()
		{
		}

		public DeployAllocatorMessage(string allocator)
		{
			Allocator = allocator;
		}
	}
}
