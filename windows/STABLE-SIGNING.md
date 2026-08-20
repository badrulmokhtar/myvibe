# Stable release signing gate

MyVibe beta packages may remain unsigned and must clearly disclose the Windows **Unknown publisher** warning. Stable packages must not be created until `MyVibe.exe` has a valid, publicly trusted Authenticode signature.

1. Obtain an organization-validated or extended-validation Windows code-signing certificate from a trusted certificate authority.
2. Keep the certificate or hardware-token credentials outside the source and release repositories.
3. Set `MYVIBE_WINDOWS_CERT_PASSWORD` only in the release process when a password-protected PFX is used.
4. Sign and timestamp the executable:

   ```powershell
   .\build.ps1
   .\sign-windows.ps1 -Targets .\bin\MyVibe.exe -CertificatePath '<external certificate path>'
   ```

5. Build the stable archive:

   ```powershell
   .\build-release.ps1 -Stable -SkipBuild
   ```

The stable build exits without producing an archive unless Windows reports the signature status as `Valid`. Set `authenticodeRequired` to `true` and publish the expected signer thumbprint in the signed catalog for stable manager releases.
