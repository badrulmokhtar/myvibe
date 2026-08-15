# Security

## Supported versions

Only versions marked current in `catalog.json` receive security updates.

## Report a vulnerability

Do not disclose a vulnerability in a public issue. Use GitHub's private vulnerability reporting feature on this repository. Include the affected version, operating system, reproduction steps, and impact. Never include passwords, tokens, private documents, or customer artwork.

## Release integrity

Compare downloads with the SHA-256 digest shown in the GitHub Release and catalog. MyVibe 0.3.1 verifies `catalog.json.sig` before trusting catalog metadata and re-authorizes every plugin package after elevation. Stable packaging is blocked unless the Windows executable has a valid trusted Authenticode signature. Beta Windows binaries are not publicly trusted and can show an Unknown publisher warning. A checksum mismatch means the file must not be run.

## Data handling

MyVibe does not require an account and does not collect telemetry. It contacts GitHub only when checking the public update catalog or downloading a user-requested update.
