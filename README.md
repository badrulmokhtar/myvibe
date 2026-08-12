<p align="center">
  <img src="assets/myvibe-icon.png" width="104" alt="MyVibe icon">
</p>

<h1 align="center">MyVibe</h1>

<p align="center">A focused, safe manager for installing and maintaining creative plug-ins.</p>

<p align="center">
  <img alt="Windows 10 and 11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-4b9cff">
  <img alt="Illustrator 2026" src="https://img.shields.io/badge/Illustrator-2026-ff9a42">
  <img alt="Beta" src="https://img.shields.io/badge/channel-beta-f0b45c">
</p>

![MyVibe plugin manager](assets/myvibe-app.png)

## Install creative tools without manual folder work

MyVibe shows compatible plug-ins, their installed and available versions, and the applications they support. Installation and removal create a backup first and stop safely when Adobe Illustrator is open.

The first available plug-in is **2.5D Transform**, a non-destructive X/Y/Z and perspective workflow for editable Illustrator artwork.

## Download

Open [Releases](../../releases) and download the latest **MyVibe Windows x64** ZIP. Extract it, run `MyVibe.exe`, and approve administrator access only when installing or removing a plug-in.

> MyVibe is currently a beta. Windows may display an Unknown publisher warning until the production signing certificate is enabled. Beta limitations are always stated in the release notes.

## User guide

Follow the complete [MyVibe and 2.5D Transform user guide](docs/USER-GUIDE.md) for installation, first use, presets, linked groups, updates, removal, recovery, and troubleshooting.

Prefer a printable edition? [Download the PDF user guide](docs/MyVibe-and-2.5D-Transform-User-Guide.pdf).

## Compatibility

| Component | Current support |
| --- | --- |
| Operating system | Windows 10/11 x64 |
| Adobe application | Illustrator 2026, version 30.x |
| MyVibe | 0.2.0 beta |
| 2.5D Transform | 0.9.5 beta |

MyVibe reads the public [`catalog.json`](catalog.json), verifies package checksums, and retains the last valid catalog for offline use.

## Trust and privacy

- Release assets include SHA-256 checksums.
- Stable Windows assets must carry a trusted Authenticode signature.
- CEP panels are packaged with Adobe's signing tool.
- No account, telemetry, advertising, or background service is required.
- Source code, SDK paths, signing material, and development history are not distributed from this repository.

Read [Security](SECURITY.md), [Support](SUPPORT.md), and the [Changelog](CHANGELOG.md) before reporting a problem.

## Version history

Every published version remains available through [GitHub Releases](../../releases). `catalog.json` points MyVibe to the current compatible versions; releases and tags preserve the history.

## License

MyVibe and its distributed plug-ins are provided under the [MyVibe Binary License](LICENSE.txt).
