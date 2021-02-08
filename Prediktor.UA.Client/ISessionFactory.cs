using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Prediktor.UA.Client
{
    /// <summary>
    /// Factory for creating an OPC UA session.
    /// </summary>
	public interface ISessionFactory
	{
        /// <summary>
        /// Creates a session without user identity.
        /// </summary>
        /// <param name="endpointURL">The url to connect to</param>
        /// <param name="sessionName">The name of the session</param>
        /// <param name="useSecurity">True if security is used</param>
        /// <param name="applicationConfig">The configuration for the connection</param>
        /// <returns></returns>
        Session CreateAnonymously(string endpointURL, string sessionName, bool useSecurity, ApplicationConfiguration applicationConfig);
        /// <summary>
        /// Creates a session with user identity
        /// </summary>
        /// <param name="endpointURL">The url to connect to</param>
        /// <param name="sessionName">The name of the session</param>
        /// <param name="user">The user identity associated with the session</param>
        /// <param name="useSecurity">True if security is used</param>
        /// <param name="applicationConfig">The configuration for the connection</param>
        /// <returns></returns>
        Session CreateSession(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, ApplicationConfiguration applicationConfig);
    }
}
