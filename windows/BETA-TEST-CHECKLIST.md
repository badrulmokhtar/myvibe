# MyVibe beta lifecycle test

Use a clean Windows 10/11 x64 test profile with Illustrator 2026 and the Microsoft Visual C++ 2015–2022 Redistributable x64 installed.

- Download the MyVibe ZIP from the official GitHub release and confirm its SHA-256 checksum.
- Extract it to a writable user folder and launch `MyVibe.exe`.
- Confirm both 2.5D Transform and ToneMesh are listed.
- With Illustrator closed, install each plugin and approve the administrator prompt.
- Start Illustrator and open both panels from **Window > Extensions**.
- Create a simple document and confirm each panel can apply its primary effect.
- Close Illustrator, run **Check for updates**, and confirm available updates are shown on the correct plugin cards.
- Exercise **Update all plugins** and confirm only one administrator approval is requested.
- Restart Illustrator and confirm both updated panels open and retain independent versions.
- Close Illustrator, remove each plugin, and confirm the `.aip` and CEP folders are removed while backups remain under `%LOCALAPPDATA%\MyVibe\backups`.
- Reinstall both plugins and repeat the Illustrator launch check.
- From MyVibe 0.2, confirm 0.3.1 is detected and the migration message leads to the manual download.
- From MyVibe 0.3+, confirm a newer test manager downloads, restarts, marks itself healthy, and exposes the previous version in **Version history**.

Record the Windows build, Illustrator build, MyVibe version, plugin versions, and any failure screenshots with the beta report.
