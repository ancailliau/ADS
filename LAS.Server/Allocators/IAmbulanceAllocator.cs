using System;
using UCLouvain.AmbulanceSystem.Core.Domain;

namespace UCLouvain.AmbulanceSystem.Server.Allocators
{
	public interface IAmbulanceAllocator
	{
		void Allocate(Incident i);
	}
}
