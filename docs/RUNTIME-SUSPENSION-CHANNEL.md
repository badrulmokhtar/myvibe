# MyVibe Runtime Suspension Channel

## Goal

Allow MyVibe to suspend an already-loaded Illustrator native plug-in without removing its `.aip` file or restarting Illustrator.

Suspension means that the native plug-in stays loaded, but its selection/event handlers immediately return, panel updates stop, and active tools are cleaned up. Install, update, rollback, and uninstall remain separate file operations.

## Current limitation

MyVibe currently manages files and version state. It does not have a control path into an Illustrator process. Removing or replacing an `.aip` while Illustrator is running is intentionally blocked because Illustrator does not reliably unload native plug-ins.

The plug-ins already have Illustrator notifier and CEP/PlugPlug event paths, but MyVibe is a separate native application and cannot directly dispatch a CEP event when the panel is closed.

## Recommended architecture

Use a two-stage, event-driven design.

### Stage 1: shared runtime-state file plus Illustrator activation

This is the lowest-risk cross-platform implementation and adds no polling thread.

1. MyVibe writes the desired state atomically to a per-user runtime directory:
   - macOS: `~/Library/Application Support/MyVibe/runtime/`
   - Windows: `%LOCALAPPDATA%\\MyVibe\\runtime\\`
2. The file name is derived from the validated plug-in ID, for example `com.badru.tonemesh.json`.
3. The native plug-in reads the file only at startup, CSXS-ready, and Illustrator-activated notifications.
4. MyVibe brings Illustrator back to the foreground after changing the state. The existing application-activated notifier then applies the change immediately.

Suggested payload:

```json
{
  "schemaVersion": 1,
  "pluginId": "com.badru.tonemesh",
  "suspended": true,
  "generation": 12,
  "updatedAt": "2026-09-12T02:00:00Z"
}
```

The plug-in should default to active if no valid state file exists, reject mismatched IDs or malformed data, and apply only a newer generation. Writes must use a temporary file followed by an atomic rename. State files must be user-readable/writable only.

This approach has no periodic polling and no persistent IPC server. The only user-visible effect is a brief focus change when MyVibe activates Illustrator to deliver the state.

### Stage 2: direct command delivery when focus changes are undesirable

Add an optional direct command channel for immediate delivery while Illustrator remains active:

- macOS: a narrowly scoped Apple Event handled by the plug-in or a small Illustrator-side control component.
- Windows: a per-user named pipe with an ACL restricted to the current user, or a registered window message if a stable Illustrator window target is available.

The command should contain only `pluginId`, `suspended`, and `generation`. The plug-in validates the same schema and updates the in-memory gate. The state file remains the source of truth for restart/recovery.

The direct channel must be demand-driven: open/send/close per command, with no heartbeat and no long-running MyVibe polling loop.

## Plug-in contract

Each native plug-in should expose the same internal contract:

- `SetRuntimeSuspended(bool suspended, uint64_t generation)`
- `IsRuntimeSuspended()`
- selection/event callbacks return before any Illustrator suite traversal when suspended
- active tools and annotators are deactivated when entering suspended mode
- panel visibility (`panelReady`/`panelHidden`) remains an independent safety gate

For ToneMesh specifically, `SendData()` and `HandleSelectionChanged()` must retain the `panelActive_` early return. Runtime suspension is an additional gate controlled by MyVibe.

## MyVibe UI behavior

Do not overload the existing install/remove button.

- **Suspend now**: writes runtime state and sends the direct command when available; no restart.
- **Resume**: clears suspension and sends the enable command; no restart.
- **Update/rollback/uninstall**: keeps the existing Illustrator-running guard and explains that Illustrator must be closed for binary replacement.

The card should show `Installed · Suspended` or `Installed · Active` separately from the installed version.

If Illustrator is not running, MyVibe only persists the desired state; the next Illustrator launch applies it. If delivery fails, MyVibe must report `Saved for next launch` rather than claiming immediate suspension.

## Security and reliability requirements

- Validate plug-in IDs against the built-in registry before creating a path or command.
- Use atomic writes and monotonic generations to prevent partial or stale state.
- Restrict runtime-state permissions to the current user.
- Never execute arbitrary commands received through the channel.
- Treat missing, malformed, or inaccessible state as active (fail-open for compatibility) and show a diagnostic entry.
- Keep the direct channel local-only; do not bind a TCP listener.
- Add timeouts and bounded payload sizes to Apple Event/named-pipe delivery.

## Implementation order

1. Add the shared runtime-state schema and atomic writer to macOS Swift and Windows C#.
2. Add native state-file reads at startup and application activation to ToneMesh; mirror the contract in Transform 2.5D.
3. Add MyVibe `Suspend now`/`Resume` UI and state indicators.
4. Verify the no-focus-change fallback behavior when Illustrator is not running.
5. Add the optional direct Apple Event and Windows named-pipe transport only if focus activation is judged disruptive.
6. Test active, suspended, panel-hidden, Illustrator-restart, update, rollback, and uninstall flows on both platforms.

## Acceptance criteria

- Unchecking/suspending a loaded plug-in stops selection processing within one Illustrator activation cycle, without restarting.
- Re-enabling resumes normal panel and selection behavior without reinstalling files.
- Closed panels perform no artwork traversal or background polling.
- Update/rollback/uninstall still refuse to modify loaded `.aip` files until Illustrator is closed.
- A failed direct delivery never loses the desired state; the next launch applies it.

