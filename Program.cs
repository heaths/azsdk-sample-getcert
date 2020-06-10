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
        /// <summary>
        /// Gets information about a certificate in Azure Key Vault.
        /// </summary>
        /// <param name="certificateName">The required name of the certificate.</param>
        /// <param name="vault">Optional vault name or URI to an Azure Key Vault.</param>
        /// <param name="verbose">Enable verbose output.</param>
        /// <returns>A <see cref="Task"/> to await the operation.</returns>
        static async Task Main(string certificateName, string vault = null, bool verbose = false)
        {
            using IDisposable listener = verbose ? AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose) : null;
            using CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, args) =>
            {
                cts.Cancel();
            };

            vault ??= Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL") ?? throw new InvalidOperationException("Missing --vault parameter or $AZURE_KEYVAULT_URL");
            if (!Uri.TryCreate(vault, UriKind.Absolute, out Uri vaultUri) && !Uri.TryCreate($"https://{vault}.vault.azure.net", UriKind.Absolute, out vaultUri))
            {
                throw new InvalidOperationException($"{vault} is not a valid Azure Key Vault URI");
            }

            X509Certificate2 certificate = await GetCertificateAsync(vaultUri, certificateName, cts.Token);

            Console.WriteLine("Subject: {0}", certificate.Subject);
            Console.WriteLine("Thumbprint: {0}", certificate.Thumbprint);
            Console.WriteLine("HasPrivateKey: {0}", certificate.HasPrivateKey);
        }

        static async Task<X509Certificate2> GetCertificateAsync(Uri vaultUri, string certificateName, CancellationToken cancellationToken = default)
        {
            DefaultAzureCredential credential = new DefaultAzureCredential();
            CertificateClient certificateClient = new CertificateClient(vaultUri, credential);
            SecretClient secretClient = new SecretClient(vaultUri, credential);

            KeyVaultCertificateWithPolicy certificate = await certificateClient.GetCertificateAsync(certificateName, cancellationToken);

            // Return a certificate with only the public key if the private key is not exportable.
            if (certificate.Policy?.Exportable != true)
            {
                return new X509Certificate2(certificate.Cer);
            }

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
