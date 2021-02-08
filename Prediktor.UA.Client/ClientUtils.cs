using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace Prediktor.UA.Client
{
	public class SecurityPolicy
	{
		public const string None = "http://opcfoundation.org/UA/SecurityPolicy#None";
		public const string Basic128Rsa15 = "http://opcfoundation.org/UA/SecurityPolicy#Basic128Rsa15";
		public const string Basic256 = "http://opcfoundation.org/UA/SecurityPolicy#Basic256";
		public const string Basic256Sha256 = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256";
	}

	public class ClientUtils
	{
		public static EndpointDescription SelectEndpoint(string discoveryUrl, MessageSecurityMode messageSecurity, string securityPolicy)
		{
			// needs to add the '/discovery' back onto non-UA TCP URLs.
			if (!discoveryUrl.StartsWith(Utils.UriSchemeOpcTcp))
			{
				if (!discoveryUrl.EndsWith("/discovery"))
				{
					discoveryUrl += "/discovery";
				}
			}

			// parse the selected URL.
			Uri uri = new Uri(discoveryUrl);

			// set a short timeout because this is happening in the drop down event.
			EndpointConfiguration configuration = EndpointConfiguration.Create();
			configuration.OperationTimeout = 5000;

			EndpointDescription selectedEndpoint = null;

			// Connect to the server's discovery endpoint and find the available configuration.
			using (DiscoveryClient client = DiscoveryClient.Create(uri, configuration))
			{
				EndpointDescriptionCollection endpoints = client.GetEndpoints(null);

				// select the best endpoint to use based on the selected URL and the UseSecurity checkbox. 
				for (int ii = 0; ii < endpoints.Count; ii++)
				{
					EndpointDescription endpoint = endpoints[ii];

					// check for a match on the URL scheme.
					if (endpoint.EndpointUrl.StartsWith(uri.Scheme))
					{
						// check if security was requested.
						if (endpoint.SecurityMode == messageSecurity && string.Compare(endpoint.SecurityPolicyUri, securityPolicy, true) == 0)
						{
							if (selectedEndpoint == null || endpoint.SecurityLevel > selectedEndpoint.SecurityLevel)
								selectedEndpoint = endpoint;
						}
					}
				}

				// pick the first available endpoint by default.
				if (selectedEndpoint == null)
				{
					throw new ArgumentException("Could not find endpoint with given security policy and/or message security");
				}
			}

			// if a server is behind a firewall it may return URLs that are not accessible to the client.
			// This problem can be avoided by assuming that the domain in the URL used to call 
			// GetEndpoints can be used to access any of the endpoints. This code makes that conversion.
			// Note that the conversion only makes sense if discovery uses the same protocol as the endpoint.

			Uri endpointUrl = Utils.ParseUri(selectedEndpoint.EndpointUrl);

			if (endpointUrl != null && endpointUrl.Scheme == uri.Scheme)
			{
				UriBuilder builder = new UriBuilder(endpointUrl);
				builder.Host = uri.DnsSafeHost;
				builder.Port = uri.Port;
				selectedEndpoint.EndpointUrl = builder.ToString();
			}

			// return the selected endpoint.
			return selectedEndpoint;
		}
	}
}
