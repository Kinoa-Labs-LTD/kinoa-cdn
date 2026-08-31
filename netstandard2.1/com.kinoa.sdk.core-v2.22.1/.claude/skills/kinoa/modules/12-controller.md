# Game Controller

## Sample File(s)
- `Controllers/KinoaGameController.cs`

## Integration Notes
- **Import the controller as-is from the sample.** The default flow includes: SDK init, Messaging init, login, get player state, open session, session_start (Sync API), Feature Settings download, background retry.
- **Feature Settings download** is included by default. If the developer does not use Feature Settings, remove the `DownloadFeatureSettingsAsync()` call and related usings.

## Merge Surfaces

In Phase 6 `--merge` mode, the controller's **method bodies are frozen** — the `LogInAndOpenSessionAsync` flow, `OpenGameSessionAsync` body, `EnsureGameSessionOpen` retry path, `InitializeAndOpenSessionAsync` body, and the parallel `Task.WhenAll` invocation order all stay byte-identical to the sample. Re-run shortcut: `/kinoa controller --merge`.

The controller hosts call sites that delegate to other modules. Their carve-out surfaces are governed by the respective modules:
- `DownloadFeatureSettingsAsync()` request keys → see `modules/07-feature-settings.md` §"Merge Surfaces"
- `DownloadTranslationsAsync()` language + group keys → see `modules/09-translations.md` §"Merge Surfaces"

### Editable surfaces

#### Bootstrap wiring — game-side entry point for Kinoa init

**This is the most critical game-side wire — without it, Kinoa never runs.** `KinoaGameController` ships as `: MonoBehaviour` by default, expecting Unity to invoke `Start()` on a scene-attached component. If the game's bootstrap is plain C# / DI / service-locator (not scene-MonoBehaviour driven), `Start()` never fires and the SDK stays uninitialized.

**Discovery probe — mandatory pre-walk for this module.** Grep client code (outside `Assets/Scripts/Kinoa/`, excluding `Library/`, `Packages/`, `KinoaPackages/`) for bootstrap class patterns:
- `Loading*` — e.g., `LoadingController`, `LoadingScreen`, `LoadingManager`
- `Boot*` — e.g., `BootController`, `Bootstrap*`, `BootstrapInstaller`
- `App*` — e.g., `AppController`, `AppManager`, `AppLifecycle`
- `Main*` — e.g., `MainController`, `MainEntryPoint`
- `Game*` — e.g., `GameLifecycle`, `GameInitializer` (distinct from generated `KinoaGameController`)
- DI patterns — `Installer`, `MonoInstaller`, `ScriptableObjectInstaller` (Zenject), `ServiceLocator`, `Container.Register*`

Also detect dominant controller pattern in the game:
- **Scene-MonoBehaviour pattern** — controllers extend `MonoBehaviour`, use `[SerializeField]` fields, scene-rooted GameObjects, `Awake`/`Start` lifecycle.
- **Plain C# / DI pattern** — controllers are plain classes, instantiated via DI container or service locator, methods called explicitly from a bootstrap orchestrator.
- **Mixed** — both patterns coexist.

**Open Modify gate at controller carve-out — pose the integration-path decision verbatim:**

> *"Your bootstrap pattern looks like `<detected pattern, e.g., 'Zenject DI with BootstrapInstaller calling Initialize() on plain-C# services'>`. `KinoaGameController` integration:*
> *(a) **MonoBehaviour scene-attached** (default sample shape) — keep `: MonoBehaviour`, attach the component to a GameObject in your bootstrap scene; Unity invokes `Start()`, which calls `InitializeAndOpenSessionAsync()`. Recommended if your other controllers are scene-attached MonoBehaviours.*
> *(b) **Non-MonoBehaviour singleton** — drop `: MonoBehaviour`, drop the `Start()` method, drop the `overlay` field, and call `KinoaGameController.Instance.InitializeAndOpenSessionAsync()` from your existing bootstrap (`<discovered file:line>`). Recommended if your bootstrap is plain C# / DI / service-locator.*"

**No "hybrid extract orchestration to service" option** — it's overengineering. Pick (a) or (b).

**Apply (b) — non-MonoBehaviour migration steps** (`--merge` performs these as a single in-place edit on `KinoaGameController.cs`, with explicit Modify-gate confirmation):

1. Class declaration: `public class KinoaGameController : MonoBehaviour` → `public class KinoaGameController : KinoaSingleton<KinoaGameController>` (uses sample-shipped `Utils/KinoaSingleton.cs`).
2. Drop `[SerializeField] public GameObject overlay;` (or convert to nullable / settable property if dev wants to keep it for future use).
3. Drop `private async void Start() { await InitializeAndOpenSessionAsync(); }`.
4. Open paral-call Apply gate at the discovered bootstrap entry-point (`<file:line>`) to insert `await KinoaGameController.Instance.InitializeAndOpenSessionAsync();` next to existing init steps (Parallelism — do NOT remove existing inits, add alongside).

Note: leave `using UnityEngine;` directive untouched — even if no Unity types remain in scope after migration, the orphan using is harmless (compile warning at most) and removing it adds risk of touch-once-need-twice if a Unity type re-enters scope later.

**Apply (a) — surface in closing-summary `Unresolved`:** *"Attach `KinoaGameController` to a GameObject in your bootstrap scene (typically the bootstrap scene's root). Without scene attachment, `Start()` never fires and Kinoa session never opens."*

**Frozen-scope exception for option (b):** the class declaration line, `Start()` method, and `overlay` field are otherwise frozen. Option (b)'s in-place edits to these specific lines are explicitly permitted by this rule; all other body lines (`InitializeAndOpenSessionAsync` body, `LogInAndOpenSessionAsync` flow, `OpenGameSessionAsync`, `EnsureGameSessionOpen`, `InitializeServicesAsync`, `DownloadFeatureSettingsAsync`, `DownloadTranslationsAsync` bodies) stay frozen.

### Frozen (no in-place edits, except the bootstrap-wiring exception above)
- `InitializeAndOpenSessionAsync` body (init guard, overlay scope, parallel `Task.WhenAll`)
- `LogInAndOpenSessionAsync` flow (Step 1-N order — login → state → session → session_start → optional FS → background retry)
- `OpenGameSessionAsync` orchestration (response handling, session_start trigger)
- `EnsureGameSessionOpen` retry behavior
- `InitializeServicesAsync` (SDK init + Messaging init order)
- Session ID preservation via `Kinoa.GameSession.ActiveSession`

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not — except the explicitly-named bootstrap-wiring exception above).

## Dashboard

### Dashboard dependencies — instance types

This module has no Dashboard-configured instance dependencies of its own — `KinoaGameController` is pure orchestration. Dashboard dependencies surfaced through controller-hosted call sites are governed by the respective modules:
- `DownloadFeatureSettingsAsync()` request keys → see [`modules/07-feature-settings.md` §Dashboard](07-feature-settings.md#dashboard)
- `DownloadTranslationsAsync()` language + group keys → see [`modules/09-translations.md` §Dashboard](09-translations.md#dashboard)
- `LogInPlayer()` / Player ID writes → no Dashboard registration; `Kinoa.Player.ID` is auto-managed (see [`modules/02-player.md`](02-player.md))
- Session lifecycle (`OpenSessionAsync` / `SendSessionStartEventAsync`) → governed by [`modules/01-init.md` §Dashboard](01-init.md#dashboard) (GameID/GameToken pair) and [`modules/04-events-async.md` §Dashboard](04-events-async.md#dashboard) / [`modules/05-events-sync.md` §Dashboard](05-events-sync.md#dashboard) (events)

### Notes
- The controller never has controller-local Dashboard items to surface in the closing summary — defer to the per-module §Dashboard tables of the call sites listed above.
- Modifying controller orchestration (Step ordering, FS / Translations call placement) is scope-violating in `--merge` — go through `--fresh` instead.

## Key APIs
- `KinoaSdkInitService.Instance.InitializeAsync()` — initialize the Kinoa SDK
- `KinoaMessagingService.Instance.InitializeAsync()` — initialize in-app messaging
- `KinoaPlayerAccountService.Instance.LogInPlayer()` — set the active player ID
- `KinoaPlayerStateService.Instance.GetPlayerStateAsync()` — get current player state
- `KinoaGameSessionService.Instance.OpenSessionAsync(gameSessionData, playerState, useRetryNetworkConfiguration)` — open a game session
- `KinoaSyncGameEventsService.Instance.SendSessionStartEventAsync()` — send session_start via Sync API (inbox state in response)
- `Kinoa.GameSession.ActiveSession` — access the currently active game session for retry

## Overview
The Game Controller ties together SDK initialization, player login, and session management. It is the entry point for the Kinoa integration. The controller follows a strict sequential flow:

```
InitializeServicesAsync() → LogInAndOpenSessionAsync():
  Step 1: LogInPlayer()
  Step 2: GetPlayerStateAsync()
  Step 3: OpenGameSessionAsync() → SendSessionStartEventAsync()
  Step 4: DownloadFeatureSettingsAsync() (optional)
  Step 5: EnsureGameSessionOpen() (background retry)
```

Session start uses **Sync API by default** — returns the In-app inbox state in the response, no separate `GetInboxMessagesAsync` call needed.

## Best Practices
- Follow the initialization order strictly — APIs will fail or queue if called out of order
- Use `KinoaOverlay` (or your own loading UI) to block interaction during initialization
- Always call `EnsureGameSessionOpen()` after the initial session open — handles failures transparently
- Use `Kinoa.GameSession.ActiveSession` for retry to preserve the same Session ID
- Feature Settings download should happen after session is opened

## Configuration Notes (what's NOT in the sample)
- **Two integration paths (pick the one matching your bootstrap pattern — see §"Merge Surfaces" → "Bootstrap wiring"):**
  - **MonoBehaviour scene-attached** (default sample shape): attach `KinoaGameController` to a `GameObject` in the scene. Assign the `overlay` field in the Inspector (or leave null if no loading UI needed). Unity invokes `Start()` → `InitializeAndOpenSessionAsync()`.
  - **Non-MonoBehaviour singleton**: drop `: MonoBehaviour`, drop `Start()`, drop `overlay`, switch to `: KinoaSingleton<KinoaGameController>`. Call `KinoaGameController.Instance.InitializeAndOpenSessionAsync()` from your bootstrap.
- **One controller instance** — the `IsInitialized` guard inside `InitializeAndOpenSessionAsync` prevents double initialization regardless of integration path.
- `GameSessionData` — constructed with defaults (`new GameSessionData()`); auto-generates a GUID as Session ID
- `GameSessionData.IsOpened` — indicates if the session was successfully opened on the server
- `useRetryNetworkConfiguration: true` — switches from fast-fail config to background retry config (unlimited attempts)
- `KinoaOverlay` — implements `IDisposable` for `using` blocks (show/hide loading UI)

## Common Mistakes
- Calling APIs out of order (e.g., opening session before login or player state)
- Not calling `EnsureGameSessionOpen()` after the initial session open
- Creating a new `GameSessionData()` in the retry instead of using `Kinoa.GameSession.ActiveSession` (duplicates Session ID)
- Forgetting to initialize messaging before session operations
- Not using a loading overlay during initialization — users interact before SDK is ready
