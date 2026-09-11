# MyVibe beta lifecycle test

Use a clean Windows 10/11 x64 test profile with Illustrator 2026 and the Microsoft Visual C++ 2015–2022 Redistributable x64 installed.

- Download the MyVibe ZIP from the official GitHub release and confirm its SHA-256 checksum.
- Extract it to a writable user folder and launch `MyVibe.exe`.
- Confirm 2.5D Transform, ToneMesh, and Logolize are listed.
- With Illustrator closed, install each plugin and approve the administrator prompt.
- Start Illustrator and open all three panels from **Window > Extensions**.
- Create a simple document and confirm each panel can apply its primary effect.
- Close Illustrator, run **Check for updates**, and confirm available updates appear on the correct plugin cards.
- Exercise **Update all plugins** and confirm only one administrator approval is requested.
- Restart Illustrator and confirm all updated panels open and retain independent versions.
- Close Illustrator, remove each plugin, and confirm native `.aip` files (when present) and CEP folders are removed while backups remain under `%LOCALAPPDATA%\MyVibe\backups`.
- Select an earlier Logolize release and confirm **Roll back to** replaces only its CEP folder and preserves a recovery backup.
- Reinstall all three plugins and repeat the Illustrator launch check.
- From MyVibe 0.2, confirm 0.3.1 is detected and the migration message identifies the manual download requirement.
- From MyVibe 0.3+, confirm a newer test manager downloads, restarts, marks itself healthy, and exposes the previous version in **Version history**.

Record the Windows build, Illustrator build, MyVibe version, plugin versions, and any failure screenshots with the beta report.

## Unsigned 0.4 beta checks

- On Windows, confirm SmartScreen or UAC can show **Unknown publisher**, then confirm MyVibe requires **Unsigned beta package** consent before an unsigned install or update.
- On macOS 13+, open the universal MyVibe app with **Open** or **Open Anyway**, then confirm MyVibe requires **Unsigned beta package** consent before an unsigned install or update.
- Confirm a changed package byte causes a SHA-256 failure before administrator access or file replacement.
- Confirm an untrusted download host or unsigned catalog is rejected on both platforms.
- Confirm all three plug-ins install, update, remove, and restore independently after the v2 beta catalog is published.
- Confirm a stable test catalog rejects every artifact with `signature.required: false`.
