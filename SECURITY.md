# Security

## Supported versions

Only versions marked current in `catalog.json` receive security updates.

## Report a vulnerability

Do not disclose a vulnerability in a public issue. Use GitHub's private vulnerability reporting feature on this repository. Include the affected version, operating system, reproduction steps, and impact. Never include passwords, tokens, private documents, or customer artwork.

## Release integrity

Compare downloads with the SHA-256 digest shown in the GitHub Release and catalog. MyVibe verifies the catalog signature before trusting metadata, accepts artifacts only from the official MyVibe GitHub release path, verifies every package checksum, and re-authorizes Windows packages after elevation. A checksum mismatch means the file must not be run.

Beta artifacts may omit Windows Authenticode or Apple Developer ID certificates. MyVibe must disclose that state and receive explicit consent before installing or updating one. On macOS, quarantine is removed only from the already verified staged plug-in payload; Adobe CEP developer mode is enabled for the current user only when the verified beta CEP payload is unsigned. Stable catalog entries always require a platform-trusted signature and the expected publisher identity.

See [Unsigned beta installation](docs/UNSIGNED-BETA.md) for the expected Windows and macOS warnings. These warnings are not evidence of package integrity; the signed catalog and SHA-256 verification remain mandatory.

## Data handling

MyVibe does not require an account and does not collect telemetry. It contacts GitHub only when checking the public update catalog or downloading a user-requested update.
