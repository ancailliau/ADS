using System;
namespace LAS.Core.Messages
{
	public class MobilizationCancelled : Message
	{
		public int AllocationId {
			get;
			set;
		}

		public MobilizationCancelled()
		{
		}

		public MobilizationCancelled(int allocationId)
		{
			AllocationId = allocationId;
		}

		public override string ToString()
		{
			return string.Format("[MobilizationCancelled: AllocationId={0}]", AllocationId);
		}
	}
}
