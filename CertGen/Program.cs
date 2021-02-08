using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CertGen
{
	class Program
	{
		static void Main(string[] args)
		{
            var subject = args[0];
            var uri = args[1];
            var path = args[2];
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048);
            //var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            //var key = RSA.Create();
            


            var req = new CertificateRequest("cn=" + subject, RSA, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment, true));
            var name = new SubjectAlternativeNameBuilder();
            name.AddUri(new Uri(uri));
            var ext = name.Build();
            req.CertificateExtensions.Add(ext);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

            var privateKeyFile = Path.Combine(path, "private", string.Format("{0}.pfx", subject));
            // Create PFX (PKCS #12) with private key
            File.WriteAllBytes(privateKeyFile, cert.Export(X509ContentType.Pfx, new System.Security.SecureString()));

            // Create Base 64 encoded CER (public key only)
            //File.WriteAllText(publicKeyFile,
            //    "-----BEGIN CERTIFICATE-----\r\n"
            //    + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
            //    + "\r\n-----END CERTIFICATE-----");
            var publicKeyFile = Path.Combine(path, "certs", string.Format("{0}.der", subject));
            File.WriteAllBytes(publicKeyFile, cert.Export(X509ContentType.Cert));
        }
	}
}
