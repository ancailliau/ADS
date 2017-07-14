using System;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Text;

namespace LAS.Core.Messages
{
	[XmlInclude(typeof(AmbulanceStatusMessage))]
	[XmlInclude(typeof(MessageAck))]
	[XmlInclude(typeof(PositionMessage))]
	[XmlInclude(typeof(RegisterAmbulanceMessage))]
	[XmlInclude(typeof(MobilizationMessage))]
	[XmlInclude(typeof(IncidentForm))]
	[XmlInclude(typeof(MobilizationOrderRefusal))]
	[XmlInclude(typeof(InTrafficJamMessage))]
	[XmlInclude(typeof(NotInTrafficJamMessage))]
	[XmlInclude(typeof(DestinationUnreachableMessage))]
	[XmlInclude(typeof(MobilizationCancelled))]
	[XmlInclude(typeof(DeployAllocatorMessage))]
	public abstract class Message
	{
		public string MessageId {
			get;
			set;
		}

		public Message()
		{
			MessageId = Guid.NewGuid().ToString ();
		}

		public static string GetXML(Message o)
		{
			if (o == null)
				throw new ArgumentNullException(nameof(o));

			var settings = new XmlWriterSettings();
			settings.Encoding = Encoding.UTF32;
			// settings.OmitXmlDeclaration = true;

			var ser = new XmlSerializer(typeof(Message));

			using (var writer = new MemoryStream ()) {
				using (var xmlWriter = XmlWriter.Create (writer, settings)) {
					ser.Serialize(xmlWriter, o);

					writer.Position = 0;
					using (var sr = new StreamReader(writer)) {
						return sr.ReadToEnd ();
					}
				}
			}
		}

		public static Message FromXML(string o)
		{
			if (o == null)
				throw new ArgumentNullException(nameof(o));

			var ser = new XmlSerializer(typeof(Message));

			var memStream = new MemoryStream(Encoding.UTF32.GetBytes(o));

			return (Message) ser.Deserialize(memStream);
		}
	}
}
