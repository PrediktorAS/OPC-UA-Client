using Opc.Ua;
using Opc.Ua.Configuration;
using Prediktor.UA.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prediktor.UA.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			var server = args.Length > 0 ? args[0] : "opc.tcp://localhost:4879";
			var nodeId = new Opc.Ua.NodeId(9482, 0);
			//ReadNode(server, nodeId);
			BrowseWithMaxResults(server, nodeId);
			//System.Console.WriteLine("Unsecure: Reading value (attrib = 13) node: " + nodeId.ToString());
			//ReadAnonymouslyUnsecure(server, nodeId);
			//System.Console.WriteLine("Secure: Reading value (attrib = 13) node: " + nodeId.ToString());
			//ReadAnonymouslySecure(server, nodeId);
			//nodeId = new NodeId("V|Worker2.Sine", 2);
			//System.Console.WriteLine("Secure: History Read node: " + nodeId.ToString());
			//ReadHistoryRawDataAnonymouslySecure(server, nodeId);
			var consoleKey = System.Console.ReadKey();
		}




		private static void ReadNode(string server, Opc.Ua.NodeId nodeId)
		{
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", false);
			var sessionFactory = new SessionFactory(cert => true);
			using (var session = sessionFactory.CreateAnonymously(server, "unsecuresession", false, appConfig))
			{
				var res = session.ReadNode(nodeId);

				//var nodes = new Opc.Ua.ReadValueIdCollection();
				//nodes.Add(new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = Opc.Ua.Attributes.Value });
				//var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes,
				//	out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
				//var r = response.ServiceResult;
				//if (Opc.Ua.StatusCode.IsGood(r))
				//{
				//	if (Opc.Ua.StatusCode.IsGood(results[0].StatusCode))
				//		System.Console.WriteLine("Value is: " + results[0].Value);
				//	else
				//		System.Console.WriteLine("Error: " + results[0].StatusCode);
				//}
				//else
				//	System.Console.WriteLine("Error: " + r);
			}

		}


		private static void BrowseWithMaxResults(string server, Opc.Ua.NodeId nodeId)
		{
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", false);
			var sessionFactory = new SessionFactory(cert => true);
			using (var session = sessionFactory.CreateAnonymously(server, "unsecuresession", false, appConfig))
			{
				var res = session.Browse(nodeId, 0xFF, 5);

				//var nodes = new Opc.Ua.ReadValueIdCollection();
				//nodes.Add(new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = Opc.Ua.Attributes.Value });
				//var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes,
				//	out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
				//var r = response.ServiceResult;
				//if (Opc.Ua.StatusCode.IsGood(r))
				//{
				//	if (Opc.Ua.StatusCode.IsGood(results[0].StatusCode))
				//		System.Console.WriteLine("Value is: " + results[0].Value);
				//	else
				//		System.Console.WriteLine("Error: " + results[0].StatusCode);
				//}
				//else
				//	System.Console.WriteLine("Error: " + r);
			}

		}


		private static void ReadAnonymouslyUnsecure(string server, Opc.Ua.NodeId nodeId)
		{
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", false);
			var sessionFactory = new SessionFactory(cert => true);
			using (var session = sessionFactory.CreateAnonymously(server, "unsecuresession", false, appConfig))
			{
				var nodes = new Opc.Ua.ReadValueIdCollection();
				nodes.Add(new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = Opc.Ua.Attributes.Value });
				var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes, 
					out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
				var r = response.ServiceResult;
				if (Opc.Ua.StatusCode.IsGood(r))
				{
					if (Opc.Ua.StatusCode.IsGood(results[0].StatusCode))
						System.Console.WriteLine("Value is: " + results[0].Value);
					else
						System.Console.WriteLine("Error: " + results[0].StatusCode);
				}
				else
					System.Console.WriteLine("Error: " + r);
			}
		}

		private static void ReadAnonymouslySecure(string server, Opc.Ua.NodeId nodeId)
		{
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", true);
			var sessionFactory = new SessionFactory(cert => true);
			using (var session = sessionFactory.CreateSession(server, "securesession", new UserIdentity(new AnonymousIdentityToken()), true, appConfig))
			{
				var nodes = new Opc.Ua.ReadValueIdCollection();
				nodes.Add(new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = Opc.Ua.Attributes.Value });
				var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes, out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
				var r = response.ServiceResult;
				if (Opc.Ua.StatusCode.IsGood(r))
				{
					if (Opc.Ua.StatusCode.IsGood(results[0].StatusCode))
						System.Console.WriteLine("Value is: " + results[0].Value);
					else
						System.Console.WriteLine("Error: " + results[0].StatusCode);
				}
				else
					System.Console.WriteLine("Error: " + r);

			}
		}

		private static void ReadHistoryRawDataAnonymouslySecure(string server, Opc.Ua.NodeId nodeId)
		{
			HistoryReadResultCollection results;
			DiagnosticInfoCollection diag;
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", true);
			var sessionFactory = new SessionFactory(cert => true);
			using (var session = sessionFactory.CreateSession(server, "secureanonymous", new UserIdentity(new AnonymousIdentityToken()), true, appConfig))
			{
				var hrdc = new HistoryReadValueIdCollection(new[] { new HistoryReadValueId() { NodeId = nodeId } });
				var read = GetRawDataDetails(DateTime.Now.AddMinutes(-10), DateTime.Now, 2);
				var response = session.HistoryRead(null, new ExtensionObject(read), TimestampsToReturn.Source, false, hrdc, out results, out diag);
				var r = response.ServiceResult;
				if (Opc.Ua.StatusCode.IsGood(r))
				{
					if (StatusCode.IsGood(results[0].StatusCode))
					{
						HistoryData data = ExtensionObject.ToEncodeable(results[0].HistoryData) as HistoryData;
						if (data != null && data.DataValues != null)
						{
							for (int i = 0; i < data.DataValues.Count; i++)
								System.Console.WriteLine("Value is: " + data.DataValues[i].Value + " ---- Timestamp: " + data.DataValues[i].SourceTimestamp);

						}
						var contPoints = results[0].ContinuationPoint;
						HistoryReadResultCollection contres;
						while (contPoints != null)
						{
							hrdc = new HistoryReadValueIdCollection(new[] { new HistoryReadValueId() { NodeId = nodeId, ContinuationPoint = contPoints } });
							session.HistoryRead(null, new ExtensionObject(read), TimestampsToReturn.Source, false, hrdc, out contres, out diag);
							if (Opc.Ua.StatusCode.IsGood(contres[0].StatusCode))
							{
								data = ExtensionObject.ToEncodeable(contres[0].HistoryData) as HistoryData;
								if (data != null && data.DataValues != null)
								{
									for (int i = 0; i < data.DataValues.Count; i++)
										System.Console.WriteLine("Value is: " + data.DataValues[i].Value + " ---- Timestamp: " + data.DataValues[i].SourceTimestamp);
								}
								contPoints = contres[0].ContinuationPoint;
							}
						}
					}
					else
						System.Console.WriteLine("Error: " + results[0].StatusCode);
				}
				else
					System.Console.WriteLine("Error: " + r);
			}
		}

		/// <summary>
		/// Gets HistoryReadDetails for reading raw data.
		/// </summary>
		/// <param name="startTime"></param>
		/// <param name="endTime"></param>
		/// <param name="numValues"></param>
		/// <returns></returns>
		private static HistoryReadDetails GetRawDataDetails(DateTime startTime, DateTime endTime, int numValues)
		{
			return new ReadRawModifiedDetails()
			{
				StartTime = startTime, 
				EndTime = endTime,
				NumValuesPerNode = (uint)numValues, 
				ReturnBounds = false, 
				IsReadModified = false 
			};
		}

		/// <summary>
		/// Gets HistoryReadDetails for reading raw data.
		/// </summary>
		/// <param name="startTime"></param>
		/// <param name="endTime"></param>
		/// <param name="aggregateNodeId"></param>
		/// <param name="resampleInterval"></param>
		/// <returns></returns>
		private static HistoryReadDetails GetProcessedDetails(DateTime startTime, DateTime endTime, uint aggregateNodeId, double resampleInterval)
		{
			return new ReadProcessedDetails()
			{
				StartTime = startTime,
				EndTime = endTime,
				AggregateType = new NodeIdCollection() { new NodeId(aggregateNodeId) },
				ProcessingInterval = ((double)resampleInterval) * 1000
			};
		}
	}
}
