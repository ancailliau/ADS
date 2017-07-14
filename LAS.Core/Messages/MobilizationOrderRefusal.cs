using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
    public class MobilizationOrderRefusal : Message
    {
        public string AmbulanceIdentifier {
            get;
            set;
        }

        public int AllocationId {
            get;
            set;
        }

        public MobilizationOrderRefusal()
        {
        }

        public MobilizationOrderRefusal(string ambulanceIdentifier, int incidentIdentifier)
        {
            AmbulanceIdentifier = ambulanceIdentifier;
            AllocationId = incidentIdentifier;
        }

        public override string ToString()
        {
            return string.Format("[MobilizationOrderRefusal: AmbulanceIdentifier={0}, AllocationId={1}]", AmbulanceIdentifier, AllocationId);
        }
    }
}