# MyVibe 0.5.1 macOS beta

Dependency-free native macOS manager for the signed MyVibe v2 catalog.

## Build and verify

```sh
sh macos/build.sh
```

The script creates an ad-hoc-signed universal application at `macos/build/MyVibe.app`, runs its safety self-test, confirms both Apple Silicon and Intel slices, and verifies the bundle signature. The beta still requires the user-facing unsigned-package consent described in `docs/UNSIGNED-BETA.md`.

The app installs only artifacts authorized by a signed v2 catalog, hosted under the official GitHub Releases path, and matching the catalog SHA-256. It refuses to change plug-ins while Illustrator is running, rejects unsafe ZIP paths and symbolic links, and creates a backup before replacement.

Managed plugins: 2.5D Transform, ToneMesh, Logolize, and AutoOps. AutoOps and Logolize have no native engine; each uses the same CEP package on Mac and Windows.
