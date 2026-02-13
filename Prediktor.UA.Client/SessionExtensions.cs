using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prediktor.UA.Client
{
	public static class SessionExtensions
	{
		/// <summary>
		/// Browse for sub nodes using default settings; Direction forward, Hierarchical references targeting NodeClasses: Variable and Object
		/// </summary>
		/// <param name="s"></param>
		/// <param name="nodeToBrowse"></param>
		/// <returns></returns>
		public static ReferenceDescriptionCollection Browse(this ISession s, NodeId nodeToBrowse) => s.Browse(nodeToBrowse, (uint)NodeClass.Variable | (uint)NodeClass.Object);
		/// <summary>
		/// Browse for sub nodes using default settings; Direction forward, Hierarchical referenses.
		/// Caller must specify target NodeClassMask (i.e. Variable, Object, Method)
		/// </summary>
		/// <param name="s"></param>
		/// <param name="nodeToBrowse"></param>
		/// <param name="nodeClassMask"></param>
		/// <returns></returns>
		public static ReferenceDescriptionCollection Browse(this ISession s, NodeId nodeToBrowse, uint nodeClassMask) => s.Browse(nodeToBrowse, nodeClassMask, 10000u);
		/// <summary>
		/// Browse for sub nodes using default settings; Direction forward, Hierarchical referenses.
		/// Caller must specify target NodeClassMask (i.e. Variable, Object, Method)
		/// </summary>
		/// <param name="session"></param>
		/// <param name="nodeToBrowse"></param>
		/// <param name="nodeClassMask"></param>
		/// <returns></returns>
		public static ReferenceDescriptionCollection Browse(this ISession session, NodeId nodeToBrowse, uint nodeClassMask, uint maxResultsToReturn)
		{
			(var responseHeader, var continuationPoint, var references) = session.BrowseAsync(
				null,
				null,
				nodeToBrowse,
				maxResultsToReturn,
				BrowseDirection.Forward,
				ReferenceTypeIds.HierarchicalReferences,
				true,
				nodeClassMask).Result;

			while (references.Count < maxResultsToReturn && continuationPoint != null)
			{
				(var responseHeaderNext, var continuationPointNext, var referencesNext) = session.BrowseNextAsync(null, false, continuationPoint).Result;

				if ((references.Count + referencesNext.Count) < maxResultsToReturn)
					references.AddRange(referencesNext);
				else
					references.AddRange(referencesNext.Take((int)maxResultsToReturn - references.Count));
				continuationPoint = continuationPointNext;
			}

			if (continuationPoint != null)
			{
				// Release cont point.
				(var _, var __, var ___) = session.BrowseNextAsync(null, true, continuationPoint).Result;
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
		public static Result<object>[] ReadAttributes(this ISession s, NodeId nodeId, uint[] attributeIds) => s.ReadAttributes(new NodeId[]{nodeId}, attributeIds);
		/// <summary>
		/// Read attributes for multiple nodes.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="nodeIds"></param>
		/// <param name="attributeIds"></param>
		/// <returns>Array of Result objects in accordance with the size and order of nodeIds and attributeIds. Test for Success before using the value</returns>
		public static Result<object>[] ReadAttributes(this ISession session, NodeId[] nodeIds, uint[] attributeIds)
		{
			var nodes = new ReadValueIdCollection(Enumerable.Range(0, nodeIds.Length)
				.SelectMany(i => Enumerable.Range(0, attributeIds.Length)
				.Select(j => new ReadValueId() { NodeId = nodeIds[i], AttributeId = attributeIds[j] })));

			var response = session.ReadAsync(null, double.MaxValue, TimestampsToReturn.Both, nodes, CancellationToken.None).Result;
			var serviceResult = response.ResponseHeader.ServiceResult;

			if (StatusCode.IsGood(serviceResult))
			{
				return response.Results.Select(r => StatusCode.IsGood(r.StatusCode) 
					? new Result<object>(r.Value) 
					: new Result<object>(r.StatusCode.Code, r.StatusCode.ToString())).ToArray();
			}
			else
			{
				return response.Results.Select(_ => new Result<object>(serviceResult.Code, serviceResult.ToString())).ToArray();
			}
		}

		public static Result<HistoryEvent>[] ReadEvents(this ISession session, DateTime startTime, DateTime endTime, uint numValuesPerNode, EventFilter eventFilter, NodeId[] nodeIds)
		{
			var hrdc = new HistoryReadValueIdCollection(nodeIds.Select(n => new HistoryReadValueId() { NodeId = n }));
			var readEventDetails = new ReadEventDetails() { StartTime = startTime, EndTime = endTime, NumValuesPerNode = numValuesPerNode, Filter = eventFilter };

			var readResponse = session.HistoryReadAsync(null, new ExtensionObject(readEventDetails), TimestampsToReturn.Source, false, hrdc, CancellationToken.None).Result;
			var continuationPoints = new List<ContPoint<HistoryEvent>>();
			var res = readResponse.Results;
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
		public static DataValue[] ReadNodeValues(this ISession session, NodeId[] nodeIds)
		{
			ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
			nodesToRead.AddRange(nodeIds.Select(n => new ReadValueId()
			{
				NodeId = n,
				AttributeId = Attributes.Value
			}));

			// read the current value
			var response = session.ReadAsync(
				null,
				0,
				TimestampsToReturn.Both,
				nodesToRead,
				CancellationToken.None).Result;

			ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToRead);
			return response.Results.ToArray();
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

		private static void ReadContinuationPoints<T>(ISession session, HistoryReadDetails readDetails, List<ContPoint<T>> contpoints, Action<T, T> merge)
			where T : class
		{
			var cps = contpoints.ToArray();
			var hrdc = new HistoryReadValueIdCollection(cps.Select(cp => new HistoryReadValueId() { NodeId = cp.NodeId, ContinuationPoint = cp.ContinuationPoint }));

			var response = session.HistoryReadAsync(null, new ExtensionObject(readDetails), TimestampsToReturn.Source, false, hrdc, CancellationToken.None).Result;
			var contPoints = new List<ContPoint<T>>();
			var res = response.Results;
			for (int i = 0; i < res.Count; i++)
			{
				if (StatusCode.IsGood(res[i].StatusCode))
				{
					if (ExtensionObject.ToEncodeable(res[i].HistoryData) is T values)
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


		public static Result<HistoryData>[] ReadHistoryRaw(this ISession s, DateTime startTime, DateTime endTime, int numValues, NodeId[] nodeIds, 
			bool returnBounds = false, bool useContinuationPoints = true) => 
			s.ReadHistoryRawAsync(startTime, endTime, numValues, nodeIds, CancellationToken.None, returnBounds, useContinuationPoints).Result;

        public static async Task<Result<HistoryData>[]> ReadHistoryRawAsync
            (this ISession session, DateTime startTime, DateTime endTime, int numValues, NodeId[] nodeIds, CancellationToken ct,
            bool returnBounds = false, bool useContinuationPoints = true)
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

            var result = await session.HistoryReadAsync(requestHeader, readDetailsEx, TimestampsToReturn.Both, false, nodesToRead, ct);

            var historyData = new Result<HistoryData>[result.Results.Count];
            var continuationPoints = new List<ContPoint<HistoryData>>();
            for (int i = 0; i < result.Results.Count; i++)
            {
                if (StatusCode.IsBad(result.Results[i].StatusCode))
                {
                    historyData[i] = new Result<HistoryData>(result.Results[i].StatusCode, string.Format("Error reading node id {0}", nodeIds[i].ToString()));
                }
                else if (result.Results[i].HistoryData != null)
                {
                    HistoryData values = ExtensionObject.ToEncodeable(result.Results[i].HistoryData) as HistoryData;
                    historyData[i] = new Result<HistoryData>(values);
                    if (result.Results[i].ContinuationPoint != null)
                    {
                        continuationPoints.Add(new ContPoint<HistoryData>(i, nodeIds[i], values, result.Results[i].ContinuationPoint));
                    }
                }
                else
                {
                    historyData[i] = new Result<HistoryData>(Opc.Ua.StatusCodes.BadUnknownResponse, string.Format("No history data for node id {0}", nodeIds[i].ToString()));
                }
            }

            while (continuationPoints.Count > 0 && useContinuationPoints)
            {
                var cps = continuationPoints.ToArray();
                continuationPoints.Clear();

                var hrdc = new HistoryReadValueIdCollection();
                hrdc.AddRange(cps.Select(cp => new HistoryReadValueId() { NodeId = cp.NodeId, ContinuationPoint = cp.ContinuationPoint }));

                var res = await session.HistoryReadAsync(requestHeader, readDetailsEx, TimestampsToReturn.Both, false, hrdc, ct);

                for (int i = 0; i < res.Results.Count; i++)
                {
                    if (historyData[cps[i].Index].Success)
                    {
                        if (Opc.Ua.StatusCode.IsGood(res.Results[i].StatusCode))
                        {
                            HistoryData values = ExtensionObject.ToEncodeable(res.Results[i].HistoryData) as HistoryData;
                            if (values != null)
                            {
                                historyData[cps[i].Index].Value.DataValues.AddRange(values.DataValues);

                                if (res.Results[i].ContinuationPoint != null)
                                {
                                    continuationPoints.Add(new ContPoint<HistoryData>(i, nodeIds[i], values, res.Results[i].ContinuationPoint));
                                }
                            }
                        }
                        else
                        {
                            historyData[cps[i].Index] = new Result<HistoryData>(res.Results[i].StatusCode, string.Format("Error reading node id {0}", nodeIds[i].ToString()));
                        }
                    }
                }
            }
            return historyData;
        }

		public static Result<HistoryData>[] ReadHistoryProcessed(this ISession s, DateTime startTime, DateTime endTime, NodeId aggregateNodeId, double processingInterval, NodeId[] nodeIds) =>
			s.ReadHistoryProcessedAsync(startTime, endTime, aggregateNodeId, processingInterval, nodeIds, CancellationToken.None).Result;

        public static async Task<Result<HistoryData>[]> ReadHistoryProcessedAsync(this ISession session, DateTime startTime, DateTime endTime, 
			NodeId aggregateNodeId, double processingInterval, NodeId[] nodeIds, CancellationToken ct)
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

            var result = await session.HistoryReadAsync(
                null,
                new ExtensionObject(readDetails),
                TimestampsToReturn.Both,
                false,
                nodesToRead,
                ct);

            ClientBase.ValidateDiagnosticInfos(result.DiagnosticInfos, nodesToRead);

            var historyData = new Result<HistoryData>[result.Results.Count];

            for (int i = 0; i < result.Results.Count; i++)
            {
                if (StatusCode.IsBad(result.Results[i].StatusCode))
                {
                    historyData[i] = new Result<HistoryData>(result.Results[i].StatusCode, string.Format("Error reading node id {0}", nodeIds[i].ToString()));
                }
                else if (result.Results[i].HistoryData != null)
                {
                    HistoryData values = ExtensionObject.ToEncodeable(result.Results[i].HistoryData) as HistoryData;
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
