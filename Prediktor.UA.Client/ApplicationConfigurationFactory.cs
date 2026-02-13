using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Prediktor.UA.Client
{
	public class ApplicationConfigurationFactory
	{
		// TODO : this is a bit of a hack to be able to easily create a ServiceMessageContext using the same telemetry context in all the NodesetUtils classes. Refactor to properly make use of telemetry !
		private static ITelemetryContext _telemetryContext = DefaultTelemetry.Create(lb =>
		{
			// I have no idea how to properly set this up. Just hoping magic stuff will happen 
			//lb.AddProvider(new Log4NetProvider());
			lb.SetMinimumLevel(LogLevel.Information);
		});
		internal static ITelemetryContext GetTelemetryContext() => _telemetryContext;

		/// <summary>
		/// Loads the file containing the ApplicationConfiguration. 
		/// </summary>
		/// <param name="file">The file path</param>
		/// <param name="secure">True if connection shall be secure.</param>
		/// <returns>The application configuration</returns>
		public ApplicationConfiguration LoadFromFile(string file, bool secure)
		{
			if (!secure)
			{
				var appConfig = ApplicationConfiguration.LoadWithNoValidation(new FileInfo(file), typeof(ApplicationConfiguration), GetTelemetryContext());
				if (appConfig.CertificateValidator == null)
					appConfig.CertificateValidator = new CertificateValidator(GetTelemetryContext());
				return appConfig;
			}
			else
			{
				var appConfig = ApplicationConfiguration.LoadAsync(new FileInfo(file), ApplicationType.Client, typeof(ApplicationConfiguration), GetTelemetryContext()).Result;
				if (appConfig.CertificateValidator == null)
					appConfig.CertificateValidator = new CertificateValidator(GetTelemetryContext());
				appConfig.SecurityConfiguration.AddAppCertToTrustedStore = false;
				appConfig.ValidateAsync(ApplicationType.Client).Wait();
				return appConfig;
			}
		}
		/// <summary>
		/// Asyncronously load the file containing the ApplicationConfiguration. 
		/// </summary>
		/// <param name="file">The file path</param>
		/// <param name="secure">True if connection shall be secure.</param>
		/// <returns>The application configuration</returns>
		public async Task<ApplicationConfiguration> LoadFromFileAsync(string file, bool secure)
		{
			if (!secure)
			{
				var appConfig = ApplicationConfiguration.LoadWithNoValidation(new FileInfo(file), typeof(ApplicationConfiguration), GetTelemetryContext());
				if (appConfig.CertificateValidator == null)
					appConfig.CertificateValidator = new CertificateValidator(GetTelemetryContext());
				return appConfig;
			}
			else
			{
				var appConfig = await ApplicationConfiguration.LoadAsync(new FileInfo(file), ApplicationType.Client, typeof(ApplicationConfiguration), GetTelemetryContext());
				if (appConfig.CertificateValidator == null)
					appConfig.CertificateValidator = new CertificateValidator(GetTelemetryContext());
				appConfig.SecurityConfiguration.AddAppCertToTrustedStore = false;
				await appConfig.ValidateAsync(ApplicationType.Client);
				return appConfig;
			}
		}
	}
}
