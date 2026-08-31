# Game Session

## Sample File(s)
- `Services/KinoaGameSessionService.cs`

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces in the generated Kinoa base. All other code in `KinoaGameSessionService.cs` stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa session --merge`.

### Editable surfaces

**`firstTryNetworkConfig` and `secondTryNetworkConfig` parameters** (private readonly `NetworkConfiguration` fields used for fast-fail first attempt and unlimited background-retry second attempt respectively):
- `networkTimeout` (sample: 30s) — request timeout
- `retryStrategy` (sample: `RetryStrategy.Exponential`) — Linear / Exponential / etc.
- `maxRetryAttempts` (samples: `1` for first-try fast-fail, `int.MaxValue` for second-try unlimited)
- `maxRetryDelay` (samples: `1s` for first-try, `15s` for second-try)

These tune retry behavior at session-open time. The developer may need different timeouts / attempt counts / delay caps for their specific UX and network conditions.

### Frozen (no in-place edits)
- `OpenSessionAsync` body (the dispatch logic that picks first/second config based on `useRetryNetworkConfiguration` flag, calls `Kinoa.GameSession.OpenSessionAsync`, applies server state, logs response)
- `Kinoa.GameSession.OpenSessionAsync` call signature
- `KinoaPlayerStateService.Instance.PlayerState = response.Data.PlayerState` write logic
- `Log` helper and response-status handling

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`modules/12-controller.md` §"Merge Surfaces"](12-controller.md#merge-surfaces) — session-open orchestration (when the session opens relative to login, state hydration, FS download, retry) lives in the controller's startup flow. The retry-config tuning here affects timing within that flow.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [03 - Game Session](https://kinoa.atlassian.net/wiki/spaces/KW/pages/244318209/03+-+Game+Session+latest+version) — full API reference

## Dashboard

### Dashboard dependencies — instance types

This module has no Dashboard-configured instance dependencies of its own. `Kinoa.GameSession.OpenSessionAsync` operates against the GameID/GameToken pair from [`modules/01-init.md` §Dashboard](01-init.md#dashboard); session lifecycle (creation, open/close, ID generation) is fully SDK-managed with no per-session entries on Dashboard.

### Notes
- Server-side Session records are auto-created for every successful `OpenSessionAsync` and are observable in the [Players](https://dashboard.kinoa.io/players) view (per-player Activity / Events history) — no developer registration step required.
- Game events sent within an opened session reference Dashboard-configured instances governed by [`modules/04-events-async.md` §Dashboard](04-events-async.md#dashboard) and [`modules/05-events-sync.md` §Dashboard](05-events-sync.md#dashboard) — those tables surface in the closing-summary, this module does not.

## Key APIs
- `Kinoa.GameSession.OpenSessionAsync(gameSessionData, playerState, networkConfig, cancellationToken)` — opens and registers a new game session on Kinoa side
- `Kinoa.GameSession.ActiveSession` — gets the current active game session; use to reopen with the same `session_id` on retry

## Overview
Game Session is a mandatory initialization step. It manages session lifecycle, identifies SDK events/requests with a session ID, and synchronizes Player State with the server.

Opening a session does **NOT** send a `session_start` game event — call it separately after session open.

### Player State and OpenSessionAsync

**The game is the source of truth for Player State.** The `playerState` parameter is optional:

- **playerState provided:** Merged with server state. Always pass the latest local state, especially after offline periods.
- **playerState is null:** Server state returned as-is. For new players — a new state is created server-side.

The response always contains the actualized (merged) Player State including server-side `CalculatedFields` and `ActivityStats` — update your local reference from `response.Data.PlayerState`.

## Best Practices
- Open a session after SDK init and player ID assignment
- Open a new session when player ID changes (account switch)
- Always pass the most up-to-date local player state
- Use a two-try network strategy: first try with fast fail (e.g., 1 attempt, 1s max delay — lets the game proceed quickly), then background retry with unlimited attempts and exponential backoff (e.g., 15s max delay) using `Kinoa.GameSession.ActiveSession` to keep the same session ID
- After session open, send `SessionStartEvent` separately (Sync API recommended)

## Configuration Notes (what's NOT in the sample)
- **Offline Events:** Events accumulated in local storage from a previous session have a different `session_id`. They are sent to the server and appear in history as offline session events. In-apps do **not** trigger on offline events by default — only on events from the current session. To enable In-app triggers on offline events, check **"Support offline mode"** when configuring the In-app on the Dashboard.
- **SDK requests blocked:** Most SDK requests are blocked until the new game session is successfully registered.
- **ActiveSession for retry:** Use `Kinoa.GameSession.ActiveSession` to get the current session data (ID, state) and reopen with the same `session_id` if the first attempt failed. This prevents creating duplicate sessions.
- **GameSessionData:** Constructed with defaults (auto-generated GUID as ID). Can be customized if needed.

## Common Mistakes
- Forgetting to send `SessionStartEvent` after opening a session (session open != session start event)
- Not updating the local player state from `response.Data.PlayerState`
- Opening a session before setting `Kinoa.Player.ID`
- Opening a session before SDK initialization completes
- Using the same network config for first try and background retry (first try should fail fast)
- Creating a new `GameSessionData()` for retry instead of using `Kinoa.GameSession.ActiveSession` (causes duplicate sessions)
