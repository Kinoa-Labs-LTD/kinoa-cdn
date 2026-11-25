# Kinoa Time

## Sample File(s)
- `Services/KinoaTimeService.cs`

## Integration Notes
- **Import the sample as-is.** `KinoaTimeService` is client-ready — no transformation rules apply (no generics, no internal-only markers, no test credentials, no Dashboard-context literals). `cp` it per SKILL.md §"Generation Strategy" and generate nothing else.
- **No aggregator wiring.** `KinoaTimeService` has no usage in the `KinoaGameController` sample, so nothing is inserted into any aggregator when Time is generated — the service is a singleton the game calls from its own logic. Time's only init-time surface, `TimeConfiguration`, already ships inside `KinoaSdkInitService`, which the always-on Init module generates. See SKILL.md §"`time` argument".
- **Two customization points** (surface both in the Phase 5 summary):
  1. `new TimeConfiguration(useServiceTime: true)` in `KinoaSdkInitService.InitializeAsync()` — the sample ships `true`. Service time is consulted **only** when this flag is `true`; with `false`, every `Kinoa.Time` read returns device UTC and `IsServiceTime` is always `false`.
  2. The `OnKinoaServiceTimeUpdated()` handler body in `KinoaTimeService` — the sample only `Debug.Log`s. Replace it with the game's re-anchor logic: recompute visible countdowns, re-evaluate expiry checks, refresh anything that was rendered from device time during bootstrap.
- **Service time is not available at construction.** Subscribing in the `KinoaTimeService` constructor (as the sample does) is the point: the first sample is requested on `session_start` and completes some hundreds of milliseconds later, so anything read during bootstrap is device time. Read through `GetKinoaUnixTime` and branch on `IsServiceTime` instead of assuming.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa time --merge`.

### Cross-module dependencies
- [`01-init.md`](01-init.md) — `TimeConfiguration` is an `Kinoa.SDK.Initialize` parameter constructed inside `KinoaSdkInitService`. The `useServiceTime` surface below is physically inside module 01's file, so module 01's §"Merge Surfaces" rules govern how that file may be edited. Load it before walking this module's surfaces.

### Editable surfaces

**`useServiceTime` flag value** — `new TimeConfiguration(useServiceTime: true)` in `KinoaSdkInitService.InitializeAsync()`. A single Modify gate: keep `true` (recommended — the game gets a clock the player cannot move) or switch to `false` (device time only, no TimeAPI request). Nothing else in the `TimeConfiguration` construction line changes.

**`OnKinoaServiceTimeUpdated()` handler body** in `KinoaTimeService` — sample `Debug.Log` stub replaced with the game's re-anchor logic. Body extension per SKILL.md §"Frozen-scope philosophy": preserve the sample-shipped trace point, add alongside it.

**`IsServiceTimeAvailable()` call sites** — if the game already has a "is our clock trustworthy?" concept (a `ServerTimeProvider`, an `NtpClient`, a `GetServerNow()` helper), route it through `KinoaTimeService.Instance.IsServiceTimeAvailable()` / `Kinoa.Time.GetKinoaUnixTime` rather than keeping two independent clocks. Standard per-edit Apply/Skip/Modify gate at each site. Do **not** delete the game's existing implementation — comment out and add alongside, per the frozen-scope philosophy.

### Frozen (no in-place edits, except where body-extension applies)
- Method **signatures** of `IsServiceTimeAvailable` / `OnKinoaServiceTimeUpdated` in `KinoaTimeService` — strict frozen
- `Kinoa.Time.*` SDK call signatures — strict frozen
- The constructor's `Kinoa.Time.OnKinoaServiceTimeUpdated += OnKinoaServiceTimeUpdated;` subscription and its placement — strict frozen. Subscribing later than construction means missing the first `ServiceTimeUpdated` raise.
- `KinoaCurrentTime` — strict frozen, and not editable in any case: `UnixTime`, `IsServiceTime` and `HasMilliseconds` all have `internal` setters, so the SDK is the only writer.
- The `TimeConfiguration` **construction site** (which file, which position relative to `Kinoa.SDK.Initialize`) — strict frozen; only the `useServiceTime` argument value is a surface.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [01 - SDK Initialization](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275687/01+-+SDK+Initialization+latest+version) — `TimeConfiguration` is documented there as an `Initialize` parameter, alongside the other configuration classes.
- [com.kinoa.sdk.core](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275521/com.kinoa.sdk.core+latest+version) — SDK landing page.

> There is no dedicated KW page for Kinoa Time. This module is the authoritative local reference for the `Kinoa.Time` facade; the Initialization page covers only the configuration flag.

## Dashboard

### Dashboard dependencies — instance types

This module has no Dashboard-configured instance dependencies. Service time is served by the Kinoa TimeAPI for every game with no per-game configuration — there is nothing to create, name, or version on the Dashboard.

### Notes
- Time defines no mirrorable entity (no events, no player fields, no feature settings, no resources), so a `time`-scoped run **skips the Dashboard Sync gate** entirely. Nothing about Time ever appears in a `/kinoa dashboard-sync` manifest.
- `useServiceTime` is a **client-side** decision made in code, not a Dashboard toggle. There is no server-side override.

## Key APIs
- `Kinoa.Time.GetUnixTime(bool milliseconds = false)` — returns `long`. Seconds since `1970-01-01T00:00:00Z` by default; milliseconds when `true`. Silently falls back to device UTC.
- `Kinoa.Time.GetKinoaUnixTime(bool milliseconds = false)` — returns `KinoaCurrentTime`. Same value as above **plus** the provenance of it.
- `Kinoa.Time.IsServiceTimeAvailable()` — returns `bool`. `true` once a valid service-time sample has been applied.
- `Kinoa.Time.OnKinoaServiceTimeUpdated` — `event Action`, raised **after** each valid sample is applied, so handlers always observe available service time.
- `KinoaCurrentTime` — `UnixTime` (`long`), `IsServiceTime` (`bool` — `false` means the value is device UTC), `HasMilliseconds` (`bool` — echoes the `milliseconds` argument, so the unit travels with the value). All three have `internal` setters.
- `TimeConfiguration(bool useServiceTime = false)` — the 5th argument of `Kinoa.SDK.Initialize(...)`. Exposes `UseServiceTime`. **Defaults to `false`**; the sample passes `true`.

## Overview
`Kinoa.Time` is a server-anchored wall clock with automatic device-time fallback. The SDK samples the Kinoa TimeAPI, computes the offset between service time and device time, and every read returns `device UTC + offset`. Games use it for anything a player could cheat or an unset device clock could break: in-app countdown timers, capping and cooldown windows, mission and offer deadlines, client-stamped event times.

It is a **corrected wall clock, not a monotonic clock.** Reads are derived from `DateTimeOffset.UtcNow` plus a cached offset, so they still jump if the device clock jumps between samples — the offset is only re-measured on the triggers below.

**Offset measurement.** One HTTP sample per update. The device timestamp is estimated as the *middle of the round trip*, so the residual error is at most half the round trip. The offset is stored once and reused for every read until the next successful sample.

**Update triggers** (there is no polling and no public "refresh now" method):
1. `session_start` — but only while service time is not yet available.
2. Application resume from background.

**Sample rejection.** A sample is discarded, and up to 3 attempts are made with a 1 s delay between them (5 s request timeout), when:
- the request fails;
- a suspend/resume crossed the in-flight window — the process may have been frozen for an arbitrary time, which would poison the pairing;
- the round trip exceeded 5 s — too slow to be a trustworthy service-to-device pairing;
- the wall clock and the monotonic clock diverged by more than 1 s inside the window — the device clock was changed mid-sample.

If all 3 attempts are discarded, service time stays unavailable until the next trigger. Only one update runs at a time; a concurrent request is dropped, not queued.

## Best Practices
- **Prefer `GetKinoaUnixTime` over `GetUnixTime`** whenever the answer matters. `GetUnixTime` collapses "server-anchored" and "device fallback" into one indistinguishable `long`; `GetKinoaUnixTime` hands back `IsServiceTime` so the game can decide (soft-fail a deadline, defer a reward, show a "reconnecting" state) instead of silently trusting a device clock.
- **Subscribe to `OnKinoaServiceTimeUpdated` in the constructor**, as the sample does, and re-anchor everything time-derived in the handler. Bootstrap UI is rendered before the first sample lands, so a countdown drawn at startup is drawn from device time and needs correcting once.
- **Read once per frame / per operation, pass the value down.** Each call is a fresh `UtcNow + offset`, so two reads inside one logic step can straddle a second boundary and produce off-by-one comparisons.
- **Use `milliseconds: true` for durations and progress bars, seconds for timestamps** compared against server-issued values. Carry `HasMilliseconds` alongside the value rather than re-deriving the unit at the consumer.
- **Set `useServiceTime: true`** unless the game genuinely has no time-sensitive state. The cost is one small HTTP request per session.
- **Treat `IsServiceTimeAvailable() == false` as "not yet", not "never".** A resume or the next session retries.

## Configuration Notes (what's NOT in the sample)
- **`TimeConfiguration` defaults to `useServiceTime: false`.** `new TimeConfiguration()` — and passing `null` for the parameter, which the SDK replaces with a default instance — means device time only, no TimeAPI request at all. The sample's explicit `true` is what turns the feature on.
- **The offset is never reset once applied.** Service time stays "available" for the rest of the process lifetime even if later samples are all discarded; it just stops being refreshed. `IsServiceTimeAvailable()` answers "has a valid sample ever been applied", not "is the network up".
- **No public forced refresh.** The internal update is triggered only by `session_start` and application resume. A game that needs a fresher anchor gets it by backgrounding and resuming, not by an API call.
- **Second-precision reads floor the offset.** With `milliseconds: false` the offset is integer-divided by 1000 before being added, so a sub-second offset contributes nothing at second precision.
- **Thread safety.** The offset is read and written with interlocked operations — the sample is applied on the network thread, reads happen on the game thread. `Kinoa.Time` reads are safe from any thread; `OnKinoaServiceTimeUpdated` is raised on the thread that completed the sample, so marshal to the main thread before touching Unity objects in the handler.

## Important Notes
- **`Kinoa.Time` is safe to call before `Initialize()` completes**, but it will return device time: no configuration is set and no sample has run yet. It is *not* part of the deferred-execution queue that other `Kinoa.SDK` methods use — reads are answered immediately with whatever is known.
- **In-app countdown timers are evaluated against this clock.** A game that ships `useServiceTime: false` gives players a trivially exploitable way to extend or skip in-app countdowns by moving the device clock.
- **Offline mode returns device time silently.** There is no exception and no error response — only `IsServiceTime == false` distinguishes the two.

## Common Mistakes
- Using `GetUnixTime` for a deadline check and treating the result as authoritative — with no service-time sample applied, that is the device clock, unverified.
- Reading time once at startup and caching the `long` for the session, instead of reading per use — the value is a timestamp, not a clock.
- Assuming `IsServiceTimeAvailable()` is `true` immediately after `Initialize()` returns. The first sample is requested on `session_start` and lands afterwards; `OnKinoaServiceTimeUpdated` is the signal, not init completion.
- Leaving `TimeConfiguration` at its default (or passing `null`) and then wondering why `IsServiceTime` is always `false` — the feature is opt-in.
- Touching Unity objects directly in the `OnKinoaServiceTimeUpdated` handler without marshalling to the main thread.
- Mixing seconds and milliseconds across a comparison because `HasMilliseconds` was discarded when the value was stored.
- Expecting the offset to correct a device clock change *between* samples. It will not — only `session_start` (while unavailable) and resume re-measure.
