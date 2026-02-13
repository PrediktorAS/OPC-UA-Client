using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Prediktor.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prediktor.UA.Client
{
	public class SessionFactory : ISessionFactory
	{
        private Func<System.Security.Cryptography.X509Certificates.X509Certificate2, bool> _validateCertificate;
        private static readonly ITraceLog _log = LogManager.GetLogger(typeof(SessionFactory));
        private Opc.Ua.Client.ISessionFactory _foundationSessionFactory;
        public const int DefaultReverseConnectWaitTimeout = 20000;
        // This method is copied from opcfoundation ua client. The only difference is this version is specifying
        // ReverseConnectStrategy.AnyOnce, in order to accept any serverapplicationuri, serverendpointuri
        private async Task<ITransportWaitingConnection> WaitForConnection(
            ReverseConnectManager reverseManager,
            ReverseConnectClientConfiguration conf,
            Uri endpointUrl,
            string serverUri,
            CancellationToken ct = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<ITransportWaitingConnection>();
            int hashCode = reverseManager.RegisterWaitingConnection(endpointUrl, serverUri,
                (object sender, ConnectionWaitingEventArgs e) => tcs.TrySetResult(e),
                ReverseConnectManager.ReverseConnectStrategy.AnyOnce);

            Func<Task> listenForCancelTaskFnc = async () => {
                if (ct == default(CancellationToken))
                {
                    var waitTimeout = conf.WaitTimeout > 0 ? conf.WaitTimeout : DefaultReverseConnectWaitTimeout;
                    await Task.Delay(waitTimeout).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(-1, ct).ContinueWith(tsk => { }, TaskScheduler.Current).ConfigureAwait(false);
                }
                tcs.TrySetCanceled();
            };

            await Task.WhenAny(new Task[] {
                tcs.Task,
                listenForCancelTaskFnc()
            }).ConfigureAwait(false);

            if (!tcs.Task.IsCompleted || tcs.Task.IsCanceled)
            {
                reverseManager.UnregisterWaitingConnection(hashCode);
                throw new ServiceResultException(StatusCodes.BadTimeout, "Waiting for the reverse connection timed out.");
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        private async Task<ITransportWaitingConnection> GetReverseConnection(ApplicationConfiguration conf, ReverseConnectManager reverseManager, string endpoint)
        {
            _log.DebugFormat("Waiting for reverse connection.");
            ITransportWaitingConnection connection = null;
            try
            {
                var cts = new CancellationTokenSource(conf.ClientConfiguration.ReverseConnect.WaitTimeout);
                // Use our own, slightly modified WaitForConnection method instead of the one in ReverseConnectManager
                connection = await /*_reverseManager.*/WaitForConnection(reverseManager, conf.ClientConfiguration.ReverseConnect, new Uri(endpoint), null, cts.Token);
            }
            catch (Exception e)
            {
                _log.Error($"Unable to establish connection at reverse endpoint {endpoint}", e);
                throw new ServiceResultException(StatusCodes.BadTimeout, $"Failed to establish reverse connection at endpoint {endpoint}", e);
            }
            if (connection == null)
                throw new ServiceResultException(StatusCodes.BadTimeout, $"Waiting for a reverse connection at endpoint {endpoint} timed out.");

            return connection;
        }

        public SessionFactory(Func<System.Security.Cryptography.X509Certificates.X509Certificate2, bool> validateCertificate)
        {
            _validateCertificate = validateCertificate;
            _foundationSessionFactory = new DefaultSessionFactory(ApplicationConfigurationFactory.GetTelemetryContext());
        }


        /// <summary>
        /// Connects anonymously to endpointURL. Messages are not encrypted and no certificate is needed.
        ///
        /// If security is used, a certificate must be present. Where the certificate is stored, is defined in the ApplicationConfiguration. 
        /// The ApplicationConfiguration is usually loaded from a config file.
        /// </summary>
        /// <param name="endpointURL"></param>
        /// <param name="useSecurity">If true, </param>
        public ISession CreateAnonymously(string endpointURL, string sessionName, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig)
        {
            return CreateSession(endpointURL, sessionName, new UserIdentity(new AnonymousIdentityToken()), useSecurity, reverseConnect, applicationConfig);
        }

		public ISession CreateSession(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig)
        {
            return CreateSession(endpointURL, sessionName, user, useSecurity, 15000, 60000, reverseConnect, applicationConfig);
        }

        public ISession CreateSession(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, int operationTimeout, uint sessionTimeout, bool reverseConnect, ApplicationConfiguration applicationConfig)
		{
            bool haveAppCertificate = false;
            if (useSecurity)
            {
                var appInstance = new ApplicationInstance(applicationConfig, ApplicationConfigurationFactory.GetTelemetryContext());
                var checkCertificate = true;
                if (checkCertificate)
                {
                    haveAppCertificate = appInstance.CheckApplicationInstanceCertificatesAsync(true, 0).Result;
                    if (!haveAppCertificate)
                    {
                        throw new Exception("Application instance certificates invalid!");
                    }
                }
                applicationConfig.ApplicationUri = X509Utils.GetApplicationUrisFromCertificate(applicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate).Single();
            }
            var autoAccept = false;
            if (applicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                autoAccept = true;
            }

            applicationConfig.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler((validator, e) => CertificateValidator_CertificateValidation(autoAccept, validator, e));

            var endpointConfiguration = EndpointConfiguration.Create(applicationConfig);
            if (reverseConnect)
			{
                ITransportWaitingConnection connection = null;
                ConfiguredEndpoint endpoint;
                using (var reverseManager = new ReverseConnectManager(ApplicationConfigurationFactory.GetTelemetryContext()))
                {
                    reverseManager.AddEndpoint(new Uri(endpointURL));
                    reverseManager.StartService(applicationConfig);
					var selectedEndpoint = CoreClientUtils.SelectEndpointAsync(applicationConfig, connection, haveAppCertificate && useSecurity, operationTimeout, ApplicationConfigurationFactory.GetTelemetryContext()).Result;
					endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                    try
                    {
                        return _foundationSessionFactory.CreateAsync(applicationConfig,
                            reverseManager,
                            endpoint,
                            false,
                            false,
                            "NSS",
                            sessionTimeout,
                            user,
                            new List<string>()).Result;
                    }
					catch (Exception e)
					{
						_log.Error($"Unable to establish a reverse connection against endpoint {endpointURL}", e);
					}
					/*
					Task<ITransportWaitingConnection> connectionTask;
                    try
                    {
                        connectionTask = GetReverseConnection(applicationConfig, reverseManager, endpointURL);
                        connectionTask.Wait();
                        if (connectionTask.IsCompleted)
                            connection = connectionTask.Result;
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Unable to establish a reverse connection against endpoint {endpointURL}", e);
                    }
                    if (connection == null)
                    {
                        throw new Exception($"Unable to establish a reverse connection against endpoint {endpointURL}");
                    }

                    var selectedEndpoint = CoreClientUtils.SelectEndpointAsync(applicationConfig, connection, haveAppCertificate && useSecurity, operationTimeout, ApplicationConfigurationFactory.GetTelemetryContext()).Result;
                    endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
                    // Must establish new connection after endpoint is selected
                    connectionTask = GetReverseConnection(applicationConfig, reverseManager, endpointURL);
                    connectionTask.Wait();
                    if (connectionTask.IsCompleted)
                        connection = connectionTask.Result;
                    */
				}
				_log.DebugFormat("Creating session for reverse connection endpoint {0}", endpoint.ToString());

                return _foundationSessionFactory.CreateAsync(
                    applicationConfig,
                    (ReverseConnectManager)null,
                    endpoint,
                    false,
                    false,
                    "NSS",
                    sessionTimeout,
                    user,
                    new List<string>()).Result;
            }
            else
			{
                var selectedEndpoint = CoreClientUtils.SelectEndpointAsync(applicationConfig, endpointURL, useSecurity, operationTimeout, ApplicationConfigurationFactory.GetTelemetryContext()).Result;
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                return _foundationSessionFactory.CreateAsync(applicationConfig, endpoint, false, sessionName, sessionTimeout,
                    user, null).Result;
            }
        }

        public ISession CreateSession(EndpointDescription endpointDescription, string sessionName, IUserIdentity user, bool useSecurity, uint sessionTimeout, ApplicationConfiguration applicationConfig)
        {
            bool haveAppCertificate = false;
            if (useSecurity)
            {
                var appInstance = new ApplicationInstance(applicationConfig, ApplicationConfigurationFactory.GetTelemetryContext());
                var checkCertificate = true;
                if (checkCertificate)
                {
                    haveAppCertificate = appInstance.CheckApplicationInstanceCertificatesAsync(true, 0).Result;
                    if (!haveAppCertificate)
                    {
                        throw new Exception("Application instance certificate invalid!");
                    }
                }
                applicationConfig.ApplicationUri = X509Utils.GetApplicationUrisFromCertificate(applicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate).Single();
            }
            var autoAccept = false;
            if (applicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                autoAccept = true;
            }

            applicationConfig.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler((validator, e) => CertificateValidator_CertificateValidation(autoAccept, validator, e));

            var endpointConfiguration = EndpointConfiguration.Create(applicationConfig);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

            return _foundationSessionFactory.CreateAsync(applicationConfig, endpoint, false, sessionName, sessionTimeout,
                user, null).Result;
        }


        public async Task<ISession> CreateAnonymouslyAsync(string endpointURL, string sessionName, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig)
        {
            return await CreateSessionAsync(endpointURL, sessionName, new UserIdentity(new AnonymousIdentityToken()), useSecurity,reverseConnect, applicationConfig);
        }

        public async Task<ISession> CreateSessionAsync(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, bool reverseConnect, ApplicationConfiguration applicationConfig)
		{
            return await CreateSessionAsync(endpointURL, sessionName, user, useSecurity, 15000, 60000, reverseConnect, applicationConfig);
        }

        public async Task<ISession> CreateSessionAsync(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, int operationTimeout, uint sessionTimeout, bool reverseConnect, ApplicationConfiguration applicationConfig)
		{
            bool haveAppCertificate = false;
            if (useSecurity)
            {
                var appInstance = new ApplicationInstance(applicationConfig, ApplicationConfigurationFactory.GetTelemetryContext());
                var checkCertificate = true;
                if (checkCertificate)
                {
                    haveAppCertificate = await appInstance.CheckApplicationInstanceCertificatesAsync(true, 0);
                    if (!haveAppCertificate)
                    {
                        throw new Exception("Application instance certificate invalid!");
                    }
                }
                applicationConfig.ApplicationUri = X509Utils.GetApplicationUrisFromCertificate(applicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate).Single();
            }
            var autoAccept = false;
            if (applicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                autoAccept = true;
            }

            applicationConfig.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler((validator, e) => CertificateValidator_CertificateValidation(autoAccept, validator, e));
            var endpointConfiguration = EndpointConfiguration.Create(applicationConfig);
            if (reverseConnect)
            {
                ITransportWaitingConnection connection = null;
                ConfiguredEndpoint endpoint;
                using (var reverseManager = new ReverseConnectManager(ApplicationConfigurationFactory.GetTelemetryContext()))
                {
                    reverseManager.AddEndpoint(new Uri(endpointURL));
                    reverseManager.StartService(applicationConfig);

                    try
                    {
                        connection = await GetReverseConnection(applicationConfig, reverseManager, endpointURL);
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Unable to establish a reverse connection against endpoint {endpointURL}", e);
                    }
                    if (connection == null)
                    {
                        throw new Exception($"Unable to establish a reverse connection against endpoint {endpointURL}");
                    }
                    var selectedEndpoint = CoreClientUtils.SelectEndpointAsync(applicationConfig, connection, haveAppCertificate && useSecurity, operationTimeout, ApplicationConfigurationFactory.GetTelemetryContext()).Result;
                    endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
                    // Must establish new connection after endpoint is selected
                    connection = await GetReverseConnection(applicationConfig, reverseManager, endpointURL);
                }

                _log.DebugFormat("Creating session for reverse connection endpoint {0}", endpointURL);
                return await _foundationSessionFactory.CreateAsync(
                    applicationConfig,
                    connection,
                    endpoint,
                    false,
                    false,
                    "NSS",
                    sessionTimeout,
                    user,
                    Array.Empty<string>());
            }
            else
            {
                var selectedEndpoint = CoreClientUtils.SelectEndpointAsync(applicationConfig, endpointURL, useSecurity, operationTimeout, ApplicationConfigurationFactory.GetTelemetryContext()).Result;
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                return await _foundationSessionFactory.CreateAsync(applicationConfig, endpoint, false, sessionName, sessionTimeout, user, null);
            }
        }

        //private void Client_KeepAlive(Session sender, KeepAliveEventArgs e)
        //{
        //    if (e.Status != null && ServiceResult.IsNotGood(e.Status))
        //    {
        //        Console.WriteLine("{0} {1}/{2}", e.Status, sender.OutstandingRequestCount, sender.DefunctRequestCount);

        //        if (_reconnectHandler == null)
        //        {
        //            Console.WriteLine("--- RECONNECTING ---");
        //            _reconnectHandler = new SessionReconnectHandler();
        //            _reconnectHandler.BeginReconnect(sender, _reconnectPeriod * 1000, Client_ReconnectComplete);
        //        }
        //    }
        //}

        //private void Client_ReconnectComplete(object sender, EventArgs e)
        //{
        //    // ignore callbacks from discarded objects.
        //    if (!Object.ReferenceEquals(sender, _reconnectHandler))
        //    {
        //        return;
        //    }

        //    _session = _reconnectHandler.Session;
        //    _reconnectHandler.Dispose();
        //    _reconnectHandler = null;
        //}

        private void CertificateValidator_CertificateValidation(bool autoAccept, CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                if (autoAccept)
                    e.Accept = autoAccept;
                else if (_validateCertificate != null)
                    e.Accept = _validateCertificate(e.Certificate);
                else
                    e.Accept = true;
                

                if (e.Accept)
                {
                    Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
                }
                else
                {
                    Console.WriteLine("Rejected Certificate: {0}", e.Certificate.Subject);
                }
            }
        }
    }
}
