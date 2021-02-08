using Opc.Ua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Prediktor.UA.Client
{
	public class ApplicationConfigurationFactory
	{
		/// <summary>
		/// Loads the file containing the ApplicationConfiguration. 
		/// </summary>
		/// <param name="file">The file path</param>
		/// <param name="secure">True if connection shall be secure.</param>
		/// <returns></returns>
		public ApplicationConfiguration LoadFromFile(string file, bool secure)
		{
			if (!secure)
			{
				var appConfig = ApplicationConfiguration.LoadWithNoValidation(new FileInfo(file), typeof(ApplicationConfiguration));
				if (appConfig.CertificateValidator == null)
					appConfig.CertificateValidator = new CertificateValidator();
				return appConfig;
			}
			else
			{
				var appConfig = ApplicationConfiguration.Load(new FileInfo(file), ApplicationType.Client, typeof(ApplicationConfiguration)).Result;
				if (appConfig.CertificateValidator == null)
					appConfig.CertificateValidator = new CertificateValidator();
				appConfig.SecurityConfiguration.AddAppCertToTrustedStore = false;
				appConfig.Validate(ApplicationType.Client).Wait();
				return appConfig;
			}

		}
	}
}
