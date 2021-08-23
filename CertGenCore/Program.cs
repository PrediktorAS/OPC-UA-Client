using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CertGenCore
{
	class Program
	{
        static void ExportPem(X509Certificate2 cert, string filename, string certPath, string privatePath)
        {

            byte[] certificateBytes = cert.RawData;
            char[] certificatePem = PemEncoding.Write("CERTIFICATE", certificateBytes);
            var pemCert = Path.Combine(certPath, string.Format("{0}.cert", filename));
            File.WriteAllText(pemCert, new string(certificatePem));
            var rsa = cert.GetRSAPrivateKey();
            AsymmetricAlgorithm key;
            if (rsa != null)
                key = rsa;
            else
                key = cert.GetECDsaPrivateKey();
            byte[] pubKeyBytes = key.ExportSubjectPublicKeyInfo();
            byte[] privKeyBytes = key.ExportPkcs8PrivateKey();
            char[] pubKeyPem = PemEncoding.Write("PUBLIC KEY", pubKeyBytes);
            char[] privKeyPem = PemEncoding.Write("PRIVATE KEY", privKeyBytes);
            var pubPem = Path.Combine(certPath, string.Format("{0}.pem", filename));
            File.WriteAllText(pubPem, new string(pubKeyPem));

            var privPem = Path.Combine(privatePath, string.Format("{0}.pem", filename));
            File.WriteAllText(privPem, new string(privKeyPem));
        }

		static void Main(string[] args)
		{
            var cmd = args[0];
            if (cmd == "create")
            {
                var subject = args[1];
                var uri = args[2];
                var path = args[3];
                var pw = args.Length > 4 ? args[4] : "";
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048);
                //var ecdsa = ECDsa.Create(); // generate asymmetric key pair
                //var key = RSA.Create();
                var privatePath = Path.Combine(path, "private");
                Directory.CreateDirectory(privatePath);
                var certPath = Path.Combine(path, "certs");
                Directory.CreateDirectory(certPath);

                var req = new CertificateRequest("cn=" + subject, RSA, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment, true));
                var name = new SubjectAlternativeNameBuilder();
                name.AddUri(new Uri(uri));
                var ext = name.Build();
                req.CertificateExtensions.Add(ext);
                var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

                var privateKeyFile = Path.Combine(privatePath, string.Format("{0}.pfx", subject));
                // Create PFX (PKCS #12) with private key
                File.WriteAllBytes(privateKeyFile, cert.Export(X509ContentType.Pfx, pw));
                var publicKeyFile = Path.Combine(certPath, string.Format("{0}.der", subject));
                File.WriteAllBytes(publicKeyFile, cert.Export(X509ContentType.Cert));


                // PEM and CERT.

            }
            else if (args[0] == "convert")
            {
                var cert = new X509Certificate2(args[1], args[2], X509KeyStorageFlags.Exportable);
                ExportPem(cert, args[3], args[4], args[5]);
            }

        }
	}
}
