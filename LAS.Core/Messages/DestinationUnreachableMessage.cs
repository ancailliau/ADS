using System;
namespace LAS.Core.Messages
{
	public class DestinationUnreachableMessage : Message
	{
		public int AllocationId {
			get;
			set;
		}

		public DestinationUnreachableMessage()
		{
		}

		public DestinationUnreachableMessage(int allocationId)
		{
			AllocationId = allocationId;
		}
	}
}
