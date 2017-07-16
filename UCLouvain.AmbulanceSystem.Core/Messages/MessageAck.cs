using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
	public class MessageAck : Message
	{
		public string AckMessageId {
			get;
			set;
		}

		public MessageAck() : base ()
		{
		}

		public MessageAck(string messageId) : base ()
		{
			MessageId = messageId;
		}

		public override string ToString()
		{
			return string.Format("[MessageAck: AckMessageId={0}]", AckMessageId);
		}
	}
}
