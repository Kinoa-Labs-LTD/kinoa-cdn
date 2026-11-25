# Missions In-apps

Missions is an **In-app feature** — the same shape as Milestones: an `InAppFeature`
attached to `InAppCustomTemplateData.Feature`, delivered inside an ordinary in-app. It
has **no service file of its own** — all missions code lives in
`KinoaMessagingService`. This module is the knowledge surface; the generated code comes
from module 06.

Reached from: `/kinoa missions` (docs + the missions slice of `--merge`), or loaded
alongside [`06-messaging.md`](06-messaging.md) whenever a game uses missions in-apps.

## Sample File(s)
- `Services/KinoaMessagingService.cs` — `#region Missions` (the four endpoint wrappers),
  `ProcessMissionsProgressInApps` (WebSocket refresh), `LogMissionsFeature` (field access)
- `Services/KinoaUiService.cs` — stub UI service; missions refresh routes through
  `ReplaceGameInApps(..., addToDisplayQueue: false)` (see Code Transformation Rules)
- `Constants/KinoaInAppTemplateConstants.cs` — the game's missions `template_key` constant

## Integration Notes
- **No separate module generation.** Missions ships inside `KinoaMessagingService`. If
  messaging is already generated, the missions code is already present — `/kinoa missions`
  adds knowledge, not files.
- **A missions in-app requires a Dashboard-registered custom template** whose element
  structure carries the missions feature slot. Unlike `simple` and
  `one_cta_predefined`, there is no Kinoa-shipped missions template key — the game team
  registers its own (e.g. `mission_v1`) and adds it to
  `KinoaInAppTemplateConstants`.
- **Missions do NOT use `InAppFeatureConfiguration.Register<T>()`.** Feature
  Configurations (module 06 / 07) are a different mechanism — a schema-keyed payload on
  the in-app. The missions state arrives as a first-class typed `InAppFeature`; no
  registration step exists or is needed.
- **Progression is event-driven.** Missions advance when the player fires the
  Dashboard-configured progression event through the normal event path
  (`Kinoa.GameEvents.Send*` / `Kinoa.SyncGameEvents.Send*Async`). There is no
  "add progress" client API — `ReachMissionsProgress` jumps rows straight to their goal
  (see Key APIs) but is not the incremental path.

## Merge Surfaces

In Phase 6 `--merge` mode this module exposes the surfaces below. Re-run shortcut:
`/kinoa missions --merge`. All other code stays frozen per the cross-cutting rules in
SKILL.md §"Phase 6 — `--merge` mode".

### Editable surfaces

#### `KinoaMessagingService` `#region Missions` — new wrapper methods
Adding a wrapper alongside the four shipped ones is permitted under
alongside-permission (e.g. a `CollectAllReachedMissionsAsync` convenience that folds
every `Reached` row of the active set into one `CollectMissionsProgressAsync` call). The four
shipped wrappers' `Kinoa.Messaging.*` call signatures are frozen.

#### `ProcessMissionsProgressInApps` body
Body-extension applies: the game may branch on its own criteria before routing to the
UI (e.g. suppress the refresh while a mission popup is already open). The
`x.Command is InAppMissionsProgressChangedCommand` filter itself is frozen.

#### `KinoaInAppTemplateConstants` — missions `template_key` entry
Adding the game's Dashboard-registered missions template key is the expected `--merge`
edit. Predefined keys (`TemplateKeySimple`, `TemplateKeyOneCtaPredefined`) are frozen
server-side discriminators.

#### Mission CTA routing in the game's UI layer
See §"CTA routing" — the routing implementation is game-side, governed by
[`06-messaging.md` §"Merge Surfaces"](06-messaging.md#merge-surfaces) (Tier-1
`KinoaUiService` carve-out).

### Frozen (no in-place edits)
- The four `Kinoa.Messaging.*` missions call signatures
- `InAppMissionsProgressChangedCommand` filtering in `ProcessMissionsProgressInApps`
- All `InAppMission*` SDK types (server-driven, `private set` throughout)

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`06-messaging.md`](06-messaging.md) — the in-app lifecycle missions rides on:
  inbox vs non-inbox, `IsViewed` acknowledgement, capping / eligibility, countdown
  timer + extra-life-time, `KinoaUiService` display and replacement, the other command
  types.
- [`05-events-sync.md`](05-events-sync.md) — the sync channel's
  `InboxDetails.MissionProgressionInApps` category.
- [`04-events-async.md`](04-events-async.md) — the progression event itself must exist
  as a registered custom event with the parameters the missions rows bind to.

## Wiki Reference
- [06 - In-app Messaging](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275836/06+-+In-app+Messaging+latest+version) — in-app reference; missions shares the in-app lifecycle documented there

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules:
SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **In-app Configuration (missions)** | the trigger event sent via `Kinoa.GameEvents.Send*` / `Kinoa.SyncGameEvents.Send*Async` that creates the missions in-app | [Communications → In-Apps](https://dashboard.kinoa.io/communications/in-app) | The in-app entry must select the missions custom template and carry the missions configuration table: per-row `placement_number`, `sub_mission_number`, mission text, goal score, progress-bar score, reward resources, progression/completion CTA, store package ids. Rows may be banded by Configuration Filters (player-state columns) — the server picks the band at resolution time. |
| **In-app Custom Template (missions)** | the missions `template_key` constant in `KinoaInAppTemplateConstants`, read in `InAppMessage.Data` cast paths | [Game Settings → In-App Custom Templates](https://dashboard.kinoa.io/game-settings/in-apps) | The template must be registered with a **missions feature slot** plus the element structure the game renders. No Kinoa-shipped missions template exists — this is always a game-registered key. |
| **Progression event** | the event whose emission advances mission rows | [Game Settings → Game Events](https://dashboard.kinoa.io/game-settings/game-events) | The event must be registered, and every parameter the missions rows reference (condition parameter, `bind_with_event_parameter`) must exist on it. A row that binds to a parameter the event never sends never advances. See [`04-events-async.md` §Dashboard](04-events-async.md#dashboard). |
| **Missions milestones (progress bar)** | `InAppMissionsMilestonesProgressBar.Milestones`, collected via `CollectMissionsMilestonesProgress` | same In-app entry, progress-bar section | Each milestone step needs its row number, milestone number, threshold score, and reward button configured. Steps arrive as `InAppMissionsMilestoneStep` with `InAppMilestoneStatus`. |

### Notes
- Row banding is resolved **server-side**. `InAppMessage.ConfigurationFilters` echoes the
  player-state values used to pick the band; the client never selects a band.
- Rewards on a mission row arrive as `InAppMission.ProcessedResources`
  (`List<Resource>`) — the same `Resource` shape as bundles
  ([`08-bundles.md`](08-bundles.md)).

## Key APIs

All five are `static` on `Kinoa.Messaging`, take `IInboxMessage message` (pass the
`InAppMessage`), an `int setNumber`, a `List<int> rowNumbers`, and an optional
`CancellationToken`. `setNumber` is `MissionState.CurrentSetNumber` (or a
`PreviousSetProgress.SetNumber` still inside its extra-life window); `rowNumbers` are
`InAppMission.RowNumber` values — **not** placement or sub-mission numbers.

| API | Returns | Endpoint |
|---|---|---|
| `CollectMissionsProgressAsync(message, setNumber, rowNumbers, ct)` | `Response<InAppMessage>` | `POST /inappmessages/api/in-app-messages/missions/progress/collect/v2` |
| ~~`CollectMissionsProgress(message, setNumber, rowNumbers, ct)`~~ **deprecated** | `Response<List<InAppMissionsProgressResult>>` | `POST /inappmessages/api/in-app-messages/missions/progress/collect` |
| `ReachMissionsProgress(message, setNumber, rowNumbers, ct)` | `Response<InAppMessage>` | `POST /inappmessages/api/in-app-messages/missions/progress/reach` |
| `CollectMissionsMilestonesProgress(message, setNumber, rowNumbers, ct)` | `Response<List<InAppMissionsMilestonesResult>>` | `POST /inappmessages/api/in-app-messages/progress-bar/progress/collect` |
| `DeleteMissionsProgress(message, setNumber, rowNumbers, ct)` | `Response` (no `Data`) | `DELETE /inappmessages/api/in-app-messages/missions/progress` |

- `CollectMissionsProgressAsync` — claims the reward of rows that already reached their
  goal, and **adds each collected row's `ProgressBarScore` to
  `ProgressBarState.TotalScore`**, flipping any crossed milestone from `Closed` to
  `Reached`. **Returns the refreshed `InAppMessage`** — route `response.Data` into the
  UI instead of waiting for a WebSocket push; none follows.
- `CollectMissionsProgress` — **deprecated, use `CollectMissionsProgressAsync`.** Same
  collection, older endpoint: each `InAppMissionsProgressResult` carries `SetNumber`,
  `RowNumber`, and `Status` (`InAppMissionsProgressStatus`), and **no refreshed in-app
  is returned** — the `Collected` status has to be applied to the local model from the
  result list.
- `ReachMissionsProgress` — advances the named rows straight to
  `CurrentScore == GoalScore` (`Status = Reached`). The progress bar does **not** move
  here: `TotalScore` accrues on collection, not on reaching. **Returns the refreshed
  `InAppMessage`** — route `response.Data` into the UI instead of waiting for a
  WebSocket push.
- `CollectMissionsMilestonesProgress` — claims progress-bar milestone rewards. Note the
  **different endpoint path** (`progress-bar/progress/collect`, not
  `missions/progress/...`). Each `InAppMissionsMilestonesResult` carries `SetNumber`,
  `RowNumber`, and `Status` (`InAppMilestoneStatus`). Distinct from
  `Kinoa.Messaging.CollectMilestonesAsync` — that one collects *template* milestones
  (`InAppMilestonesFeature`), a different feature with a different type.
- `DeleteMissionsProgress` — removes the named rows from `ActiveMissions` permanently;
  they do not reappear on later progression events. Returns a plain `Response` and
  **pushes no refreshed in-app** — fire the next progression event (or call
  `ReachMissionsProgress` on another row) to obtain updated state.

### Reading the state

- `(message.Data as InAppCustomTemplateData)?.Feature as InAppMissionsFeature` — the
  entry point. `null` when the in-app is not a missions in-app.
- `InAppMissionsFeature.MissionState` → `InAppMissionsProgress`:
  - `CurrentSetNumber` (`int`)
  - `ActiveSetProgress` (`InAppMissionSetProgress`)
  - `PreviousSetProgress` (`InAppMissionSetProgress`, may be `null`)
  - `ProgressBarState` (`InAppMissionsMilestonesProgressBar`)
- `InAppMissionSetProgress`: `SetNumber` (`int`), `Status` (`InAppMissionSetStatus`:
  `Unknown` / `Active` / `Finished`), `StartTimestamp` / `EndTimestamp` (`long`,
  **milliseconds**), `ActiveMissions` (`List<InAppMission>`),
  `FinishedCommonSubmissionsRows` (`List<int>`), `CustomFields`
  (`Dictionary<string, object>`)
- `InAppMission`: `RowNumber` (`int`), `PlacementNumber` (`int`), `SubMissionNumber`
  (`int?` — `null` for common missions), `MissionText` (`string`), `CurrentScore`
  (`decimal`), `GoalScore` (`decimal`), `ProgressBarScore` (`decimal?`), `Status`
  (`InAppMissionStatus`: `Unknown` / `Active` / `Reached` / `Collected`),
  `ProgressionCta` / `CompletionCta` (`InAppClickConfiguration`), `GooglePackageId` /
  `ApplePackageId` (`string`), `ProcessedResources` (`List<Resource>`), `CustomFields`
  (`Dictionary<string, object>`)
- `InAppMissionsMilestonesProgressBar`: `TotalScore` (`decimal`), `Milestones`
  (`List<InAppMissionsMilestoneStep>`), `CustomFields`
- `InAppMissionsMilestoneStep`: `RowNumber` (`int`), `MilestoneNumber` (`int`), `Score`
  (`decimal` — the threshold), `Status` (`InAppMilestoneStatus`: `Unknown` / `Closed` /
  `Reached` / `Collected`), `Button` (`InAppCustomButton` — carries the reward)
- Result hierarchy: `InAppMissionsCollectResult` (`SetNumber`, `RowNumber`) is the base;
  `InAppMissionsProgressResult` adds `Status` (`InAppMissionsProgressStatus`),
  `InAppMissionsMilestonesResult` adds `Status` (`InAppMilestoneStatus`).

All missions types are server-driven with `private set` — never construct or mutate
them client-side.

## Overview

A missions in-app is an inbox in-app whose custom template carries a missions feature.
Its state is a **set** of mission rows plus a shared **progress bar** of milestones:

- Rows are grouped by `PlacementNumber` (the UI's mission slots) and ordered inside a
  placement by `SubMissionNumber` (the chain within a slot).
- Rows with `PlacementNumber`/`SubMissionNumber` semantics of "common" (no
  sub-mission number) form a shared track, tracked by
  `FinishedCommonSubmissionsRows`.
- Each row contributes its `ProgressBarScore` to `ProgressBarState.TotalScore` when it
  is **collected** (not when it reaches its goal), which drives the milestone steps.
- Sets are time-boxed (`StartTimestamp` / `EndTimestamp`); a finished set may still be
  visible via `PreviousSetProgress` while its rewards are collectable.

### Delivery — two channels, same in-app

- **WebSocket** — `InAppMessage.Command is InAppMissionsProgressChangedCommand`.
  Handled by `ProcessMissionsProgressInApps` in the sample.
- **Sync API** — `response.Data.InboxDetails.MissionProgressionInApps`, a
  `List<string>` of UUIDs. Resolve the actual message by matching those UUIDs against
  `response.Data.InboxMessages`. See [`05-events-sync.md`](05-events-sync.md).

**The in-app UUID is stable across trigger → progression.** The missions in-app you
received on the trigger event and the one you receive on a progression event are the
same instance: `progressedInApp.Uuid == originalInApp.Uuid`. Update in place; never
create a second UI object.

**`InAppMissionsProgressChangedCommand` has no `DisplayOnProgressChange` flag** —
unlike `InAppScoreChangedCommand` and `InAppMilestonesProgressChangedCommand`. Whether
a missions progression re-opens the in-app is entirely the game's decision. The sample
refreshes silently (`addToDisplayQueue: false`).

## Progression Semantics
- **Active row per placement** — the row with the lowest `SubMissionNumber` that is not
  yet `Collected`. Sort by `SubMissionNumber`; do not assume `ActiveMissions` order.
- **The next sub-mission appears on *Reached*, not on *Collected*.** As soon as row N
  hits its goal, row N+1 is added to `ActiveMissions` with `Status = Active`,
  `CurrentScore = 0`, and its own `GoalScore` / `ProgressBarScore`. Both rows coexist
  until N is collected — so a placement can legitimately show one `Reached` row awaiting
  collection and one `Active` row already accruing. Render both.
- **Common missions unlock after a placement's chain completes.** Once every
  sub-mission of a placement has been reached, the common rows enter `ActiveMissions`;
  `FinishedCommonSubmissionsRows` records which common rows are done.
- **Progress bar accrual happens on collect, not on reach.** `ProgressBarState.TotalScore`
  grows by each **collected** row's `ProgressBarScore` — a row sitting at `Reached`
  awaiting collection contributes nothing yet. Milestone steps whose `Score` threshold is crossed flip from
  `Closed` to `Reached` and become collectable via
  `CollectMissionsMilestonesProgress`. Progress is **not reset** on reaching a
  milestone — the same accumulate-don't-reset rule as template milestones (module 06).
- **Score amounts come from the Dashboard row, not the client.** A row may add a fixed
  `+1` per progression event, or bind its increment to an event parameter
  (`bind_with_event_parameter`) so the parameter's value is the amount added — and may
  gate on a condition over that parameter. If the event does not carry the bound
  parameter, the row does not advance. This is why the progression event's parameter
  set is a Dashboard prerequisite (see §Dashboard).
- **`decimal`, not `int`.** `CurrentScore`, `GoalScore`, `ProgressBarScore`, and
  milestone `Score` are all `decimal`. Comparisons and UI formatting must not truncate.

## CTA routing

Mission CTAs are **not template buttons.** `InAppMission.ProgressionCta` and
`InAppMission.CompletionCta` — and `InAppMissionsMilestoneStep.Button` — are per-row
values, not entries in `InAppCustomTemplateData.Buttons`. The sample's
`KinoaUiService.HandleInAppButtonClickAsync(message, buttonKey)` resolves clicks by
looking up `buttonKey` in the template's buttons map, so it **cannot** route a mission
row's CTA.

Route mission CTAs by handing the `InAppClickConfiguration` itself to the click
handler, pattern-matching the derived type exactly as the sample's `LogClickConfig`
does — `InAppCloseClickConfiguration`, `InAppBillingClickConfiguration`,
`InAppCollectResourceClickConfiguration`, `InAppCustomClickConfiguration` (read
`CtaName` and route to the game's own handler), and the rest. Milestone step rewards
come from `InAppMissionsMilestoneStep.Button` in the same way template milestone
rewards do (`Button.Resources`, `Button.ClickConfig`).

## Best Practices
- Read the feature once per update: `(message.Data as InAppCustomTemplateData)?.Feature as InAppMissionsFeature`, and null-guard it — a non-missions in-app returns `null`
- Sort `ActiveMissions` by `SubMissionNumber` before rendering; group by `PlacementNumber`
- Update the existing UI object in place on progression (stable UUID); do not spawn a second one
- Route the `InAppMessage` returned by `CollectMissionsProgressAsync` / `ReachMissionsProgress` straight into the UI refresh path — it is fresh state, and no WebSocket push follows
- Expect the progress bar to move on `CollectMissionsProgressAsync`, not on progression or reach — read `ProgressBarState.TotalScore` off the in-app the collect call returns
- After `CollectMissionsMilestonesProgress`, apply the returned per-row `Status` values to the local model — no refreshed in-app arrives
- After `DeleteMissionsProgress`, drop the rows locally; the next progression event brings the authoritative state
- Batch `rowNumbers`: one call per set covering every eligible row beats one call per row
- Acknowledge view after a missions refresh — the server resets `IsViewed = false` on every new `Command`, missions included (module 06)
- Check the set window (`StartTimestamp` / `EndTimestamp`, milliseconds) and the in-app's `CountdownTimer.EndTimestampWithExtraLifeTime` before offering collection on a finished set
- Send the standard in-app analytics events (`SendInAppImpressionEvent`, `SendInAppCloseEvent`, `SendInAppClickEvent`) for the missions in-app like any other

## Configuration Notes (what's NOT in the sample)
- **Row banding by player state.** Missions rows can be filtered by player-state columns (level bands, custom fields) so different players see different goals and rewards for the same in-app. Entirely server-side; the client reads whichever band arrived.
- **Parameter-bound increments.** A row can gate on a condition over an event parameter and take its increment from that parameter's value rather than a flat `+1`. Configured per row on the Dashboard.
- **Set rollover.** When a set finishes, `PreviousSetProgress` keeps the finished set (for collection during the extra-life window) while `ActiveSetProgress` carries the new one. `CurrentSetNumber` follows the active set.
- **Store packages per row.** `GooglePackageId` / `ApplePackageId` on a mission row support billing CTAs — the same pattern as template milestone `InAppStorePackages`.
- **Custom fields at three levels.** `InAppMission.CustomFields`, `InAppMissionSetProgress.CustomFields`, and `InAppMissionsMilestonesProgressBar.CustomFields` are `Dictionary<string, object>` — operator-defined extras (image values deserialize as `InAppCustomFieldOfTypeImage`, same as elsewhere).

## Important Notes
- **A missions in-app is triggered like any regular in-app** (WebSocket or Sync API). The only difference is the missions `InAppFeature` on the custom template data — same as milestones.
- **Missions are inbox in-apps.** They carry a countdown timer and persist across sessions. Non-inbox missions make no sense — there would be nothing to progress.
- **Post-expiration behaviour follows the in-app rules in module 06**: after the countdown expires, the in-app stays in the inbox for the Dashboard-configured extra days ("Allow Extra Time to Collect Reward"); collection stays enabled, progression halts, reminders and replacements stop.
- **Missions are not supported by the offline in-apps pipeline** — `com.kinoa.sdk.offline` does not match missions configurations. Missions require the online path.
- **UI is entirely client-side.** The SDK ships state; layout, tabs per set, row animation, and reward presentation are the game's. `KinoaUiService` is a stub.

## Common Mistakes
- Casting `custom.Feature` to `InAppMilestonesFeature` on a missions in-app — the cast yields `null` and the missions state silently disappears. Match the concrete type: `InAppMissionsFeature` vs `InAppMilestonesFeature`
- Calling `Kinoa.Messaging.CollectMilestonesAsync` for missions milestones — that collects *template* milestones. Missions milestones use `CollectMissionsMilestonesProgress` (different endpoint, different result type)
- Reading `DisplayOnProgressChange` off `InAppMissionsProgressChangedCommand` — the type has no such property; it is not a `InAppMilestonesProgressChangedCommand`
- Awaiting a WebSocket push after `CollectMissionsProgressAsync`, `ReachMissionsProgress` or `DeleteMissionsProgress` — the first two already return the fresh in-app, and `DeleteMissionsProgress` neither returns nor triggers one
- Discarding `CollectMissionsProgressAsync`'s or `ReachMissionsProgress`'s `response.Data` — that `InAppMessage` is the updated in-app; throwing it away forces an unnecessary round trip
- Still calling the deprecated `CollectMissionsProgress` — it hits the pre-v2 endpoint and returns only per-row statuses, so the refreshed progress bar has to be fetched separately
- Expecting `ProgressBarState.TotalScore` to advance on a progression event or on `ReachMissionsProgress` — it accrues when rows are collected
- Passing `PlacementNumber` or `SubMissionNumber` where `rowNumbers` is expected — the endpoints key on `RowNumber`
- Assuming the next sub-mission appears only after collection — it appears on *Reached*, so a placement can hold a `Reached` and an `Active` row simultaneously
- Rendering `ActiveMissions` in arrival order — order is not guaranteed; sort by `SubMissionNumber` within `PlacementNumber`
- Ignoring `FinishedCommonSubmissionsRows` — the common track's completion state lives there, not in the row statuses alone
- Treating `CurrentScore` / `GoalScore` / `ProgressBarScore` / milestone `Score` as `int` — all four are `decimal`
- Reading `PreviousSetProgress` as the current set — the active set is `ActiveSetProgress` (and `CurrentSetNumber`)
- Treating `StartTimestamp` / `EndTimestamp` as seconds — they are **milliseconds**
- Routing `ProgressionCta` / `CompletionCta` through the template button click router — they are per-row `InAppClickConfiguration`s with no `buttonKey`. See §"CTA routing"
- Creating a second UI object when a progression arrives — the UUID is stable; update in place
- Expecting missions to work through `com.kinoa.sdk.offline` — the offline pipeline does not support them
