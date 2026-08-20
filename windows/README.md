# MyVibe 0.4.0

MyVibe is a dependency-free Windows manager for **2.5D Transform 0.9.5** and **ToneMesh 0.9.3** for Adobe Illustrator 2026.

## Build

Create and verify the signed CEP payload first. Keep the certificate password in the process environment, never in a file:

```powershell
$env:MYVIBE_ZXP_CERT_PASSWORD = '<local certificate password>'
powershell -ExecutionPolicy Bypass -File .\myvibe\package-plugin.ps1 -CreateBetaCertificate
Remove-Item Env:\MYVIBE_ZXP_CERT_PASSWORD
```

Then build MyVibe:

```powershell
powershell -ExecutionPolicy Bypass -File .\myvibe\build.ps1
```

Create the signed ToneMesh release package with:

```powershell
powershell -ExecutionPolicy Bypass -File .\myvibe\package-tonemesh.ps1 -ToneMeshRoot '<ToneMesh repository>'
```

The executable is written to `myvibe\bin\MyVibe.exe`. 2.5D Transform remains embedded for offline installation. ToneMesh is downloaded only from the signed MyVibe catalog and is re-authorized after elevation before installation.

Create the private-beta distribution ZIP with:

```powershell
powershell -ExecutionPolicy Bypass -File .\myvibe\build-release.ps1
```

## Current behavior

- Displays and independently manages 2.5D Transform and ToneMesh.
- Detects complete, missing, and partial installations for each plugin.
- Reads the CEP manifest when a manually installed plugin has no MyVibe version record.
- Detects the Illustrator 2026 installation and required Visual C++ runtime.
- Refuses install/remove while Illustrator is open.
- Requests administrator access only when installing or removing.
- Backs up the exact current `.aip` and CEP folder before changing them.
- Installs 2.5D Transform from the embedded release and ToneMesh from the verified catalog.
- Removes both components and clears only the current plugin's CEP cache.
- Restores the previous copy if installation or removal fails partway through.
- Requires an Adobe-verified signed CEP payload during the build.
- Verifies the catalog with an embedded RSA public key and restricts all downloads to the official MyVibe GitHub release repository.
- Rechecks catalog authorization inside administrator package installs.
- Updates MyVibe in-app with bounded, traversal-safe ZIP extraction, backup, health check, and rollback.
- Updates all installed plugins from one action and one administrator approval.
- Shows stored manager versions when self-update backups become available.

## Upgrading from 0.2

MyVibe 0.2 detects the newer manager but does not replace its own unsigned executable. Download MyVibe 0.4.0 from the official GitHub release, extract it to a writable user folder, and run `MyVibe.exe`. Starting with 0.3, future verified manager updates can install and restart in-app.

## Release verification

Run the isolated package checks after creating the beta ZIP:

```powershell
powershell -ExecutionPolicy Bypass -File .\myvibe\test-beta-lifecycle.ps1
```

The automated test does not alter an Illustrator installation. Complete the real application checks in `BETA-TEST-CHECKLIST.md` on a clean Windows test profile before promoting a release.

Stable packaging is blocked until `MyVibe.exe` has a valid Authenticode signature. See `STABLE-SIGNING.md`.

The catalog signing private key stays outside the source and distribution repositories. MyVibe 0.4 prefers the cross-platform signed `catalog-v2.json` feed and retains signed v1 support for rollback compatibility. Publish a catalog and its matching signature together.
