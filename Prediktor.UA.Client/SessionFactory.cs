using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Prediktor.UA.Client
{
	public class SessionFactory : ISessionFactory
	{
        //private bool _autoAccept = false;
        private Func<System.Security.Cryptography.X509Certificates.X509Certificate2, bool> _validateCertificate;

        public SessionFactory(Func<System.Security.Cryptography.X509Certificates.X509Certificate2, bool> validateCertificate)
        {
            _validateCertificate = validateCertificate;
        }


        /// <summary>
        /// Connects anonymously to endpointURL. Messages are not encrypted and no certificate is needed.
        ///
        /// If security is used, a certificate must be present. Where the certificate is stored, is defined in the ApplicationConfiguration. 
        /// The ApplicationConfiguration is usually loaded from a config file.
        /// </summary>
        /// <param name="endpointURL"></param>
        /// <param name="useSecurity">If true, </param>
        public Session CreateAnonymously(string endpointURL, string sessionName, bool useSecurity, ApplicationConfiguration applicationConfig)
        {
            return CreateSession(endpointURL, sessionName, new UserIdentity(new AnonymousIdentityToken()), useSecurity, applicationConfig);
        }

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
        /// <param name="endpointURL"></param>
        /// <param name="user"></param>
        /// <param name="useSecurity">If user is not anonymous, security must be used</param>
        public Session CreateSession(string endpointURL, string sessionName, IUserIdentity user, bool useSecurity, ApplicationConfiguration applicationConfig)
        {
            if (useSecurity)
            {
                var appInstance = new ApplicationInstance(applicationConfig);
                var checkCertificate = true;
                if (checkCertificate)
                {
                    bool haveAppCertificate = appInstance.CheckApplicationInstanceCertificate(true, 0).Result;
                    if (!haveAppCertificate)
                    {
                        throw new Exception("Application instance certificate invalid!");
                    }
                }
                applicationConfig.ApplicationUri = Utils.GetApplicationUriFromCertificate(applicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate);
            }
            var autoAccept = false;
            if (applicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                autoAccept = true;
            }

            applicationConfig.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler((validator, e) => CertificateValidator_CertificateValidation(autoAccept, validator, e));

            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURL, useSecurity, 15000);

            var endpointConfiguration = EndpointConfiguration.Create(applicationConfig);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
            return Session.Create(applicationConfig, endpoint, false, sessionName, 60000,
                user, null).Result;
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
