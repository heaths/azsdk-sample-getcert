using System;
using System.Diagnostics.Tracing;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace GetCert
{
    class Program
    {
        static async Task Main(string certificateName, bool verbose = false)
        {
            using IDisposable listener = verbose ? AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose) : null;
            using CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, args) =>
            {
                cts.Cancel();
            };
            
            X509Certificate2 certificate = await GetCertificateAsync(certificateName, cts.Token);
            
            Console.WriteLine("Subject: {0}", certificate.Subject);
            Console.WriteLine("Thumbprint: {0}", certificate.Thumbprint);
            Console.WriteLine("HasPrivateKey: {0}", certificate.HasPrivateKey);
        }

        static async Task<X509Certificate2> GetCertificateAsync(string certificateName, CancellationToken cancellationToken = default)
        {
            Uri vaultUri = new Uri(Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL") ?? throw new InvalidOperationException("Missing $AZURE_KEYVAULT_URL"));

            DefaultAzureCredential credential = new DefaultAzureCredential();
            CertificateClient certificateClient = new CertificateClient(vaultUri, credential);
            SecretClient secretClient = new SecretClient(vaultUri, credential);

            KeyVaultCertificateWithPolicy certificate = await certificateClient.GetCertificateAsync(certificateName, cancellationToken);

            // Parse the secret ID and version to retrieve the private key.
            string[] segments = certificate.SecretId.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3)
            {
                throw new InvalidOperationException($"Number of segments is incorrect: {segments.Length}, URI: {certificate.SecretId}");
            }

            string secretName = segments[1];
            string secretVersion = segments[2];

            KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName, secretVersion, cancellationToken);

            // For PEM, you'll need to extract the base64-encoded message body.
            // .NET 5.0 preview introduces the System.Security.Cryptography.PemEncoding class to make this easier.
            if ("application/x-pkcs12".Equals(secret.Properties.ContentType, StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] pfx = Convert.FromBase64String(secret.Value);
                return new X509Certificate2(pfx);
            }

            throw new NotSupportedException($"Only PKCS#12 is supported. Found Content-Type: {secret.Properties.ContentType}");
        }
    }
}
