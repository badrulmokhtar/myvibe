# Unsigned beta installation

MyVibe beta builds can run and install plug-ins before publicly trusted Windows and Apple developer certificates are available. The operating system warning is expected; package integrity checks are still mandatory.

MyVibe requires all of the following before an unsigned beta install or update:

- a valid MyVibe catalog signature;
- an HTTPS download from the official `badrulmokhtar/myvibe` GitHub Releases path;
- an exact SHA-256 match;
- a safe package layout with the expected native and CEP components;
- explicit **Install Anyway** consent;
- a backup before replacement and rollback after failure.

Stable catalog entries cannot use this exception. They require a trusted Authenticode signature on Windows or the expected Apple Developer ID identity on macOS.

## Windows 10 or 11

1. Download the Windows x64 ZIP from the official MyVibe release.
2. Extract it to a writable user folder.
3. Run `MyVibe.exe`. If Microsoft Defender SmartScreen appears, choose **More info**, verify the app name and source, then choose **Run anyway**.
4. Select 2.5D Transform, ToneMesh, or Logolize and choose Install or Update.
5. Read the **Unsigned beta package** warning and continue only if you intended to install that release.
6. Approve the administrator prompt. An **Unknown publisher** label is expected for the beta.

Stop if the download did not come from the official release, the published checksum differs, or MyVibe reports a catalog or checksum failure.

## macOS 13 or later

1. Download the universal macOS ZIP from the official MyVibe release and extract it.
2. Control-click `MyVibe.app`, choose **Open**, then choose **Open** again. If macOS still blocks it, open **System Settings > Privacy & Security** and choose **Open Anyway** for MyVibe.
3. Select 2.5D Transform, ToneMesh, or Logolize and choose Install or Update.
4. Read the **Unsigned beta package** warning and choose **Install Anyway** only if you intended to install that release.
5. Enter an administrator password when MyVibe installs the native Illustrator plug-in.

For an unsigned beta, MyVibe removes quarantine only from the verified staged plug-in payload. If its CEP panel has no Adobe signature, MyVibe enables CEP developer mode for the current macOS user. It does not disable Gatekeeper globally or weaken the signed-catalog and checksum checks.
