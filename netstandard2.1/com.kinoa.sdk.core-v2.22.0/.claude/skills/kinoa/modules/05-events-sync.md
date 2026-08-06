# Sync Game Events

## Sample File(s)
- `Services/KinoaSyncGameEventsService.cs`
- `Services/KinoaGameEventBuildingService.cs`
- `Services/KinoaUiService.cs` — stub UI service (see Code Transformation Rules)

## Integration Notes
- **Wizard mode — ask once (2 options, single-select):**
  - `None (recommended)` — trim to `session_start` + `SendCustomEventAsync` only. `session_start` is mandatory.
  - `All predefined` — keep the full sample set: Progression, LevelUp, WatchAd, InGamePurchase, Tutorial, CollectedResource, Social*, Error, InApp*.
- **`--auto` mode:** skip the question entirely and **generate all events from the sample as-is** — do NOT trim to `None`. Developer can delete unused methods later if they wish.
- Use Sync API only when in-app response is needed immediately; prefer Async otherwise.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the editable surfaces below across `KinoaSyncGameEventsService.cs` and `KinoaUiService.cs` (the latter governed by `modules/06-messaging.md` §"Merge Surfaces" — sync flow calls into UI stubs but the UI implementation lives there). All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa sync-events --merge`.

### Editable surfaces

#### Adding new `Send<NewEvent>Event(...)` methods

**Only if** a corresponding builder was added to `KinoaGameEventBuildingService` (governed by `modules/04-events-async.md` §"Merge Surfaces"). Call pattern follows the sample's existing events. Do not refactor other event methods' bodies.

**Sync-vs-async criterion:** add a Send method here (sync) **only when the call site must wait for an SDK response before proceeding** — e.g., server-acknowledged session_start (the canonical sync use case), an in-app trigger that must block UI until inbox state is returned, or any flow that needs the server's response inline.

For **fire-and-forget analytics mirroring** where the call site doesn't need a response (the typical case for custom-event mirrors of an existing analytics taxonomy), use the async path in `KinoaGameEventsService` instead. See `modules/04-events-async.md` §"Merge Surfaces" for the analytics-mirror coverage gate, parameter-name reuse, and constants consolidation rules.

**Mirroring the same event into BOTH services is almost always wrong — pick one.**

#### `KinoaUiService` calls from sync flow

Sync responses carry inbox state and trigger immediate In-app display through `KinoaUiService`. When a sync flow needs new UI capability:
- The implementation goes on `KinoaUiService` per the architectural rule that UI implementation lives **only** in `KinoaUiService` (single facade) — not inlined into the sync service body.
- `KinoaUiService` stub bodies and new method additions are governed by `modules/06-messaging.md` §"Merge Surfaces".
- Only the **call site** from sync into `KinoaUiService` is editable on this side (e.g., adding `KinoaUiService.Instance.DisplayInApp(inApp)` at the orchestration layer of a new sync Send method).

### Frozen (no in-place edits, except where body-extension applies)
- Post-SDK-call sections of every `Send<X>EventAsync` method (response handling, `IsSuccessful` check, `ProcessResponse` invocation, inbox processing) — Frozen verbatim. Pre-SDK-call statements ARE editable in **any** event method per SKILL.md §"Body extension on SDK-wrapper service methods" — this is a legitimate location for PlayerState refresh (Pattern A in [`modules/02-player.md` §"Snapshot vs runtime mutation"](02-player.md#kinoaplayerstateservicegetlocalplayerstateasync-body)). `session_start` has no special status — it's one event among many for PlayerState sync; body-extension permission is symmetric across all `Send<X>EventAsync` methods. The choice between Pattern A (pre-event refresh here) and Pattern B (mutation-site writes from game code) is made once for the whole `CustomPlayerState` per the rule in mod 02 — don't reopen that decision per event.
- Inbox-state response processing logic (`ProcessResponse` and the SDK callback chain)
- `Kinoa.SyncGameEvents.*` SDK call signatures

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`modules/04-events-async.md` §"Merge Surfaces"](04-events-async.md#merge-surfaces) — shared `KinoaGameEventBuildingService` builders, the analytics-mirror coverage gate, parameter-name reuse rule, and constants consolidation rule. Builder additions originate there; sync Send methods only get added if a matching builder exists. Sync-vs-async pick is mutually-exclusive.
- [`modules/06-messaging.md` §"Merge Surfaces"](06-messaging.md#merge-surfaces) — sync responses carry inbox state with In-apps, and processing them requires access patterns from messaging: in-app field reads, in-app method calls (update, eligibility-use), inbox operations (delete), and `KinoaUiService` for UI display. Edits in sync flow that touch in-apps must follow the messaging module's conventions; UI changes go through `KinoaUiService` (governed by 06), not inlined here.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [04 - Game Events](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275783/04+-+Game+Events+latest+version) — full API reference (covers both async and sync events)

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

Sync game events share Dashboard registration with async events — both call paths produce the same event records on the backend and are subject to identical Dashboard configuration. See [`modules/04-events-async.md` §Dashboard](04-events-async.md#dashboard) for the `Custom Event` / `Predefined Event` / `Debug Event` instance-type table and verification rules — this module surfaces no additional rows of its own.

### Notes
- Sync-vs-async pick is mutually-exclusive per-event (see §"Merge Surfaces"); the registration in 04's table covers BOTH flows — there is no separate "sync custom event" entry on Dashboard.
- `OptionalMessages` and `InboxDetails` categories returned by sync responses do NOT correspond to Dashboard-configured instances of their own — they are server-computed views over already-registered In-apps (governed by [`modules/06-messaging.md` §Dashboard](06-messaging.md#dashboard)).

## Key APIs
All methods return `Task<Response<SyncGameEventResponse>>`. Every async event from `Kinoa.GameEvents` (see 04-events-async.md) has a sync counterpart in `Kinoa.SyncGameEvents` with `Async` suffix (e.g., `SendSessionStartEventAsync`, `SendPaymentEventAsync`, etc.). No batch `SendEvents` equivalent.

## Overview
Sync game events return a `SyncGameEventResponse` containing the player's In-App inbox state. Use sync events when you need to process in-app messages at the place of execution (e.g., showing offers after level-up or purchase). Despite the "sync" name, calls are non-blocking (`async Task`).

Event data construction is identical to async events — same constructors, same setters, same mandatory/predefined/custom classification (see 04-events-async.md). The only difference is the response handling.

## Best Practices
- **Always use Sync API for `session_start`** — the inbox state returned in the response is the authoritative starting inbox for the player. Use Async for other events unless you need in-app messages in the response.
- Use sync events when you need to display in-app messages from the response
- Always check `response.IsSuccessful()` and `response.Data?.InboxDetails != null` before processing
- Recommended processing order: non-inbox → removed → replaced → new → reminders → progression → milestones → instance updates. The order can be changed depending on game needs
- Inbox categories reference:

| Category | Inbox state | Overlaps with | Action |
|---|---|---|---|
| `OptionalMessages` | Not in inbox | — | Display once, not stored in inbox |
| `NewInApps` | New entry | — | Add to display queue |
| `OldInApps` | Existing | — | No action needed (unless UUID also in a specific category below) |
| `RemovedInApps` | Removed | — | Remove from UI |
| `ReplacedInApps` | Removed | `OldInApps` | Remove from UI; replacement arrives in `NewInApps` |
| `ReminderInApps` | Existing | `OldInApps` | Re-show to player — reminder that this in-app should be displayed |
| `ProgressionScoreInApps` | Existing | `OldInApps` | Progression score incremented — refresh on the in-app UI object |
| `MilestonesProgressInApps` | Existing | `OldInApps` | Milestones progress updated — refresh on the in-app UI object |
| `UpdatedInApps` | Existing | `OldInApps` | Config/placeholders updated by operator on Kinoa Dashboard — refresh |

  **Note:** A UUID can appear in multiple lists simultaneously. For example, a reminder in-app appears in both `OldInApps` and `ReminderInApps`. Always process by specific category first.

- Use the same event data construction patterns as async events (see 04-events-async.md)
- **UI implementation is client-side:** The sample uses `KinoaUiService` as a demo reference, but each game implements its own UI logic for displaying, replacing, and removing in-apps (content loading, layout, animations, etc.)

## Configuration Notes (what's NOT in the sample)
- **SyncGameEventResponse:** Contains `InboxMessages` (full player inbox — stored on server, persists across app restarts, returned with every sync event response), `OptionalMessages` (non-inbox in-apps — displayed once and disappear), and `InboxDetails` (categorization of inbox changes).
- **InAppInboxDetails categories:** `NewInApps`, `OldInApps`, `ReplacedInApps`, `RemovedInApps`, `ReminderInApps`, `ProgressionScoreInApps`, `MilestonesProgressInApps`, `UpdatedInApps` — all are `List<string>` of UUIDs (match `InAppMessage.Uuid`).
- **Race condition:** If sync and async events are triggered in parallel for the same event, local PlayerState may temporarily differ from server state. The server state will be synchronized during the next event. Prefer using one approach consistently per event type.

## Common Mistakes
- Not checking `InboxDetails` for null before accessing its properties
- Mixing sync and async events for the same event types (causes race conditions with WebSocket delivery)
- Re-showing old in-apps on every event (showing them in an inbox UI makes sense, but don't queue them for display on each event — unless they also appear in a specific category like `ReminderInApps`, or on game start)
- Ignoring progression/milestones/instance-updated in-apps — they carry updated data for existing in-apps. Whether to re-show or just refresh depends on your game's UI design
- Ignoring `OptionalMessages` (these are non-inbox in-apps that should be shown immediately)
- Processing inbox categories in the wrong order (remove/replace before adding new ones)
- Reading `InAppMessage.Command` from Sync API response to determine what happened to the In-app — use `InboxDetails` categories instead. `InAppMessage.Command` is used for WebSocket In-apps (see 06-messaging), while Sync API uses `InboxDetails` (`ReplacedInApps`, `ReminderInApps`, etc.) to convey the same instructions
