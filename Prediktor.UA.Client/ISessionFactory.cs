using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="reverseConnect">Wether the session should be established through reverse connect. 
        /// Ie. wether the endpoint defines a client uri the server should try to connect to, or wether the endpoint 
        /// defines an endpoint on the server that the client should attempt to connect to</param>
        /// <returns>A session object if successful, or null if the method fails</returns>
        ISession CreateAnonymously(string endpointURL, string sessionName, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig);

        /// <summary>
        /// Connects to endPointUrl.
        /// 
        /// If user is not anonymous, useSecurity must be true.
        /// 
        /// If security is used, a certificate must be present. Where the certificate is stored, is defined in the ApplicationConfiguration. 
        /// The ApplicationConfiguration is usually loaded from a config file.
        /// 
        /// The certificate is found by comparing subject names of the certificates in the certificate store (e.g. a directory) 
        /// to the subject name defined in the ApplicationConfiguration.
        /// 
        /// Which security policy (algorithm) and message encryption to use is decided by finding the "most secure" alternative of the server.
        /// 
        /// </summary>
        /// <param name="endpointURL">The url to connect to</param>
        /// <param name="sessionName">The name of the session</param>
        /// <param name="user">The user identity associated with the session</param>
        /// <param name="useSecurity">True if security is used</param>
        /// <param name="applicationConfig">The configuration for the connection</param>
        /// <param name="reverseConnect">Wether the session should be established through reverse connect. 
        /// Ie. wether the endpoint defines a client uri the server should try to connect to, or wether the endpoint 
        /// defines an endpoint on the server that the client should attempt to connect to</param>
        /// <returns>A session object if successful, or null if the method fails</returns>
        ISession CreateSession(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig);


        /// <summary>
        /// Connects to endPointUrl.
        /// 
        /// If user is not anonymous, useSecurity must be true.
        /// 
        /// If security is used, a certificate must be present. Where the certificate is stored, is defined in the ApplicationConfiguration. 
        /// The ApplicationConfiguration is usually loaded from a config file.
        /// 
        /// The certificate is found by comparing subject names of the certificates in the certificate store (e.g. a directory) 
        /// to the subject name defined in the ApplicationConfiguration.
        /// 
        /// Which security policy (algorithm) and message encryption to use is decided by finding the "most secure" alternative of the server.
        /// 
        /// </summary>
        /// <param name="endpointURL">The url to connect to</param>
        /// <param name="sessionName">The name of the session</param>
        /// <param name="user">The user identity associated with the session</param>
        /// <param name="useSecurity">True if security is used</param>
        /// <param name="operationTimeout">Timeout when selecting endpoint</param>
        /// <param name="sessionTimeout">How long the session will be kept alive if idle</param>
        /// <param name="applicationConfig">The configuration for the connection</param>
        /// <param name="reverseConnect">Wether the session should be established through reverse connect. 
        /// Ie. wether the endpoint defines a client uri the server should try to connect to, or wether the endpoint 
        /// defines an endpoint on the server that the client should attempt to connect to</param>
        /// <returns>A session object if successful, or null if the method fails</returns>

        ISession CreateSession(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, int operationTimeout, uint sessionTimeout, bool reverseConnect, ApplicationConfiguration applicationConfig);

        /// <summary>
        /// Creates a session without user identity.
        /// </summary>
        /// <param name="endpointURL">The url to connect to</param>
        /// <param name="sessionName">The name of the session</param>
        /// <param name="useSecurity">True if security is used</param>
        /// <param name="applicationConfig">The configuration for the connection</param>
        /// <param name="reverseConnect">Wether the session should be established through reverse connect. 
        /// Ie. wether the endpoint defines a client uri the server should try to connect to, or wether the endpoint 
        /// defines an endpoint on the server that the client should attempt to connect to</param>
        /// <returns>A session object if successful, or null if the method fails</returns>
        Task<ISession> CreateAnonymouslyAsync(string endpointURL, string sessionName, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig);

        /// <summary>
        /// Connects to endPointUrl.
        /// 
        /// If user is not anonymous, useSecurity must be true.
        /// 
        /// If security is used, a certificate must be present. Where the certificate is stored, is defined in the ApplicationConfiguration. 
        /// The ApplicationConfiguration is usually loaded from a config file.
        /// 
        /// The certificate is found by comparing subject names of the certificates in the certificate store (e.g. a directory) 
        /// to the subject name defined in the ApplicationConfiguration.
        /// 
        /// Which security policy (algorithm) and message encryption to use is decided by finding the "most secure" alternative of the server.
        /// 
        /// </summary>
        /// <param name="endpointURL">The url to connect to</param>
        /// <param name="sessionName">The name of the session</param>
        /// <param name="user">The user identity associated with the session</param>
        /// <param name="useSecurity">True if security is used</param>
        /// <param name="applicationConfig">The configuration for the connection</param>
        /// <param name="reverseConnect">Wether the session should be established through reverse connect. 
        /// Ie. wether the endpoint defines a client uri the server should try to connect to, or wether the endpoint 
        /// defines an endpoint on the server that the client should attempt to connect to</param>
        /// <returns>A session object if successful, or null if the method fails</returns>
        Task<ISession> CreateSessionAsync(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig);


        /// <summary>
        /// Connects to endPointUrl.
        /// 
        /// If user is not anonymous, useSecurity must be true.
        /// 
        /// If security is used, a certificate must be present. Where the certificate is stored, is defined in the ApplicationConfiguration. 
        /// The ApplicationConfiguration is usually loaded from a config file.
        /// 
        /// The certificate is found by comparing subject names of the certificates in the certificate store (e.g. a directory) 
        /// to the subject name defined in the ApplicationConfiguration.
        /// 
        /// Which security policy (algorithm) and message encryption to use is decided by finding the "most secure" alternative of the server.
        /// 
        /// </summary>
        /// <param name="endpointURL">The url to connect to</param>
        /// <param name="sessionName">The name of the session</param>
        /// <param name="user">The user identity associated with the session</param>
        /// <param name="useSecurity">True if security is used</param>
        /// <param name="operationTimeout">Timeout when selecting endpoint</param>
        /// <param name="sessionTimeout">How long the session will be kept alive if idle</param>
        /// <param name="applicationConfig">The configuration for the connection</param>
        /// <param name="reverseConnect">Wether the session should be established through reverse connect. 
        /// Ie. wether the endpoint defines a client uri the server should try to connect to, or wether the endpoint 
        /// defines an endpoint on the server that the client should attempt to connect to</param>
        /// <returns>A session object if successful, or null if the method fails</returns>

        Task<ISession> CreateSessionAsync(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, int operationTimeout, uint sessionTimeout, bool reverseConnect, ApplicationConfiguration applicationConfig);

        /// <summary>
        /// Connects to endpointDescription.
        /// 
        /// If user is not anonymous, useSecurity must be true.
        /// 
        /// If security is used, a certificate must be present. Where the certificate is stored, is defined in the ApplicationConfiguration. 
        /// The ApplicationConfiguration is usually loaded from a config file.
        /// 
        /// The certificate is found by comparing subject names of the certificates in the certificate store (e.g. a directory) 
        /// to the subject name defined in the ApplicationConfiguration.
        /// </summary>
        /// <param name="endpointDescription"></param>
        /// <param name="sessionName"></param>
        /// <param name="user"></param>
        /// <param name="useSecurity"></param>
        /// <param name="sessionTimeout"></param>
        /// <param name="applicationConfig"></param>
        /// <returns></returns>
        ISession CreateSession(EndpointDescription endpointDescription, string sessionName, IUserIdentity user, bool useSecurity, uint sessionTimeout, ApplicationConfiguration applicationConfig);
    }
}
