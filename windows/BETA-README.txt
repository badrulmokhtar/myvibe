MyVibe 0.4.0 beta
=================

MyVibe installs, updates, and removes 2.5D Transform 0.9.5 and ToneMesh 0.9.8
for Adobe Illustrator.

Compatibility
-------------
- Windows 10 or Windows 11, x64
- Adobe Illustrator 2026 (version 30.x)
- Microsoft Visual C++ 2015-2022 Redistributable, x64

Install
-------
1. Close Adobe Illustrator.
2. Run MyVibe.exe.
3. Select a plugin and choose Install plugin.
4. Approve the Windows administrator prompt.
5. Restart Illustrator and open the installed panel from Window > Extensions.

Remove
------
Close Illustrator, run MyVibe, and choose Remove plugin. MyVibe creates a
local backup before changing the plug-in files.

Updates
-------
Choose Check for updates to load the signed GitHub catalog. Update a selected
plugin or choose Update all plugins to install every available plugin update
with one administrator approval. MyVibe verifies every package and backs up
each installed plugin before replacing it.

Upgrading from MyVibe 0.2
-------------------------
MyVibe 0.2 detects this release but cannot replace its own unsigned executable.
Download this ZIP once, extract it to a writable user folder, and run MyVibe.exe.
Future verified manager updates can install and restart in-app from MyVibe 0.3+.

Beta security notice
--------------------
The 2.5D Transform CEP panel is packaged with an Adobe-verified self-signed beta signature.
ToneMesh is downloaded only from the signed MyVibe catalog.
MyVibe.exe and the native AIP are not yet signed by a publicly trusted Windows
code-signing certificate, so Windows may show an Unknown publisher warning.

Do not redistribute a copy whose SHA-256 checksum differs from SHA256SUMS.txt.

Diagnostics and backups
-----------------------
Startup report: %TEMP%\MyVibe-startup-error.log
Backups:        %LOCALAPPDATA%\MyVibe\backups
