# MyVibe plugin manager and 2.5D Transform user guide

This guide explains how to install, update, remove, and recover **2.5D Transform** and **ToneMesh** with **MyVibe**, then covers the complete 2.5D Transform workflow inside Adobe Illustrator.

> Current beta compatibility: Windows 10/11 x64 or macOS 13+ universal; Adobe Illustrator 2026, version 30.x; MyVibe 0.4.0; 2.5D Transform 0.9.5; ToneMesh 0.9.8. Installation steps below cover Windows; see [Unsigned beta installation](UNSIGNED-BETA.md) for macOS opening instructions.

[Download the PDF edition](MyVibe-and-2.5D-Transform-User-Guide.pdf)

## Contents

1. [What you are installing](#1-what-you-are-installing)
2. [Before you begin](#2-before-you-begin)
3. [Download and verify MyVibe](#3-download-and-verify-myvibe)
4. [Install a plugin](#4-install-a-plugin)
5. [Open the plug-in in Illustrator](#5-open-the-plug-in-in-illustrator)
6. [Create your first 2.5D transformation](#6-create-your-first-25d-transformation)
7. [Work with multiple objects](#7-work-with-multiple-objects)
8. [Save and manage view presets](#8-save-and-manage-view-presets)
9. [Copy, paste, link, and unlink transformations](#9-copy-paste-link-and-unlink-transformations)
10. [Manage linked groups](#10-manage-linked-groups)
11. [Reset or expand artwork](#11-reset-or-expand-artwork)
12. [Update MyVibe and the plug-in](#12-update-myvibe-and-the-plug-in)
13. [Remove the plug-in](#13-remove-the-plug-in)
14. [Troubleshooting](#14-troubleshooting)
15. [Quick reference](#15-quick-reference)

## 1. What you are installing

MyVibe manages creative plug-ins on Windows and macOS. It installs both parts required by 2.5D Transform and ToneMesh:

- The native `.aip` engine that performs the Illustrator transformation.
- The CEP interface that provides the main panel and Linked Groups panel.

2.5D Transform adds live X, Y, Z, and perspective controls for editable Illustrator artwork. It supports paths, compound paths, Pathfinder results, groups, clipping groups, live text, symbols, gradients, raster images, and placed images.

ToneMesh creates editable tone-driven vector fields from solid fills, gradients, and mesh gradients. It includes multiple structures and marks, custom marks, appearance handles, and contour-aware edge behavior.

MyVibe creates a backup before it installs, updates, or removes the plug-in. If an operation fails, it attempts to restore the previous copy.

![MyVibe showing the installed plug-in and compatibility details](../assets/myvibe-app.png)

## 2. Before you begin

Confirm that your computer meets every requirement:

| Requirement | Current beta support |
| --- | --- |
| Operating system | Windows 10 or Windows 11 |
| Architecture | x64 |
| Adobe application | Illustrator 2026, version 30.x |
| Required runtime | Microsoft Visual C++ 2015-2022 Redistributable, x64 |

Also check these points:

- Save your Illustrator documents.
- Close Illustrator before installing, updating, or removing the plug-in.
- Use a Windows account that can approve an administrator prompt.
- Download MyVibe only from the official GitHub Releases page.

### Beta security notice

The current MyVibe executable and native AIP are beta builds and are not yet signed with a publicly trusted Windows certificate. Windows may display an **Unknown publisher** warning.

Continue only when the file came from the official repository and its SHA-256 checksum matches the value published with the release. Stop if the checksum differs.

## 3. Download and verify MyVibe

1. Open the repository [Releases page](https://github.com/badrulmokhtar/myvibe/releases).
2. Download the latest **MyVibe Windows x64** ZIP.
3. Extract the ZIP to a writable user folder. Do not run the app from inside the ZIP or place it under `Program Files`.
4. Find `MyVibe.exe`, `BETA-README.txt`, and `SHA256SUMS.txt` in the extracted folder.
5. Verify the checksum before running the app.

To calculate the checksum in PowerShell:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath .\MyVibe.exe
```

Compare the displayed hash with the `MyVibe.exe` value in `SHA256SUMS.txt`. The values must match exactly.

### Upgrade from MyVibe 0.2

MyVibe 0.2 detects the current manager but cannot replace its own unsigned executable. Download the latest ZIP once, extract it to a writable user folder, and run the new `MyVibe.exe`. MyVibe 0.3 and later can install future verified manager updates in-app.

## 4. Install a plugin

1. Close Adobe Illustrator.
2. Run `MyVibe.exe`.
3. Select the **2.5D Transform** or **ToneMesh** card.
4. Review the detail panel. Confirm that Illustrator 2026 and the Visual C++ runtime are detected.
5. Select **Install plugin**.
6. Approve the Windows administrator prompt. MyVibe requests elevated access only when it changes the Illustrator installation.
7. Wait for the success message. Do not open Illustrator while installation is in progress.
8. Start Illustrator after MyVibe confirms that installation is complete.

MyVibe installs the selected native engine and CEP interface together. 2.5D Transform can be installed from the bundled offline payload; ToneMesh requires the verified online catalog. Avoid manually mixing files from different releases.

## 5. Open the plug-in in Illustrator

Open the main panel from:

**Window > Extensions > 2.5D Transform**

Open the companion panel from:

**Window > Extensions > 2.5D Transform: Linked Groups**

Open ToneMesh from:

**Window > Extensions > ToneMesh**

You can dock either panel with your other Illustrator panels and resize it. The controls reflow as the panel becomes wider or narrower.

If the extension is missing, close Illustrator, reinstall the plug-in with MyVibe, then restart Illustrator.

## 6. Create your first 2.5D transformation

1. Select one supported object with Illustrator's Selection tool.
2. Confirm that the status at the top of the panel recognizes the selection.
3. Choose a **View preset**, or change the transformation manually.
4. Drag the cube to orbit the view. Hold **Shift** while dragging to roll it.
5. Fine-tune the values with the X, Y, Z, and Perspective controls.

The effect updates live. Values are absolute, so moving a value from 50 back to 0 returns that parameter to 0 instead of stacking another transformation.

### What each control does

| Control | Result |
| --- | --- |
| Rotate X | Tilts artwork forward or backward |
| Rotate Y | Turns artwork left or right |
| Rotate Z | Rolls artwork clockwise or counterclockwise |
| Perspective / Depth | Controls perspective strength from 0 to 100 |
| Orbit cube | Adjusts X and Y together; Shift-drag adjusts roll |

## 7. Work with multiple objects

Select two or more objects, then choose a transform mode:

- **Individual** transforms each object around its own center. Use it when repeated items should keep their own orientation and position.
- **Together** transforms the full selection around one shared center. Use it when the selection should behave like one composition.

The tabs are disabled when the current selection cannot use that mode.

For large or complex selections, the panel may show **Updating items**. During a drag, complex linked artwork can preview the selected or source objects first. Every follower receives the final precise value when you release the control.

## 8. Save and manage view presets

The plug-in includes protected presets such as Front, Isometric Left, Isometric Right, Two-point Left, Two-point Right, Top plane, Cabinet Left, and Cabinet Right.

### Apply a preset

1. Open **View preset**.
2. Search by name if the list is long.
3. Select a preset to apply it to the current selection.

### Save a custom preset

1. Create the view you want.
2. Select the save icon beside the preset menu.
3. Enter a clear name.
4. Select **Save**.

You can rename, duplicate, or delete custom presets. Built-in presets can be duplicated but cannot be renamed or deleted.

## 9. Copy, paste, link, and unlink transformations

The **Transform sync** section offers two different workflows.

### Copy and paste a view

Use this when objects should receive the same current values but remain independent.

1. Select the object whose view you want to reuse.
2. Select **Copy view**.
3. Select different artwork.
4. Select **Paste view**.

Later edits to either object do not affect the other.

### Link a selection

Use this when every member should continue sharing exact X, Y, Z, perspective, and transform-mode values.

1. Select two or more objects or groups.
2. Select **Link selection**.
3. If the selected artwork has different transformations, choose the existing linked group or selected object whose transform should be used.
4. Confirm the link.

Editing any linked member updates the complete linked group. Each member keeps its own location and center according to Individual or Together mode.

### Unlink artwork

1. Select one or more linked members.
2. Select **Unlink**.

The selected artwork leaves the relationship but keeps its current appearance. Future edits to the remaining group no longer affect it.

## 10. Manage linked groups

Open **Manage linked groups** from the main panel, or open **Window > Extensions > 2.5D Transform: Linked Groups**.

Use the companion panel to:

- Search linked groups or member names.
- Switch between **All groups** and **Current selection**.
- Select every member of a linked group on the artboard.
- Rename a linked group.
- Select an individual member in Illustrator.
- Unlink one member while preserving its appearance.
- Unlink an entire relationship after confirmation.
- Choose which existing transformation a new or merged selection should follow.

Blue highlighting means the artwork is currently selected on the Illustrator artboard. A colored group dot identifies membership and does not mean the item is selected.

## 11. Reset or expand artwork

### Reset

Select **Reset** to restore the cached original view and default transformation values. When the selection belongs to a linked group, the operation can affect every member that shares the transformation.

If verification fails, the plug-in rolls the operation back and shows **Change not applied** in both panels.

### Expand to editable vectors

Select **Expand to editable vectors** when you want to commit the visible result as regular Illustrator vector artwork.

Expansion is a finishing step. The result no longer behaves like the same live 2.5D effect, and live text or other live content may become vector output. Duplicate important artwork or save the document before expanding.

## 12. Update MyVibe and the plug-in

MyVibe contacts GitHub only when it checks the public catalog or downloads an update you requested.

### Check for updates

1. Open MyVibe.
2. Select **Check for updates**.
3. Review any available MyVibe or plug-in version.

### Update a plugin

1. Save your work and close Illustrator.
2. Select the plugin card, then select **Update to [version]** when MyVibe offers an update.
3. Approve the administrator prompt.
4. Restart Illustrator after the update completes.

MyVibe verifies the download and saves the current plug-in before replacing it. If the update fails, it attempts to restore the previous copy.

### Update all plugins

1. Save your work and close Illustrator.
2. Select **Check for updates**.
3. Select **Update all plugins** when the action appears.
4. Review the listed versions and confirm.
5. Approve the single administrator prompt.
6. Restart Illustrator after every update completes.

MyVibe downloads and verifies all selected packages before changing any installed plugin. Each plugin receives its own backup and rollback protection.

### Update or restore MyVibe

When a future manager update is offered, select **Check for updates**, then select **Update MyVibe to [version]**. MyVibe verifies the release, stores the current manager version, replaces itself, and restarts.

Open **Version history** to see restorable manager versions. A restorable entry appears only after the first successful manager update. Restoring a manager version does not automatically change the installed plug-in version.

## 13. Remove the plug-in

1. Save your Illustrator documents and close Illustrator.
2. Open MyVibe and select the plugin you want to remove.
3. Select **Remove plugin**.
4. Read the confirmation, then select the removal action.
5. Approve the administrator prompt.
6. Restart Illustrator to refresh the Extensions menu.

MyVibe backs up the current native engine and CEP interface before removal. Backups are stored under:

`%LOCALAPPDATA%\MyVibe\backups\<plugin name>`

## 14. Troubleshooting

### MyVibe says Illustrator is open

Save your work, close every Illustrator window, wait a few seconds, and retry.

### Illustrator 2026 is not detected

Install the supported Illustrator 2026 release, version 30.x. This beta does not install into older Illustrator versions.

### The Visual C++ runtime is missing

Install the Microsoft Visual C++ 2015-2022 Redistributable for x64, restart MyVibe, and confirm that the detail panel shows **Detected**.

### Windows shows Unknown publisher

This is expected for the current beta. Verify that the ZIP came from the official GitHub Releases page and that its SHA-256 checksum matches. Do not continue when the checksum differs.

### The extension is missing after installation

Close Illustrator completely, reopen it, and check **Window > Extensions**. If it is still missing, close Illustrator and use MyVibe to remove and reinstall the plug-in.

### The panel says Native engine unavailable

The CEP interface is present but the `.aip` engine is missing, incompatible, or not loaded. Close Illustrator and reinstall with MyVibe so both components use the same release.

### Controls stay disabled after selecting artwork

- Use Illustrator's Selection tool and select the top-level object or group.
- Unlock locked artwork and show hidden artwork.
- Meshes and live graph objects are not supported in the current beta.

### A linked object changes unexpectedly

Open Linked Groups and check its membership. Select **Unlink** if the object should keep its current appearance but stop following the group.

### A large selection looks delayed while dragging

This is the lightweight preview mode. Keep dragging normally and release the control. The plug-in then applies the exact final value to every linked member.

### Installation, update, or removal fails

MyVibe normally restores the previous copy. Read the complete message, keep Illustrator closed, and retry. If recovery also fails, open a GitHub issue with the exact error, Windows version, Illustrator version, MyVibe version, and plug-in version. Do not upload private artwork, credentials, environment files, signing certificates, or AI chat exports.

## 15. Quick reference

| Goal | Action |
| --- | --- |
| Install a plug-in | Close Illustrator, open MyVibe, select its card, select Install plugin |
| Open the main panel | Window > Extensions > 2.5D Transform |
| Open ToneMesh | Window > Extensions > ToneMesh |
| Open linked-group management | Window > Extensions > 2.5D Transform: Linked Groups |
| Reuse a view without a relationship | Copy view, select other artwork, Paste view |
| Keep objects synchronized | Select two or more objects, Link selection |
| Stop synchronization | Select linked artwork, Unlink |
| Restore the original view | Select artwork, Reset |
| Commit the result to vectors | Select artwork, Expand to editable vectors |
| Check for releases | Open MyVibe, Check for updates |
| Remove the plug-in | Close Illustrator, open MyVibe, Remove plugin |

## Get help

Use [GitHub Issues](https://github.com/badrulmokhtar/myvibe/issues) for reproducible bugs and feature requests. For security concerns, use GitHub private vulnerability reporting instead of a public issue.

Include the affected version, operating system, Illustrator version, reproduction steps, and exact error message. Never include passwords, tokens, private documents, customer artwork, or signing material.
