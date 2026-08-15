# Changelog

All notable public changes are documented here. Releases follow semantic versioning.

## [0.3.0] - 2026-08-15

### Added

- ToneMesh 0.9.3 as the second independently managed Illustrator plugin.
- In-app MyVibe updates with backup, health verification, restart, and rollback.
- Signed catalog verification using the public key embedded in MyVibe 0.3.0.
- Separate install, update, repair, removal, version state, backup, and CEP cache handling for each plugin.

### Security

- Restricted downloads to the official `badrulmokhtar/myvibe` GitHub release path.
- Re-authorized plugin packages from the signed catalog inside elevated installation.
- Added traversal-safe, size-bounded archive extraction and required signed CEP payloads.
- MyVibe and native AIP binaries remain unsigned during beta and can trigger Windows warnings.

## [0.2.0] - 2026-08-12

### Added

- Public version catalog for manager and plug-in discovery.
- MyVibe product icon and responsive Windows interface.
- Illustrator and Visual C++ prerequisite detection.
- Backup, rollback, action-only elevation, and offline catalog fallback foundations.
- Signed CEP packaging and release checksum workflow.

### Security

- Generated signing material, credentials, assistant history, and local environment files are excluded from publication.
