<p align="center">
  <img src="assets/myvibe-icon.png" width="104" alt="MyVibe icon">
</p>

<h1 align="center">MyVibe</h1>

<p align="center">Install and maintain Illustrator plug-ins on Windows and macOS.</p>

<p align="center">
  <img alt="Windows 10 and 11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-4b9cff">
  <img alt="macOS 13 and later" src="https://img.shields.io/badge/macOS-13%2B-8c8c8c">
  <img alt="Illustrator 2026" src="https://img.shields.io/badge/Illustrator-2026-ff9a42">
  <img alt="Beta" src="https://img.shields.io/badge/channel-beta-f0b45c">
</p>

![MyVibe plugin manager](assets/myvibe-app.png)

## Download MyVibe 0.4.0 beta

| Platform | Download | Requirements |
| --- | --- | --- |
| Windows | [Windows x64 ZIP](https://github.com/badrulmokhtar/myvibe/releases/download/myvibe-v0.4.0/MyVibe-0.4.0-beta.1-win-x64.zip) | Windows 10/11 x64 |
| macOS | [Universal ZIP](https://github.com/badrulmokhtar/myvibe/releases/download/myvibe-v0.4.0/MyVibe-0.4.0-beta.1-macos-universal.zip) | macOS 13+, Apple silicon or Intel |

SHA-256 files are published beside both downloads on the [release page](https://github.com/badrulmokhtar/myvibe/releases/tag/myvibe-v0.4.0).

> **Beta signing notice:** These builds are not yet signed with Windows Authenticode or Apple Developer ID. The operating system may show an Unknown Publisher or unidentified developer warning. MyVibe still requires a signed catalog, an official GitHub release URL, and an exact SHA-256 match before installation. See [Unsigned beta installation](docs/UNSIGNED-BETA.md).

## Managed plug-ins

- **2.5D Transform 0.9.5** — editable X/Y/Z rotation and perspective views for Illustrator artwork.
- **ToneMesh 0.9.3** — editable tone-driven vector fields with custom marks, structures, and contour handling.

Each plug-in can be installed, updated, repaired, or removed independently.

## What MyVibe checks

- Loads the signed catalog automatically and keeps the last verified copy for offline use.
- Accepts downloads only from the official MyVibe GitHub release path.
- Verifies each package against its catalog SHA-256 before changing Illustrator.
- Stops if Illustrator is open.
- Creates a backup and rolls back failed changes.
- Requires administrator approval only when protected application files must change.

## Compatibility

| Component | Current support |
| --- | --- |
| Operating system | Windows 10/11 x64; macOS 13+ universal |
| Adobe application | Illustrator 2026, version 30.x |
| MyVibe | 0.4.0 beta |
| 2.5D Transform | 0.9.5 beta |
| ToneMesh | 0.9.3 beta |

## Documentation

- [User guide](docs/USER-GUIDE.md)
- [Unsigned beta installation](docs/UNSIGNED-BETA.md)
- [Windows build and release](windows/README.md)
- [macOS build](macos/README.md)
- [Security policy](SECURITY.md) · [Support](SUPPORT.md) · [Changelog](CHANGELOG.md)

## Trust and privacy

- Catalog metadata is signed independently from GitHub hosting, and package hashes are verified before installation.
- No account, telemetry, advertising, or background service is required.
- Private signing keys are never stored in this repository or release archives.
- Stable releases require platform-trusted signing identities.

## License

MyVibe and its distributed plug-ins are provided under the [MyVibe Binary License](LICENSE.txt).
