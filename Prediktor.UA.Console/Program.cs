using Opc.Ua;
using Opc.Ua.Configuration;
using Prediktor.UA.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
namespace Prediktor.UA.Console
{
	class Program
	{
		static void Main(string[] args)
		{

			//url:"opc.tcp://localhost:4850" usesecurity:true username:user password:MyPass

			var helpAsked = (args.Length == 1 && (args[0].StartsWith("?") || args[0].StartsWith("h")));

			if (args.Length == 0 || helpAsked)
			{
				System.Console.WriteLine($"Usage: \nurl:<opc.tcp://server:port> usesecurity:<true/false> username:<username> password:<password>\nUseSecrity, Username and password are optional\n");
			}

			if (!helpAsked)
			{
				var server = GetServer(args);
				var useSecurity = GetUseSecurity(args);
				var userName = GetUsername(args);
				var password = GetPassword(args);

				//var server = args.Length == 1 ? args[0] : "opc.tcp://localhost:4850";
				//var server = "opc.tcp://NOFREL015.prediktor.no:48020"; // C server
				//var server = "opc.tcp://NOFREL015:48010"; // CPP server
				//var server = "opc.tcp://opcuaserver.com:48010"; // on web
				//var server = "opc.tcp://opcuaserver.com:48484"; // on web

				System.Console.WriteLine($"Target UA Server: '{server}'\n");

				//var nodeId = new Opc.Ua.NodeId(2267);
				var nodeId = new Opc.Ua.NodeId(2255); // NamespaceArray

				try
				{

					if (useSecurity && !string.IsNullOrWhiteSpace(userName))
					{
						ReadUserIdentitySecure(server, nodeId, userName, password);
					}
					else if (useSecurity)
					{
						ReadAnonymouslySecure(server, nodeId);
					}
					else
					{
						ReadAnonynouslyUnsecure(server, nodeId);
					}


					//nodeId = new NodeId("V|Worker2.Sine", 2);
					//System.Console.WriteLine("Secure: History Read node: " + nodeId.ToString());
					//ReadHistoryRawDataAnonymouslySecure(server, nodeId);
				}
				catch (Exception e)
				{
					System.Console.WriteLine(e.ToString());
				}
			}
			System.Console.WriteLine("\nPress any key to exit");

			System.Console.ReadKey();
		}

		private static string GetPassword(string[] args)
		{
			var targetKey = "password:";
			var value = args.FirstOrDefault(a => a.StartsWith(targetKey, StringComparison.InvariantCultureIgnoreCase));
			if (value != null)
				return value.Substring(targetKey.Length);

			return string.Empty;
		}

		private static string GetUsername(string[] args)
		{
			var targetKey = "username:";
			var value = args.FirstOrDefault(a => a.StartsWith(targetKey, StringComparison.InvariantCultureIgnoreCase));
			if (value != null)
				return value.Substring(targetKey.Length);

			return string.Empty;
		}

		private static bool GetUseSecurity(string[] args)
		{
			var targetKey = "usesecurity:";
			var value = args.FirstOrDefault(a => a.StartsWith(targetKey, StringComparison.InvariantCultureIgnoreCase));
			if (value != null)
			{
				var boolVs = value.Substring(targetKey.Length);
				if(bool.TryParse(boolVs, out bool result))
				{
					return result;
				}
				else
				{
					System.Console.WriteLine($"Error: Could not parse '{boolVs}' as a boolean. No security will be used");
				}
			}

			System.Console.WriteLine("No security will be used");

			return false;
		}

		private static string GetServer(string[] args)
		{
			var targetKey = "url:";
			var value = args.FirstOrDefault(a => a.StartsWith(targetKey, StringComparison.InvariantCultureIgnoreCase));
			if (value != null)
				return value.Substring(targetKey.Length);

			return "opc.tcp://localhost:4850";
		}

		private static void ReadAnonynouslyUnsecure(string server, Opc.Ua.NodeId nodeId)
		{
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", false);
			var sessionFactory = new SessionFactory(cert => true);
			using (var session = sessionFactory.CreateAnonymously(server, "unsecuresession", false, false, appConfig))
			{
				System.Console.WriteLine("\nUnsecure: Reading value (attrib = 13) node: " + nodeId.ToString());

				var nodes = new Opc.Ua.ReadValueIdCollection();
				nodes.Add(new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = Opc.Ua.Attributes.Value });
				var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes, 
					out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
				var r = response.ServiceResult;
				if (Opc.Ua.StatusCode.IsGood(r))
				{
					if (Opc.Ua.StatusCode.IsGood(results[0].StatusCode))
					{
						System.Console.WriteLine("Value is: " + results[0].Value);
						if (results[0].Value is string[] sArr)
						{
							foreach (var s in sArr)
								System.Console.WriteLine(s);
						}
					}
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
			var sessionFactory = new SessionFactory(ValidateCert);
			using (var session = sessionFactory.CreateSession(server, "securesession", new UserIdentity(new AnonymousIdentityToken()), true, false, appConfig))
			{
				System.Console.WriteLine($"\nSecure/Anonymous: Reading value (attrib = 13) node: '{nodeId}'\n");

				var nodes = new Opc.Ua.ReadValueIdCollection();
				nodes.Add(new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = Opc.Ua.Attributes.Value });
				var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes, out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
				var r = response.ServiceResult;
				if (Opc.Ua.StatusCode.IsGood(r))
				{
					if (Opc.Ua.StatusCode.IsGood(results[0].StatusCode))
					{
						System.Console.WriteLine("Value is: " + results[0].Value);

						if (results[0].Value is string[] sArr)
						{
							foreach (var s in sArr)
								System.Console.WriteLine(s);
						}
					}
					else
						System.Console.WriteLine("Error: " + results[0].StatusCode);
				}
				else
					System.Console.WriteLine("Error: " + r);

			}
		}

		private static void ReadUserIdentitySecure(string server, Opc.Ua.NodeId nodeId, string username, string password)
		{
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", true);
			var sessionFactory = new SessionFactory(ValidateCert);
			var userIdentity = new UserIdentity(username, new System.Net.NetworkCredential(string.Empty, password).Password);
			using (var session = sessionFactory.CreateSession(server, "securesession", userIdentity, true, false, appConfig))
			{
				System.Console.WriteLine($"\nSecure/UserIdentity: Reading value (attrib = 13) node: '{nodeId}'\n");

				var nodes = new Opc.Ua.ReadValueIdCollection();
				nodes.Add(new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = Opc.Ua.Attributes.Value });
				var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes, out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
				var r = response.ServiceResult;
				if (Opc.Ua.StatusCode.IsGood(r))
				{
					if (Opc.Ua.StatusCode.IsGood(results[0].StatusCode))
					{
						System.Console.WriteLine("Value is: " + results[0].Value);

						if (results[0].Value is string[] sArr)
						{
							foreach (var s in sArr)
								System.Console.WriteLine(s);
						}
					}
					else
						System.Console.WriteLine("Error: " + results[0].StatusCode);
				}
				else
					System.Console.WriteLine("Error: " + r);

			}
		}

		private static bool ValidateCert(X509Certificate2 cert)
		{
			if (cert != null)
			{
				System.Console.WriteLine("/*** Server Cerfificate: ");
				System.Console.WriteLine(cert.ToString());

				System.Console.WriteLine("Content Type: '{0}'{1}", X509Certificate2.GetCertContentType(cert.RawData), Environment.NewLine);
				System.Console.WriteLine("Friendly Name: '{0}'{1}", cert.FriendlyName, Environment.NewLine);
				System.Console.WriteLine("Certificate Verified?: '{0}'{1}", cert.Verify(), Environment.NewLine);
				System.Console.WriteLine("Simple Name: '{0}'{1}", cert.GetNameInfo(X509NameType.SimpleName, true), Environment.NewLine);
				System.Console.WriteLine("Signature Algorithm: '{0}'{1}", cert.SignatureAlgorithm.FriendlyName, Environment.NewLine);
				System.Console.WriteLine("Public Key: '{0}'{1}", cert.PublicKey.Key.ToXmlString(false), Environment.NewLine);
				System.Console.WriteLine("Certificate Archived?: '{0}'{1}", cert.Archived, Environment.NewLine);
				System.Console.WriteLine("Length of Raw Data: '{0}'{1}", cert.RawData.Length, Environment.NewLine);

				System.Console.WriteLine("***/");

				//StoreCertInTrusted(cert);
			}
			else
				System.Console.WriteLine("Error: ValidateCert received empty server certificate!");

			return true;
		}

		private static void StoreCertInTrusted(X509Certificate2 cert)
		{
			string filePath = string.Empty;

			try
			{
				var certFileName = $"{cert.GetNameInfo(X509NameType.SimpleName, true)} [{cert.Thumbprint}].der";
				//File.WriteAllBytes($"testcertificates\\pki\\trusted\\{certFileName}", Encoding.ASCII.GetBytes(serializedCert));
				filePath = $"C:\\Git\\Apis\\Source\\Shared\\UA\\UA.Net\\Prediktor.UA.Console\\Prediktor.UA.Console\\testcertificates\\pki\\trusted\\{certFileName}";

				if (File.Exists(filePath))
					File.Delete(filePath);

				File.WriteAllBytes(filePath, cert.Export(X509ContentType.Cert));
			}
			catch(Exception e)
			{
				System.Console.WriteLine($"Error: Store server cert file '{filePath}' failed:");
				System.Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// Export a certificate to a PEM format string
		/// </summary>
		/// <param name="cert">The certificate to export</param>
		/// <returns>A PEM encoded string</returns>
		public static string ExportToDer(X509Certificate cert)
		{
			StringBuilder builder = new StringBuilder();

			builder.AppendLine("-----BEGIN CERTIFICATE-----");
			builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
			builder.AppendLine("-----END CERTIFICATE-----");

			return builder.ToString();
		}

		private static void ReadHistoryRawDataAnonymouslySecure(string server, Opc.Ua.NodeId nodeId)
		{
			HistoryReadResultCollection results;
			DiagnosticInfoCollection diag;
			var fact = new ApplicationConfigurationFactory();
			var appConfig = fact.LoadFromFile("uaconfig.xml", true);
			var sessionFactory = new SessionFactory(cert => true);
			using (var session = sessionFactory.CreateSession(server, "secureanonymous", new UserIdentity(new AnonymousIdentityToken()), true, false, appConfig))
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
