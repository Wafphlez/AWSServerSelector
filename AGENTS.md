# AWSServerSelector Agent Context

## Project Summary

- Desktop app for selecting/controlling Dead by Daylight AWS connectivity behavior.
- Stack: `WPF`, `.NET 9`, `C#`, `MVVM`.
- Main executable name: `PingByDaylight`.
- Supported UI locales: `en`, `ru`.

## Core Architecture

- UI windows and controls:
  - `Windows/*.xaml`, `Windows/*.xaml.cs`
  - `Controls/*.xaml`, `Controls/*.xaml.cs`
- ViewModels:
  - `ViewModels/*`
  - Prefer binding/commands over UI logic in code-behind.
- Services:
  - Interfaces in `Services/Interfaces/*`
  - Implementations in `Services/*`
  - Wired in `App.xaml.cs` via DI.
- Configuration:
  - `appsettings.json`
  - `Config/regions.json`
  - `Config/default-hosts.txt`

## Important Entry Points

- App startup and DI graph: `App.xaml.cs`
- Main user flow and hosts apply/revert: `Windows/MainWindow.xaml.cs`
- Connection monitoring window: `Windows/ConnectionInfoWindow.xaml.cs`
- Update check dialog: `Windows/UpdateDialog.xaml.cs`
- Settings dialog: `Windows/SettingsDialog.xaml.cs`

## Current Service Conventions

- Hosts operations: `IHostsFileService`
- Network probing (ping/dns): `INetworkProbeService`
- Toast notifications: `INotificationService`
- Localization abstraction: `ILocalizationService`
- Timer creation: `IDispatcherTimerFactory`

## UI/UX Rules

- Do not use modal alert windows for normal success/warning flows.
- Prefer toast notifications via `INotificationService`.
- Toast visual rendering should use `Controls/ToastHostControl`.
- Toast placement default: bottom-center.
- Keep code-behind focused on UI orchestration and event wiring.

## Localization Rules

- User-visible strings must be localized through resource files:
  - `Resources/Strings.resx`
  - `Resources/Strings.ru.resx`
- Add keys to both locales together.
- Prefer `ILocalizationService` for runtime text retrieval.

## Hosts and Network Safety

- Do not overwrite arbitrary user hosts content.
- Change only app-managed section via section marker builder.
- Always keep backup behavior for hosts changes.
- Flush DNS after hosts write.
- Use `INetworkProbeService` and `IDispatcherTimerFactory` instead of direct ad-hoc networking/timers in UI.

## Change Strategy

- Prefer minimal, reversible changes over broad rewrites.
- Reuse existing services before creating new abstractions.
- Remove dead code only after confirming no references.
- Keep repeated UI patterns in shared controls, not duplicated XAML.

## Quality Gates Before Finish

Run after substantial changes:

1. `dotnet build`
2. `dotnet test` (or `dotnet test --no-build` if build artifacts are already valid and file lock appears)
3. Smoke checks:
   - MainWindow Apply/Revert flows
   - Settings Apply behavior
   - ConnectionInfo window updates
   - Update dialog checks/download action

## Non-Functional Guardrails

- Avoid committing local artifacts (`.vs`, `bin`, `obj`, temp logs).
- Keep commits scoped to one concern.
- Preserve existing behavior unless user requested change.
