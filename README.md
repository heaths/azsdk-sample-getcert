# Get the private key for Key Vault certificate

This is a simple sample to show how you can get the private key for a certificate in Azure Key Vault using the following packages:

* [Azure.Security.KeyVault.Certificates](https://nuget.org/packages/Azure.Security.KeyVault.Certificates)
* [Azure.Security.KeyVault.Secrets](https://nuget.org/packages/Azure.Security.KeyVault.Secrets)

The private key is stored as a PEM- or PKCS#12-encoded secret, while the public key is stored as a key. The `CertificateClient` and associated REST APIs are more to manage and rotate certificates.

## Building

To build this project, you'll need .NET Core 3.1 or newer. Running the project as shown in the example below will restore packages, build the project, and run the executable:

```bash
dotnet run -- --certificate-name test-certificate
```
