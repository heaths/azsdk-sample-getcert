# Get the private key for Key Vault certificate

This is a simple sample to show how you can get the private key for a certificate in Azure Key Vault using the following packages:

* [Azure.Security.KeyVault.Certificates](https://nuget.org/packages/Azure.Security.KeyVault.Certificates)
* [Azure.Security.KeyVault.Secrets](https://nuget.org/packages/Azure.Security.KeyVault.Secrets)

The private key is stored as a PEM- or PKCS#12-encoded secret, while the public key is stored as a key. The `CertificateClient` and associated REST APIs intended more to create, manage, and rotate certificates. The secret will only have the public and private key pair if the key was marked as exportable; otherwise, it contains only the public key, which is already available from the `KeyVaultCertificate.Cer` property.

## Building

To build and run this project, you'll need:

1. [.NET Core 3.1](https://dot.net) or newer.
2. A service principal with access to get certificates and secrets, which you need to authenticate:
   * Log into Azure using the [az CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (preview): `az login`
   * Log into Azure using the [Az PowerShell module](https://docs.microsoft.com/powershell/azure/install-az-ps?view=azps-1.2.0) (preview): `Connect-AzAccount`
   * Log into Azure using your browser: pass `--interactive` to allow opening your default browser to authenticate.
   * Create a service principal in the portal and save the following information in user environment variables:
     Name                 | Description
     -------------------- | -----------
     `AZURE_TENANT_ID`    | The tenant ID (GUID) to which the service principal belongs.
     `AZURE_CLIENT_ID`    | The application ID (GUID).
     `AZURE_CLIENT_ID`    | The application password/secret (GUID).
     `AZURE_KEYVAULT_URL` | Optional URI of the Key Vault, e.g. `https://myvault.vault.azure.net`.

Running the project as shown in the example below will restore packages, build the project, and run the executable:

```bash
dotnet run -- --certificate-name test-certificate
```

For more usage information, run:

```bash
dotnet run -- --help
```
