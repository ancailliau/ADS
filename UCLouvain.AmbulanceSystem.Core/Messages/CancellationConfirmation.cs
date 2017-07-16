using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
    public class CancellationConfirmation : Message
    {
        public string AmbulanceIdentifier {
            get;
            set;
        }

        public int AllocationId {
            get;
            set;
        }

        public CancellationConfirmation()
        {
        }

        public CancellationConfirmation(string ambulanceIdentifier, int incidentIdentifier)
        {
            AmbulanceIdentifier = ambulanceIdentifier;
            AllocationId = incidentIdentifier;
        }

        public override string ToString()
        {
            return string.Format("[CancellationConfirmation: AmbulanceIdentifier={0}, AllocationId={1}]", AmbulanceIdentifier, AllocationId);
        }
    }
}
