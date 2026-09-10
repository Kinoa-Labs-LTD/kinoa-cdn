# P2P Events

## Sample File(s)
- `Services/KinoaP2PEventsService.cs`

## Integration Notes
- **Import all methods from the sample as-is** (Get, Send, Delete + callbacks). Do not ask — always include everything.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces in `KinoaP2PEventsService.cs`. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa p2p --merge`.

### Editable surfaces

**`MockedEventData` class (or its equivalent)** — replace with the developer's real P2P payload class. The replacement class shape is dictated by the game's P2P feature design, NOT by Kinoa.

If the game has no client-side P2P payload class yet, **Skip + surface to Unresolved** — invite the developer to design the payload class first, then re-run `/kinoa p2p --merge`.

**P2P event-key literals** — sample placeholder values follow the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates".

### Frozen (no in-place edits, except where body-extension applies)
- Method **signatures** of `Send` / `Get` / `Delete` in `KinoaP2PEventsService.cs` — strict frozen
- `Kinoa.P2PEvents.*` SDK call signatures and parameter shapes — strict frozen
- The order in which the SDK is invoked relative to callback registration — strict frozen
- `Send` / `Get` / `Delete` method bodies — **body extension allowed** per SKILL.md §"Frozen-scope philosophy" (preserve key moments: SDK call invocation, callback dispatch, response-status check, sample-shipped trace points; do not rewrite wholesale). Typical extensions: logging, error formatting, payload (de)serialization details, debug instrumentation.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [05 - P2P (Player to Player) Events](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275809/05+-+P2P+Player+to+Player+Events+latest+version) — full API reference

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **P2P Event Type** (with its payload schema) | the `eventType` string + `eventData` object (any client-defined class — sample `MockedEventData`, replaced at `--merge`) passed to `new OutgoingP2PEvent(targetPlayerID, eventType, eventData)` at `Kinoa.P2PEvents.Send(...)` call sites; the matching `IncomingP2PEvent` shape consumed in `Kinoa.P2PEvents.Get(...)` callbacks | [Game Settings → P2P Events](https://dashboard.kinoa.io/game-settings/events/p2p) | Each event-type string the client sends or receives should be registered on Dashboard for it to be usable in Dashboard-side configuration. *Example utility*: trigger a push notification to the receiving player when a matching P2P event with a specific payload-field value arrives. The payload class is fully client-defined — there are NO predefined base fields (unlike game events in [`modules/04-events-async.md`](04-events-async.md)); the entire payload is serialized by the SDK from the `eventData` object into the `EventData` JSON property of `OutgoingP2PEvent`. For Dashboard utility, every field of the payload class that should be referenceable on Dashboard must be registered on the matching P2P Event Type entry with its name and type. Sender and receiver must also agree on the payload schema out-of-band — Kinoa does not enforce inter-client compatibility. |

### Notes
- `ReloadP2PCommand` delivery via WebSocket requires `KinoaMessagingService` initialized — without it, online notifications won't arrive even if the P2P type is registered.
- P2P events persist server-side until explicitly deleted via `Kinoa.P2PEvents.Delete(...)` — Dashboard registration governs Dashboard-side utility, not lifecycle.
- Field-name keys on the Dashboard P2P Event Type entry must match the JSON-serialized property names of the payload class (SDK uses `SnakeCaseLower` naming policy by default, so `MyField` in C# becomes `my_field` on Dashboard).

## Key APIs
- `Kinoa.P2PEvents.Send(OutgoingP2PEvent)` — fire-and-forget, no callback
- `Kinoa.P2PEvents.Get(Action<Response<List<IncomingP2PEvent>>>)` — retrieve incoming events
- `Kinoa.P2PEvents.Delete(List<string> ids, Action<Response>)` — delete events by ID

All APIs are **callback-based** (`Action<Response<T>>`), not async/await.

## Overview
P2P events let players send data to each other (attacks, gifts, trades, etc.). Each event carries a target player ID, event type string, and a custom JSON payload.

**Delivery:**
- **Target online** — Kinoa stores the event and sends `ReloadP2PCommand` via WebSocket (`KinoaMessagingService.OnCommandReceived`). Handle by calling `Kinoa.P2PEvents.Get()`.
- **Target offline** — events queue on the server; available on next `Kinoa.P2PEvents.Get()` after login.
- **Sender offline** — `Send` queues locally (same as async game events — see 04-events-async) and sends when connection is restored.

## Best Practices
- Use strongly-typed classes for payloads (serialized as JSON)
- Process first, delete after — never delete before processing
- After processing, update local Player State and send a game event to sync with the server
- Check `response.IsSuccessful()` before accessing `response.Data`

## Important Notes
- **Events persist until explicitly deleted.** `Kinoa.P2PEvents.Get()` returns the same events on repeated calls — always `Kinoa.P2PEvents.Delete` after processing.
- **`ReloadP2PCommand` requires initialized `KinoaMessagingService`** — without it, online notifications won't arrive.
- **Payload is opaque to Kinoa** — no server-side validation. The game defines and interprets the data.
- **Sample uses `MockedEventData`** — replace with your actual classes.

## Common Mistakes
- Forgetting to delete events after processing — duplicate processing on next `Get`
- Deleting before processing — if processing fails, events are lost
- Not sending a game event after processing — Kinoa won't record the player state change
- Not checking `response.IsSuccessful()` in callbacks
