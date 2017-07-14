using System;
using LAS.Core.Domain;

namespace LAS.Server.Allocators
{
	public interface IAmbulanceAllocator
	{
		void Allocate(Incident i);
	}
}
