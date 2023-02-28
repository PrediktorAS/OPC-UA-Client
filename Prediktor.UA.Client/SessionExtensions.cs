using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prediktor.UA.Client
{
	public static class SessionExtensions
	{
		/// <summary>
		/// Browse for sub nodes using default settings; Direction forward, Hierarchical references targeting NodeClasses: Variable and Object
		/// </summary>
		/// <param name="session"></param>
		/// <param name="nodeToBrowse"></param>
		/// <returns></returns>
		public static ReferenceDescriptionCollection Browse(this Session session, NodeId nodeToBrowse)
		{
			session.Browse(
				null,
				null,
				nodeToBrowse,
				10000u,
				BrowseDirection.Forward,
				ReferenceTypeIds.HierarchicalReferences,
				true,
				(uint)NodeClass.Variable | (uint)NodeClass.Object,
				out byte[] continuationPoint,
				out ReferenceDescriptionCollection references);

			while (continuationPoint != null)
			{
				session.BrowseNext(null, false, continuationPoint, out byte[] continuationPointNext, out ReferenceDescriptionCollection referencesNext);
				references.AddRange(referencesNext);
				continuationPoint = continuationPointNext;
			}

			return references;
		}

		/// <summary>
		/// Browse for sub nodes using default settings; Direction forward, Hierarchical referenses.
		/// Caller must specify target NodeClassMask (i.e. Variable, Object, Method)
		/// </summary>
		/// <param name="session"></param>
		/// <param name="nodeToBrowse"></param>
		/// <param name="nodeClassMask"></param>
		/// <returns></returns>
		public static ReferenceDescriptionCollection Browse(this Session session, NodeId nodeToBrowse, uint nodeClassMask)
		{
			session.Browse(
				null,
				null,
				nodeToBrowse,
				10000u,
				BrowseDirection.Forward,
				ReferenceTypeIds.HierarchicalReferences,
				true,
				nodeClassMask,
				out byte[] continuationPoint,
				out ReferenceDescriptionCollection references);

			while (continuationPoint != null)
			{
				session.BrowseNext(null, false, continuationPoint, out byte[] continuationPointNext, out ReferenceDescriptionCollection referencesNext);
				references.AddRange(referencesNext);
				continuationPoint = continuationPointNext;
			}

			return references;
		}



		/// <summary>
		/// Browse for sub nodes using default settings; Direction forward, Hierarchical referenses.
		/// Caller must specify target NodeClassMask (i.e. Variable, Object, Method)
		/// </summary>
		/// <param name="session"></param>
		/// <param name="nodeToBrowse"></param>
		/// <param name="nodeClassMask"></param>
		/// <returns></returns>
		public static ReferenceDescriptionCollection Browse(this Session session, NodeId nodeToBrowse, uint nodeClassMask, uint maxResultsToReturn)
		{
			session.Browse(
				null,
				null,
				nodeToBrowse,
				maxResultsToReturn,
				BrowseDirection.Forward,
				ReferenceTypeIds.HierarchicalReferences,
				true,
				nodeClassMask,
				out byte[] continuationPoint,
				out ReferenceDescriptionCollection references);


			while (references.Count < maxResultsToReturn && continuationPoint != null)
			{
				session.BrowseNext(null, false, continuationPoint, out byte[] continuationPointNext, out ReferenceDescriptionCollection referencesNext);

				if ((references.Count + referencesNext.Count) < maxResultsToReturn)
					references.AddRange(referencesNext);
				else
					references.AddRange(referencesNext.Take((int)maxResultsToReturn - references.Count));
				continuationPoint = continuationPointNext;
			}

			if (continuationPoint != null)
			{
				// Release cont point.
				session.BrowseNext(null, true, continuationPoint, out byte[] continuationPointNext, out ReferenceDescriptionCollection referencesNext);
			}
			return references;
		}




		/// <summary>
		/// Read attributes for a node.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="nodeId"></param>
		/// <param name="attributeIds"></param>
		/// <returns>Array of Result objects in accordance with the size and order of attributeIds. Test for Success before using the value</returns>
		public static Result<object>[] ReadAttributes(this Session session, NodeId nodeId, uint[] attributeIds)
		{
			var res = new Result<object>[attributeIds.Length];
			var nodes = new Opc.Ua.ReadValueIdCollection();
			var readValues = attributeIds.Select(a => new Opc.Ua.ReadValueId() { NodeId = nodeId, AttributeId = a }).ToArray();
			nodes.AddRange(readValues);

			var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes,
				out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
			var serviceResult = response.ServiceResult;

			if (Opc.Ua.StatusCode.IsGood(serviceResult))
			{
				for (int i = 0; i < results.Count; i++)
				{
					if (Opc.Ua.StatusCode.IsGood(results[i].StatusCode))
						res[i] = new Result<object>(results[i].Value);
					else
						res[i] = new Result<object>(results[i].StatusCode.Code, results[i].StatusCode.ToString());
				}
			}
			else
			{
				for (int i = 0; i < results.Count; i++)
					res[i] = new Result<object>(serviceResult.Code, serviceResult.ToString());
			}

			return res;
		}

		/// <summary>
		/// Read attributes for multiple nodes.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="nodeIds"></param>
		/// <param name="attributeIds"></param>
		/// <returns>Array of Result objects in accordance with the size and order of nodeIds and attributeIds. Test for Success before using the value</returns>
		public static Result<object>[] ReadAttributes(this Session session, NodeId[] nodeIds, uint[] attributeIds)
		{
			var res = new Result<object>[nodeIds.Length * attributeIds.Length];
			var nodes = new Opc.Ua.ReadValueIdCollection();

			var readValues = new ReadValueId[nodeIds.Length * attributeIds.Length];

			int index = 0;
			for (int i = 0; i < nodeIds.Length; i++)
			{
				for (int j = 0; j < attributeIds.Length; j++)
				{
					readValues[index] = new Opc.Ua.ReadValueId() { NodeId = nodeIds[i], AttributeId = attributeIds[j] };
					index++;
				}
			}

			nodes.AddRange(readValues);

			var response = session.Read(null, double.MaxValue, Opc.Ua.TimestampsToReturn.Both, nodes,
				out Opc.Ua.DataValueCollection results, out Opc.Ua.DiagnosticInfoCollection diagnosticInfos);
			var serviceResult = response.ServiceResult;

			if (Opc.Ua.StatusCode.IsGood(serviceResult))
			{
				for (int i = 0; i < results.Count; i++)
				{
					if (Opc.Ua.StatusCode.IsGood(results[i].StatusCode))
						res[i] = new Result<object>(results[i].Value);
					else
						res[i] = new Result<object>(results[i].StatusCode.Code, results[i].StatusCode.ToString());
				}
			}
			else
			{
				for (int i = 0; i < results.Count; i++)
					res[i] = new Result<object>(serviceResult.Code, serviceResult.ToString());
			}

			return res;
		}

		public static Result<HistoryEvent>[] ReadEvents(this Session session, DateTime startTime, DateTime endTime, uint numValuesPerNode, EventFilter eventFilter, NodeId[] nodeIds)
		{
			HistoryReadResultCollection res;
			DiagnosticInfoCollection diag;

			var hrdc = new HistoryReadValueIdCollection(nodeIds.Select(n => new HistoryReadValueId() { NodeId = n }));

			var readEventDetails = new ReadEventDetails() { StartTime = startTime, EndTime = endTime, NumValuesPerNode = numValuesPerNode, Filter = eventFilter };

			session.HistoryRead(null, new ExtensionObject(readEventDetails), TimestampsToReturn.Source, false, hrdc, out res, out diag);
			var continuationPoints = new List<ContPoint<HistoryEvent>>();

			var results = new Result<HistoryEvent>[res.Count];
			for (int i = 0; i < res.Count; i++)
			{
				if (StatusCode.IsGood(res[i].StatusCode))
				{
					HistoryEvent data = ExtensionObject.ToEncodeable(res[i].HistoryData) as HistoryEvent;
					if (data != null)
					{
						results[i] = new Result<HistoryEvent>(data);
						if (res[i].ContinuationPoint != null)
						{
							continuationPoints.Add(new ContPoint<HistoryEvent>(i, nodeIds[i], data, res[i].ContinuationPoint));
						}
					}
					else
						results[i] = new Result<HistoryEvent>(StatusCodes.BadUnknownResponse, string.Format("No history data for node id {0}", nodeIds[i].ToString()));
				}
				else
					results[i] = new Result<HistoryEvent>(res[i].StatusCode, string.Format("Error reading node id {0}", nodeIds[i].ToString()));
			}
			if (continuationPoints.Count > 0)
			{
				ReadContinuationPoints(session, readEventDetails, continuationPoints, (data, merge) => data.Events.AddRange(merge.Events));
			}
			return results;
		}



		/// <summary>
		/// Read a value node from server
		/// </summary>
		/// <param name="nodeId">node id</param>
		/// <returns>DataValue</returns>
		public static DataValue[] ReadNodeValues(this Session session, NodeId[] nodeIds)
		{
			ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
			nodesToRead.AddRange(nodeIds.Select(n => new ReadValueId()
			{
				NodeId = n,
				AttributeId = Attributes.Value
			}));


			// read the current value
			session.Read(
				null,
				0,
				TimestampsToReturn.Both,
				nodesToRead,
				out DataValueCollection results,
				out DiagnosticInfoCollection diagnosticInfos);

			//ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);
			return results.ToArray();
		}

		private class ContPoint<T>
		{
			public ContPoint(int index, NodeId nodeId, T data, byte[] continuationPoint)
			{
				Index = index;
				Data = data;
				NodeId = nodeId;
				ContinuationPoint = continuationPoint?.ToArray();
			}
			public int Index { get; }
			public T Data { get; }
			public byte[] ContinuationPoint { get; }

			public NodeId NodeId { get; }
		}

		private static void ReadContinuationPoints<T>(Session session, HistoryReadDetails readDetails, List<ContPoint<T>> contpoints, Action<T, T> merge)
			where T : class
		{
			var cps = contpoints.ToArray();
			var hrdc = new HistoryReadValueIdCollection();
			hrdc.AddRange(cps.Select(cp => new HistoryReadValueId() { NodeId = cp.NodeId, ContinuationPoint = cp.ContinuationPoint }));

			session.HistoryRead(null, new ExtensionObject(readDetails), TimestampsToReturn.Source, false, hrdc, out HistoryReadResultCollection res, out DiagnosticInfoCollection diag);
			var contPoints = new List<ContPoint<T>>();
			for (int i = 0; i < res.Count; i++)
			{
				if (Opc.Ua.StatusCode.IsGood(res[i].StatusCode))
				{
					T values = ExtensionObject.ToEncodeable(res[i].HistoryData) as T;
					if (values != null)
					{
						merge(cps[i].Data, values);

						if (res[i].ContinuationPoint != null)
						{
							contPoints.Add(new ContPoint<T>(i, cps[i].NodeId, cps[i].Data, res[i].ContinuationPoint));
						}
					}
				}
				else
				{
					// TODO: Add logging?
				}
			}
			if (contPoints.Count > 0)
				ReadContinuationPoints<T>(session, readDetails, contPoints, merge);
		}


		public static Result<HistoryData>[] ReadHistoryRaw(this Session session, DateTime startTime, DateTime endTime, int numValues, NodeId[] nodeIds, bool returnBounds = false)
		{
			RequestHeader requestHeader = new RequestHeader();

			HistoryReadValueIdCollection nodesToRead = new HistoryReadValueIdCollection();
			nodesToRead.AddRange(nodeIds.Select(n => new HistoryReadValueId()
			{
				NodeId = n,
			}));

			HistoryReadDetails readDetails = new ReadRawModifiedDetails()
			{
				StartTime = startTime,
				EndTime = endTime,
				NumValuesPerNode = (uint)numValues,
				ReturnBounds = returnBounds,
				IsReadModified = false
			};

			var readDetailsEx = new ExtensionObject(readDetails);

            session.HistoryRead(requestHeader, readDetailsEx, TimestampsToReturn.Both, false, nodesToRead,
				out HistoryReadResultCollection result, out DiagnosticInfoCollection diagnosticInfos);

			var historyData = new Result<HistoryData>[result.Count];
			var continuationPoints = new List<ContPoint<HistoryData>>();
			for (int i = 0; i < result.Count; i++)
			{
				if (StatusCode.IsBad(result[i].StatusCode))
				{
					historyData[i] = new Result<HistoryData>(result[i].StatusCode, string.Format("Error reading node id {0}", nodeIds[i].ToString()));
				}
				else if (result[i].HistoryData != null)
				{
					HistoryData values = ExtensionObject.ToEncodeable(result[i].HistoryData) as HistoryData;
					historyData[i] = new Result<HistoryData>(values);
					if (result[i].ContinuationPoint != null)
					{
						continuationPoints.Add(new ContPoint<HistoryData>(i, nodeIds[i], values, result[i].ContinuationPoint));
					}
				}
				else
				{
					historyData[i] = new Result<HistoryData>(Opc.Ua.StatusCodes.BadUnknownResponse, string.Format("No history data for node id {0}", nodeIds[i].ToString()));
				}
			}

			while (continuationPoints.Count > 0)
			{
                var cps = continuationPoints.ToArray();
                continuationPoints.Clear();

                var hrdc = new HistoryReadValueIdCollection();
                hrdc.AddRange(cps.Select(cp => new HistoryReadValueId() { NodeId = cp.NodeId, ContinuationPoint = cp.ContinuationPoint }));

                session.HistoryRead(null, readDetailsEx, TimestampsToReturn.Source, false, hrdc, out HistoryReadResultCollection res, out DiagnosticInfoCollection diag);

                for (int i = 0; i < res.Count; i++)
                {
					if (historyData[cps[i].Index].Success)
					{
						if (Opc.Ua.StatusCode.IsGood(res[i].StatusCode))
						{
							HistoryData values = ExtensionObject.ToEncodeable(res[i].HistoryData) as HistoryData;
							if (values != null)
							{
								historyData[cps[i].Index].Value.DataValues.AddRange(values.DataValues);

								if (res[i].ContinuationPoint != null)
								{
									continuationPoints.Add(new ContPoint<HistoryData>(i, nodeIds[i], values, res[i].ContinuationPoint));
								}
							}
						}
						else
						{
							historyData[cps[i].Index] = new Result<HistoryData>(res[i].StatusCode, string.Format("Error reading node id {0}", nodeIds[i].ToString()));
						}
					}
                }
            }
            return historyData;
		}

		public static Result<HistoryData>[] ReadHistoryProcessed(this Session session, DateTime startTime, DateTime endTime, NodeId aggregateNodeId, double processingInterval, NodeId[] nodeIds)
		{
			HistoryReadValueIdCollection nodesToRead = new HistoryReadValueIdCollection();
			nodesToRead.AddRange(nodeIds.Select(n => new HistoryReadValueId()
			{
				NodeId = n,
				Processed = true
			}));

			NodeIdCollection aggregateTypes = new NodeIdCollection();
			for (int i = 0; i < nodeIds.Length; i++)
				aggregateTypes.Add(aggregateNodeId);

			ReadProcessedDetails readDetails = new ReadProcessedDetails
			{
				StartTime = startTime,
				EndTime = endTime,
				AggregateType = aggregateTypes,
				ProcessingInterval = processingInterval
			};

			session.HistoryRead(
				null,
				new ExtensionObject(readDetails),
				TimestampsToReturn.Both,
				false,
				nodesToRead,
				out HistoryReadResultCollection result,
				out DiagnosticInfoCollection diagnosticInfos);

			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			var historyData = new Result<HistoryData>[result.Count];

			for (int i = 0; i < result.Count; i++)
			{
				if (StatusCode.IsBad(result[i].StatusCode))
				{
					historyData[i] = new Result<HistoryData>(result[i].StatusCode, string.Format("Error reading node id {0}", nodeIds[i].ToString()));
				}
				else if (result[i].HistoryData != null)
				{
					HistoryData values = ExtensionObject.ToEncodeable(result[i].HistoryData) as HistoryData;
					historyData[i] = new Result<HistoryData>(values);
				}
				else
				{
					historyData[i] = new Result<HistoryData>(Opc.Ua.StatusCodes.BadUnknownResponse, string.Format("No history data for node id {0}", nodeIds[i].ToString()));
				}
			}

			return historyData;
		}
	}
}
