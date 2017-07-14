using System;
namespace LAS.Core.Messages
{
    public class MobilizationConfirmation : Message
    {
        public string AmbulanceIdentifier {
            get;
            set;
        }

        public int AllocationId {
            get;
            set;
        }

        public MobilizationConfirmation()
        {
        }

        public MobilizationConfirmation(string ambulanceIdentifier, int incidentIdentifier)
        {
            AmbulanceIdentifier = ambulanceIdentifier;
            AllocationId = incidentIdentifier;
        }

        public override string ToString()
        {
            return string.Format("[MobilizationConfirmation: AmbulanceIdentifier={0}, AllocationId={1}]", AmbulanceIdentifier, AllocationId);
        }
    }
}
