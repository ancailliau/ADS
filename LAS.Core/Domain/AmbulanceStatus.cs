using System;
using System.Xml.Serialization;

namespace LAS.Core.Domain
{
	public enum AmbulanceStatus
	{
		[XmlEnum(Name = "ATSTATION")]   AvailableAtStation = 0,
		[XmlEnum(Name = "LEAVING")]		Leaving = 1, 
		[XmlEnum(Name = "ONSCENE")]		OnScene = 2, 
		[XmlEnum(Name = "TOHOSPITAL")] 	ToHospital = 3, 
		[XmlEnum(Name = "ATHOSPITAL")]	AtHospital = 4, 
		[XmlEnum(Name = "RADIO")]		AvailableRadio = 5, 
		[XmlEnum(Name = "UNAVAILABLE")]	Unavailable = 6
	}

	public enum AmbulancePositionStatus
	{
		[XmlEnum(Name = "UNRESOLVABLE")] Unresolvable = 0,
		[XmlEnum(Name = "RESOLVABLE")]   Resolvable = 1
	}
}
