# Async (Fire-and-Forget) Game Events

## Sample File(s)
- `Services/KinoaGameEventsService.cs`
- `Services/KinoaGameEventBuildingService.cs`

## Integration Notes
- **Default:** generate all predefined events + `SendCustomEvent`. `payment` is mandatory.
- **Do NOT ask** — include all by default (too many events to fit `AskUserQuestion`'s 4-option limit). Developer can delete unused methods afterwards.
- `session_start` belongs to Sync API (see 05-events-sync) — exclude from Async unless explicitly requested.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces in the generated Kinoa base across `KinoaGameEventsService.cs` and `KinoaGameEventBuildingService.cs`. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa async-events --merge`.

### Editable surfaces

#### `KinoaGameEventBuildingService` — adding new event-construction methods

Adding new event-construction methods (for custom events the developer introduces, or game-specific predefined-event helpers), AND modifying existing event-construction methods to set default field values, add optional `SetXxx(value)` chains, or enrich data.

**Guidance:** scan the client codebase for existing analytics calls in TWO categories:
1. **String-keyed SDK calls** (`Analytics.Track*`, `FirebaseAnalytics.LogEvent`, `AppsFlyer.Track`, `Amplitude.logEvent`, `GameAnalytics`, `Adjust.LogEvent`, etc.) → mirror into **custom** event-builder methods here in `KinoaGameEventBuildingService`.
2. **Typed dispatcher methods on the game's own analytics layer** (`AnalyticsManager.OnLevelUp(...)`, `OnRealPayment(...)`, `OnTutorialFinish/Skipped(...)`, `OnInAppPurchase(...)`, `OnWatchAd(...)`, `OnSessionStart(...)`, `OnCollectedResource(...)`, etc.) → **prefer the matching Kinoa predefined event** (`SendLevelUpEvent`, `SendPaymentEvent`, `SendTutorialEvent`, `SendInGamePurchaseEvent`, `SendWatchAdEvent`, `SendSessionStartEvent`, `SendCollectedResourceEvent` — see sample-shipped methods on `KinoaGameEventsService`) over a custom-event mirror. Predefined events ship with pre-registered Dashboard schemas (no per-game registration needed) and use SDK-shipped setters for canonical fields. Reserve custom-event mirrors for genuinely game-specific signals (e.g., UA attribution, game-specific progression markers) where no Kinoa predefined event matches semantically.

**Coverage gate — ask the developer how many events to mirror BEFORE generating any builders.** Before adding any event-builder methods, enumerate the analytics call sites you found in a tabular form with reachability data, and open a gate with the developer:

*"Pick which events to mirror — (a) all / (b) subset (by `#`, e.g., '1, 3, 5-8') / (c) none. I found N distinct events:*

```
# | event              | source                          | params              | mirror type          | callers
1 | OnRealPayment      | AnalyticsManager.cs:OnRealPay   | productId, price... | predefined (Payment) | (dispatcher)
2 | OnLevelUp          | AnalyticsManager.cs:OnLevelUp   | level               | predefined (LevelUp) | (dispatcher)
3 | purchase_succeed   | PackPurchaseAnalytics.cs:31     | product_id, price   | custom               | 0 (override)
4 | game_started       | CarGameAnalytics.cs:24          | level, mode         | custom               | 12
5 | level_failed       | CarGameAnalytics.cs:48          | level, attempt      | custom               | 4
6 | orphan_event       | OrphanAnalytics.cs:15           | (none)              | custom               | 0
```

*Behavior: (a) generates builders + Send pairs for every row, register all as Dashboard prerequisites; (b) recommended when `callers: 0` rows with no annotation suggest orphan code; (c) skips this carve-out entirely."*

**Notes (skill internals — not shown in gate prompt):** `mirror type` column distinguishes:
- `predefined (<Kinoa-event-name>)` — typed-dispatcher game-action site with semantic Kinoa-predefined match (pre-registered Dashboard schemas, preferred over custom mirrors when semantics match). Auto-classifies into State 1/2/3 per §"Three states" below — no separate gate.
- `custom` — string-keyed analytics call requiring a new builder + Send pair on the Kinoa side.

**`callers` column — informational reachability signal.** Best-effort detection of dispatch infrastructure:

- **Number** = direct callers found via Grep on the enclosing method name OUTSIDE its own class (e.g., `Grep "OnLevelStart\\(" --excluding analytics-class-file`).
- **Annotation in parentheses** when callers=0 but method is reachable via non-direct dispatch:
  - `(override)` — method declaration uses `override` keyword; reachable via base/virtual dispatch (e.g., `Completed` overriding `PackPurchaseAnalytics<T>.Completed`, `OnPurchaseCompleted` overriding IAP listener, `Update` overriding `MonoBehaviour`).
  - `(interface)` — class implements an interface and method matches an interface contract (Grep `class X : I*` then verify method signature matches).
  - `(signal handler)` — class registered with signal bus / event aggregator (Grep `Subscribe<>`, `signalBus.Subscribe`, `EventBus.Listen`, `+= OnXxx`).
  - `(lifecycle)` — method name matches Unity engine lifecycle hook (`Awake`, `Start`, `Update`, `OnEnable`, `OnDestroy`, `OnApplicationPause`, etc.).
  - `(DI bound)` — class bound in DI container (Grep `Container.Bind<>`, `Container.Register<>`, `[Inject]`).
- **No annotation when callers=0** = potential orphan; developer's call to mirror or skip.

This signal is **informational — does NOT auto-withdraw**. Developer's coverage-gate choice is the only filter. Detection is best-effort; if a method is reachable via a dispatch mechanism the skill doesn't recognize, annotation is missing but column still shows `callers: 0` — developer investigates if uncertain.

Without this gate, merge behavior is non-deterministic — one run mirrors 3 of 20 (silent under-coverage, Dashboard audiences on unmirrored events never fire), another run mirrors 15 of 15 (potential over-generation for events the developer never intended to track in Kinoa). The developer's choice must be explicit.

**Discovery iteration depth — exhaust the analytics surface before opening the coverage gate.** Discovery scan continues iterating Grep passes until **no new event names** surface across the project, NOT until the first batch returns. Single Grep on `Analytics\.|TrackEvent|LogEvent` is the **starting probe**, not the only one — many event names live inside dispatcher method bodies or constants files, not at call-site args. Per-pass:

1. **Pass 1 (broad call-site Grep):** initial Grep on the analytics keyword cheatsheet (SKILL.md §"Workflow → Discover candidates"). Collect distinct event-name strings appearing as `Track*(...)` / `LogEvent(...)` / `Analytics.*(...)` call args.
2. **Pass 2 (dispatcher walk):** for each typed analytics-dispatcher class found in Pass 1 (`*AnalyticsManager`, `*AnalyticController`, `*Analytics*Helper`, `*GameAnalytics`), Read the **full class body** (not just method signatures). Grep for `EventBuilder.Init(...)` / `LogEvent(...)` / `Track*(...)` / `Send*(...)` literal call chains inside switch-case statements and typed dispatcher methods — many event names live inside method bodies rather than being passed as call-site args.
3. **Pass 3 (constants files):** Grep for `*EventName*.cs`, `*EventNames.cs`, `*AnalyticsConstants*.cs`, `*AnalyticsEventKeys*.cs`, `Constants/*Events*.cs`. Collect any `public const string ... = "<event_name>"` declarations and add to the candidate list.
4. **Stop condition:** when the next Grep pass adds **zero new distinct event-name strings**. Only THEN compose the coverage-gate prompt.

Coverage-gate `N` count is the union across all passes. Surface in the gate prompt: *"Found N distinct event names across <M> files, <K> dispatcher classes, <L> constants files."* **Do not pose the gate after Pass 1 alone** — that's stopping at first dispatcher hop, which under-covers by 5×–80× depending on project structure.

**Coverage-gate counting — "subset N events" means N distinct event names.** When the developer picks `(b) subset` and names a count, count by distinct event-name strings, NOT by method signatures. A single event name (e.g., `payment`) wired across multiple game-action sites with one builder + one Send pair counts as ONE toward N. Legacy-absorbing overloads added per the two-handed rule (SKILL.md §"Pre-existing compile blockers" → Method-call signature mismatches) DO NOT contribute to N — they're a compile-blocker resolution, not a new event-coverage contribution. Predefined-event wiring picked under §"Predefined-event coverage gate" below counts toward N alongside custom-event mirrors. Without this clarification, "subset 5-7" gets interpreted ambiguously — one read delivers 1 event with 5 absorbing overloads, another delivers 7 distinct events with no overloads.

**No auto-withdrawal — coverage gate is authoritative.** Skill does NOT auto-withdraw candidates based on reachability heuristics. All discovered events surface at the coverage gate above with the `callers` column showing reachability data; developer's choice at the gate is the only filter. The previous auto-withdrawal rule was over-eager — it silently dropped candidates reachable through virtual override / interface contract / signal handler / Unity lifecycle / DI dispatch (none of which Grep on event-name string finds), losing the highest-value Kinoa wiring sites (templated analytics wrappers, framework-dispatched purchase listeners). Replacing it with informed-developer choice eliminates the false-negative class entirely; if a chosen event turns out to be unreachable orphan code, it surfaces in `git diff` as easily-removable dead code (one builder + one Send wrapper + one constants entry per event).

**Atomic per-event delivery — honor the coverage gate choice in full.** The developer's coverage-gate answer (the specific N events picked under `(a)` or `(b)`) is the authoritative scope. For every picked event, complete the entire mirror chain — Constants entries → builder method → Send wrapper → parallel-call wiring at every reachable game-action site — before moving to the next event. **No silent partial delivery.** If a step cannot complete for some event (compile dependency unresolvable, source dispatcher unreadable, parameter shape conflicts, etc.) → STOP, surface a per-event Modify/Skip gate to the developer for THAT specific event, do NOT proceed to the next leaving partial Constants entries leaked and the builder unimplemented. Silent truncation breaks the implicit contract from the coverage gate: developer picked N events expecting N mirrors; receiving K of N with the remaining K+1..N as orphan partial work is worse than picking K explicitly upfront. The standard per-edit Apply confirmation still fires per chain step — atomic delivery means all-or-explicit-stop, not "skip confirmation gates".

**Context-budget exhaustion is NOT a silent-truncation excuse.** Framings used to defer the developer's `(a) all` picks WITHOUT an explicit gate — *"focused scope"*, *"tiered coverage"*, *"remaining context budget"*, *"atomic delivery limit"*, *"will surface remainder as Discovered but not mirrored"*, *"emitter files not read this session"*, *"deferring sites whose files weren't read"*, *"wire candidates surfaced for follow-up"* — are forbidden. They're rule-bending workarounds for the all-or-explicit-stop contract above. STOP reasons are LIMITED to: (1) compile dependency unresolvable on a referenced symbol, (2) source dispatcher unreadable for value-literal verification, (3) parameter shape conflict between SDK signature and call-site variables. Context budget is not on that list. If the session genuinely cannot complete the picked scope mid-flight, STOP at the point of exhaustion and surface a **session-truncation gate**: *"Mirror chain delivered for K of N events; remaining N−K events: \<enumerate event names\>. (a) Defer remaining to follow-up `/kinoa async-events --merge`; (b) Pick specific remaining events to wire now; (c) Cancel session — coverage scope was too broad, re-run with narrower pick."* Developer's gate answer reconciles the partial state; silent surfacing as informational `Discovered but not mirrored` table is NOT equivalent to that gate — `Discovered but not mirrored` is reserved for events the developer **opted out of** at the coverage gate, NOT for events the skill silently dropped after `(a) all`.

**Game-action site emitter Read is mandatory — "not read this session" is NOT a STOP reason.** For every picked event, the chain MUST land at every reachable game-action site (per the Paired call-site Apply gate below) — that requires Reading the emitter file (e.g., `EndGameController.cs` for `OnSolitaireLevelUp`, `Intro.cs` for `OnTutorialFinish/Skipped`, `*GameplayManager.cs` for level-end emits, IAP-handler for `OnIAPPurchased`) to verify call-site context + locate the insertion line. **Read is part of the workflow, not optional.** Framings used to defer wiring — *"emitter files not read in this session"*, *"deferring sites whose files weren't read"*, *"wire candidates surfaced for follow-up"* — are forbidden when the events those emitters fire were picked at the coverage gate. Legitimate Read-failure exceptions are LIMITED to: file absent from disk, file locked by another process, permission denial — each of which surfaces in the per-event Modify/Skip gate per §"Atomic per-event delivery". *"Skill chose not to Read"* is not a Read-failure exception — it's silent partial delivery in disguise.

For events the developer does NOT pick at the gate, surface them in the closing summary's `Discovered but not mirrored` table per SKILL.md §"Closing summary" rule — this preserves audit trail of unmirrored signals so the developer can re-run `/kinoa async-events --merge` later with a different subset choice.

**Pre-mirror source dispatcher Read + value-literal match check — every event before generating its builder.** When mirroring an event into a builder, do NOT rely on the call site's signature or surrounding context alone. **Read the source dispatcher method's full body** (the method on the game's analytics layer that the call site invokes — e.g., `AnalyticsManager.OnLevelUp`, a `*GameAnalytics.<EventName>` method) to discover the literal string values passed to the underlying `Track*(name, params)` / `LogEvent(name, params)` / `EventBuilder.Init(name).<Setter>(...).Send()` chain. Mirror those literals byte-for-byte:
- If the source dispatcher's method body has `EventBuilder.Init("game_finished").ST3("terminate")`, the Kinoa builder must pass `"terminate"` for the matching field — NOT a synthesized `"quit"` inferred from the source method name `GameQuit`.
- If the source dispatcher passes `Parameter("seed", currentSeed)` AND `Parameter("moves_made", movesCount)` AND `Parameter("score", finalScore)` (3 of 11 source params materially set), and the surrounding call site doesn't expose all 11, the mirror's builder + Send pair must include those 3 wired params at minimum — declaring constants for unused params is dead code in the constants file (drop them).

Read the dispatcher body via `Read` (not Grep) so the full literal chain is visible; do not infer values from method names alone. This is a verify-before-assert obligation specific to event-mirror generation. Cite the source file:line in the rationale comment of the new builder method (`// Mirrors AnalyticsManager.GameQuit at AnalyticsManager.cs:1442 — preserves "terminate" literal from source ST3 call`).

**Predefined-event matches are part of the combined coverage gate above** — when the discovery scan finds typed dispatcher methods on the game's analytics layer (`AnalyticsManager.OnLevelUp`, `OnRealPayment`, `OnIAPPurchased`, `OnTutorialFinish`, `OnCollectedResource`, `OnSessionStart`, etc.) that semantically match a Kinoa predefined event, they appear in the same table rows with `mirror type = predefined (<Kinoa-event-name>)`. No separate coverage gate; the developer's single (a)/(b)/(c) pick covers both custom mirrors and predefined matches.

**After the batch (a)/(b)/(c) choice, no per-event permission gate.** For each picked predefined match, automatically classify into one of three states based on the integrated Kinoa base + the game's existing taxonomy, and wire accordingly. The standard per-edit Apply / Skip / Modify confirmation still applies to each resulting `Edit` (game-side parallel-call line, Kinoa-side overload, or restored builder), but no extra "which params?" / "which state?" / "extend or wrap?" gate is opened — the analysis is automatic, only the edits themselves are confirmed.

**Three states of the Kinoa-side builder + Send pair, classified automatically:**

1. **Builder + Send pair exists in the integrated Kinoa base** AND the current SDK signature already accepts the game's taxonomy fields (resolve via `KinoaCore.xml` setter probe per §"Base-class setter reuse" above) → wiring is purely a game-side Edit. Apply gate at the game-action site inserts the parallel-call line per the placement rule below. Wire **all** matched parameters available at the call site (locally-visible variables that map to SDK setters / `AddCustomParameter` keys) — minimum richness is automatic, not gated.

2. **Builder + Send pair exists, but the game's taxonomy includes fields not in the current SDK signature** → automatically extend the Kinoa side using alongside-permission (no in-place edit on existing methods): add a new overload OR a wrapping helper method that calls the existing predefined builder and chains `AddCustomParameter("snake_case_key", value)` for each unmatched field. Use base-class setters first when SDK fields exist; fall back to `AddCustomParameter` only for fields with no base-class equivalent. The Kinoa-side addition is its own Apply gate alongside the game-side parallel-call Apply gate.

3. **Builder + Send pair does NOT exist** (developer opted out at `--auto` Phase 2, OR deleted the predefined method pre-merge) → automatically restore the missing builder + Send pair using the sample as the base, extending so the game's taxonomy fits (base-class setters for matched fields; `AddCustomParameter` for fields without a base-class equivalent). The Kinoa-side restoration is its own Apply gate alongside the game-side parallel-call Apply gate. The new methods follow the same constants-consolidation rule below.

In all three states, the predefined-event mirror counts toward the coverage-gate `subset N` count alongside custom mirrors. Without this gate, the integration defaults to mirroring everything as custom events even when 1:1 predefined matches exist — losing the pre-registered Dashboard schemas — and silently misses events the developer opted out of pre-merge that semantically need wiring back.

**State 1 vs State 2 — call-site variable scan (mandatory before classification).** Before classifying a match as State 2, Grep the **5-10 lines around each game-action site** for variables matching canonical SDK params. The typed-dispatcher's args alone don't define what's available — surrounding scope does (e.g., `OnIAPPurchased(iapName, purchasePlace, couponId)` may not pass `price` / `currency`, but `product.metadata.localizedPrice` / `product.metadata.isoCurrencyCode` are usually available 5-10 lines above). Recognise naming variations by type + semantic role:
- SDK `(string productId, decimal price, string currency, ...)` ↔ call-site `productId` / `product.metadata.localizedPrice` / `purchasePrice`, `isoCurrencyCode` / `currencyCode` / `currencyStr`
- SDK `(int level, string place, ...)` ↔ call-site `level` / `currentLevel` + `place` / `screen` / `source`
- SDK `Dictionary<string, decimal>` ↔ call-site resource dicts or per-resource fields aggregatable into one

**Per-param matching outcome:**
- **Match** — canonical param has a same-scope variable.
- **Extra** — call-site has a field not in the canonical SDK signature.
- **Missing** — canonical param has no match in surrounding scope.

**Classification — automatic, no extra gate.** State 1 / State 2 outcome is determined by the per-param scan above; no new "which state?" / "which params?" question is opened on top of the standard per-edit Apply/Skip/Modify confirmation that already fires for every Edit:

- **State 1 — All Match, no Extra.** Wire the game-side parallel-call directly with canonical SDK signature: `KinoaGameEventsService.Instance.SendPaymentEvent(productId, product.metadata.localizedPrice, product.metadata.isoCurrencyCode);`. **No Kinoa-side edit.**
- **State 2 — All Match + ≥1 Extra.** Add an overload method on the Kinoa side (alongside-permission, not in-place edit) with extended signature `(canonical params..., extra params...)`. Body chains `AddCustomParameter("snake_case_key", extraField)` for each Extra **and then** calls the canonical SDK predefined event. Game-side parallel-call uses the new overload. **Redundant-encoding check:** before adding `AddCustomParameter("<key>", ...)` for an Extra, verify the same `<key>` is not already populated via a canonical SDK setter on the event-data class OR via an upstream dictionary parameter (e.g., `spent` / `received` dicts on `InGamePurchaseEventData`). If the same logical value would be written twice on the event, drop the `AddCustomParameter` call — duplicate-encoding ships the value twice (once via canonical setter / dict entry, once via custom param), polluting Dashboard data shape.
- **≥1 Missing — proceed as State 2 with explicit default for the Missing param + surface in `Discovered but not mirrored → Predefined param defaulted for <event_name>: <param> defaulted to <value> at <file:line> (real value not available in surrounding scope).`** The Apply / Modify (real value) / Skip decision rides on the **standard per-edit Apply/Skip/Modify gate** — no separate Missing-param gate is created. The Edit proposal text surfaces the defaulted param so the developer reacts informed at the standard gate.

**Anti-pattern (forbidden):** generating an overload that takes ONLY typed-dispatcher params and silently drops or hardcode-defaults canonical SDK params. Silent data loss — Dashboard reports wrong revenue / wrong currency / wrong placement.

**Canonical params MUST be mandatory in the overload signature (no default values).** Optional canonical params with sample defaults (`decimal spent = 0.99m, string isoCurrencyCode = "USD"`) let call sites silently skip passing real values — the overload compiles, the call site looks complete in a diff, and the event ships hardcoded defaults to Dashboard. Mandatory canonical params force the wiring step to scan surrounding scope at each call site for the actual variable (e.g., `product.metadata.localizedPrice`, `product.metadata.isoCurrencyCode`) — the compiler error on a missing arg is the safety net. Extras chained via `AddCustomParameter` MAY be optional with defaults; canonical SDK setters / ctor params on the predefined event MUST NOT be.

If the canonical-param value is genuinely unavailable at every call site (scope-local scan turns up nothing), fall through to the State-2 Missing-param path: explicit default at the call site (not in the signature) + entry in `Discovered but not mirrored → Predefined param defaulted for <event_name>: <param> defaulted to <value> at <file:line>`. The two-handed-rule (SKILL.md §"Pre-existing compile blockers" → signature-mismatch absorbing overloads) authorizes typed-dispatcher-only overloads only for legacy compile-error resolution, not as a substitute for State-1 / State-2 wiring.

**Event-name + parameter-name taxonomy — reuse verbatim, do not synthesize, do not reformat.** When mirroring an existing analytics event, READ the source analytics method's **parameter dictionary and any dedicated constants / enum class** (`*EventName*.cs`, `*EventParams*.cs`, `*AnalyticsConstants*.cs`, `*AnalyticsEventKeys*.cs`) and copy the **exact event-name and parameter-key strings byte-for-byte from the source call site** — preserve original casing, do not invent parallel names, do not heuristic-recase. If the game sends event `"skip_coloring"` with parameter `"Level"`, your Kinoa custom-event mirror also sends `"skip_coloring"` with parameter `"Level"` — NOT `"Coloring_skipped"`, NOT `"SkipColoring"`, NOT recased `"level"`. Skill must NEVER apply snake-case-to-PascalCase or PascalCase-to-snake-case transformations on discovered strings. The game's Dashboard audiences, trigger conditions, and segments are keyed on the existing names with their existing casing; synthesizing parallel names or reformatting case silently fails those integrations in prod. This is the mechanical contract of the parallelism principle: Kinoa sees the taxonomy the game already sees, character-for-character.

**Event-name + parameter-key constants consolidation:** when adding any custom string-key parameter (≥1) to ANY event — custom-event mirror OR custom param on a predefined event (via `customParams` dict / `AddCustomParameter`) — centralize the event-name + parameter-key string literals in a constants class. **The sample-shipped `Assets/Scripts/Kinoa/Constants/KinoaGameEventConstants.cs`** (imported from `com.kinoa.sdk.core` UPM sample `Kinoa Constants` — mandatory baseline) ships as a TODO-scaffolded `public static class` in `namespace Core.Constants`. Extend it with `public const string` entries for both event names (custom events only — predefined event names live in SDK) and parameter keys (both custom-event params AND custom params added to predefined events). Naming pattern: `EventName_<UpperCamelCase>` for event names, `ParamKey_<UpperCamelCase>` for parameter keys (e.g., `EventName_OnPurchaseSucceed = "on_purchase_succeed";`, `ParamKey_PurchasePrice = "purchase_price";`, `ParamKey_JourneyLevel = "journey_level";`). Single source of truth, typos caught at compile time, rename in one place. **If the game also has an existing analytics-constants file**, prefer extending `KinoaGameEventConstants` AND/OR referencing the game's class directly from the Kinoa call site when the namespace allows it — avoid duplicating literals across two constants files. **Mandatory self-verification after mirror generation:** Grep `Assets/Scripts/Kinoa/Constants/KinoaGameEventConstants.cs` and confirm every custom-event name + every custom param-key string used in this run's edits has a matching `public const string` entry. Zero or mismatched count signals the constants step was skipped — go back and extend the constants file before closing the run. This rule fires whenever ≥1 custom-event mirror OR ≥1 custom-param-on-predefined-event was added — including in-place repair of legacy call sites that introduce `customParams` dict keys (e.g., rewriting `KinoaIntegration.SendLevelUpEvent(level)` → `SendLevelUpEvent(customParams: new() { ["journey_level"] = level })` requires `ParamKey_JourneyLevel` in the constants file too).

**Dead-constants safety net (mandatory before closing summary).** Immune system for partial-work leak that slipped past the atomic-per-event rule above. If ≥1 declared `EventName_*` / `ParamKey_*` constant in `Assets/Scripts/Kinoa/Constants/KinoaGameEventConstants.cs` has only its declaration as Grep reference (no usage anywhere in `Assets/Scripts/`), surface ONE closing-summary line under `Unresolved`: *"Review `Assets/Scripts/Kinoa/Constants/KinoaGameEventConstants.cs` — some declared constants have no usage; wire a builder/dispatcher OR remove the declaration manually. IDE 'Find Usages' will identify them."* No table, no per-constant enumeration — developer reviews via IDE usage info.

**Atomic-delivery final-verify Grep (mandatory before closing summary).** For every event picked at the coverage gate, verify the Kinoa-side chain artifacts landed:

```
# For each picked custom event <EventName> (e.g., "PurchaseSucceed", "EpisodeReached", "LevelAttempt"):
grep -n "EventName_<EventName>" Assets/Scripts/Kinoa/Constants/KinoaGameEventConstants.cs
grep -n "On<EventName>Event\|On<EventName>(" Assets/Scripts/Kinoa/Services/KinoaGameEventBuildingService.cs
grep -n "Send<EventName>Event" Assets/Scripts/Kinoa/Services/KinoaGameEventsService.cs
```

If any of the 3 Kinoa-side artifacts returns 0 hits → **auto-complete the missing artifact in the same run before closing summary**. Developer's coverage-gate pick is authoritative; don't open another Modify gate to ask permission for subsequent chain steps — just generate the missing Constants entry / builder method / Send wrapper using the canonical template for that event class. Per-edit Apply confirmation still fires for the actual `Edit` that lands the artifact (gate is consent layer, not permission to re-ask scope).

**Parallel-call wiring at game-action sites is informational, not strict.** Count and surface in closing summary as *"Parallel-call sites for <EventName>: N wired"*. Zero is acceptable — developer may have picked an event knowing wiring is post-merge manual work, OR the event fires from a dispatcher pattern the skill couldn't auto-detect. Do NOT roll back Constants/builder/Send when 0 parallel-call sites — the developer's pick stands; absent callers don't invalidate the Kinoa-side chain.

Predefined events (Payment, LevelUp, Tutorial, etc.) are exempt from the Constants check (event names live in SDK, not in `KinoaGameEventConstants.cs`) but still require builder + Send presence; auto-complete same as custom events.

**Base-class setter reuse — discover available setters before falling back to `AddCustomParameter`.** When building a predefined event (any subclass of `GameEventData` other than `CustomEventData`), the SDK exposes setter methods on the event-data class hierarchy that auto-populate fields into the predefined event's pre-registered Dashboard schema. **Discover the setters available in the SDK version actually shipped with the project** before mirroring analytics fields — do not hard-code a setter list, the hierarchy may evolve between SDK versions. Procedure:

1. **Walk the sample's existing builder methods** for the same predefined event in `Services/KinoaGameEventBuildingService.cs` (sample-shipped) — these reveal which setters are in use for the SDK version being integrated against.
2. **Resolve the SDK XML docs** the same way `modules/02-player.md` §"Merge Surfaces" resolves `PlayerState` introspection — sibling `KinoaCore.xml` next to `KinoaCore.dll` in the package's `Runtime/` folder (registry-install path `Library/PackageCache/com.kinoa.sdk.core@*/Runtime/KinoaCore.xml` first, embedded fallback `Packages/com.kinoa.sdk.core/Runtime/KinoaCore.xml`). Then Grep for setter members on the target event-data class AND every base class up the chain to `GameEventData`:
   ```
   grep -E 'member name="M:Kinoa\.Data\.Events\.<EventClass>\.Set[^"]+"' <resolved-xml-path>
   ```
3. **Map analytics payload fields → setters at the call site:** for each field in the game's existing analytics call, check whether a matching setter exists on the event-data class (current or inherited). If yes, use it. If not, fall back to `AddCustomParameter("snake_case_key", value)`.

Duplicating an SDK-provided field via `AddCustomParameter("level", ...)` etc. on the same predefined event ships TWO copies of the same value (one in the base field, one in custom params) and dilutes Dashboard data shape — always reuse a setter when one exists.

**Custom events (`CustomEventData`) — narrower setter surface.** `CustomEventData` extends `GameEventData` directly (not the extended branch of the hierarchy), so the extended setters available on predefined events are not on its API. Use the same XML-docs probe as above to enumerate what IS available on `CustomEventData` and `GameEventData`; everything else flows through `AddCustomParameter("key", value)` and must be registered as a custom parameter on the matching custom-event entry on Dashboard (see §Dashboard verification rule).

**Parallelism does not protect dormant/broken legacy — in-place repair.** Parallelism principle preserves EXISTING WORKING parallel-channel calls so Kinoa integration adds siblings without disturbing existing flow. When an existing Kinoa-shaped call references types/namespaces that don't resolve in `Assets/` (Grep `class <TypeName>` / `namespace <Namespace>` returns 0 hits), the parallel path is dead — won't execute (dead `#if SYMBOL` block) or won't compile (when symbol defined). Parallelism rule does NOT apply.

**Repair the broken call in-place per SKILL.md §"Method-call signature mismatches — in-place repair by default"** — rewrite to current SDK shape, preserving method semantics + arg expressions verbatim. Do NOT add a fresh parallel-channel call alongside (that would duplicate the call at the same site post-repair).

**Applies inside `#if SYMBOL` blocks regardless of whether the symbol is currently defined.** Even when the symbol is inactive (block doesn't currently execute), repair the call so that if the symbol is later defined for any build target, the repaired code compiles cleanly. Skipping repair on inactive blocks just defers the same work to a future moment when the developer activates the symbol — better to fix once during this merge.

**Paired call-site Apply gate — mandatory for every mirrored event (no dead-code mirrors).** A new builder + Send pair in the Kinoa base is unreachable until something invokes it. Without a corresponding game-side call site, the mirror methods are dead code — they ship in the integration but never fire. After adding a builder + Send pair, **immediately open an Apply gate at the matching game-side analytics call site** to add a parallel-channel call alongside the existing analytics provider (Parallelism principle preserved — do NOT edit the existing call; add a new line next to it):

```csharp
// existing analytics call (UNCHANGED — parallelism preserved)
AnalyticsManager.Instance.LogEvent("on_purchase_succeed", parameters);
+ // new Kinoa parallel-channel call (proposed by --merge)
+ KinoaGameEventsService.Instance.SendOnPurchaseSucceedEventAsync(productId, packName, ...);
```

**Placement of the parallel-call line — game-action site, NOT dispatcher.** The Kinoa call goes at the **game-action site** where the existing analytics call is invoked from (e.g., a purchase-handler file after `AnalyticsManager.Instance.OnPurchaseSucceed(...)`, a gameplay-manager file after `AnalyticsManager.Instance.OnLevelUp(level)`, an in-app-event file after a collected-resource event). Do NOT add Kinoa calls inside the centralized analytics dispatcher file itself (e.g., `AnalyticController.cs`, `AnalyticsManager.cs`, `AnalyticsHelper.cs`) — the dispatcher collects taxonomy but is one step removed from the game-action moment; touching it couples Kinoa to the game's analytics-layer plumbing rather than to the game-event semantic. **Special case — provider-registry pattern**: if the project's analytics manager already registers multiple platform providers alongside (e.g., `AnalyticsManager.Providers` collection with `FacebookProvider`/`FirebaseProvider`/etc.), adding a parallel `SendAnalyticsEventForKinoa(eventName)` dispatcher method on the analytics manager IS acceptable — additive only (new method, existing platform paths untouched), pose Apply gate for the additive method.

**Single batch gate for parallel-call wiring — avoid per-event / per-site sequential gates.** After the coverage gate's `(a)/(b)/(c)` choice and pre-flight game-action-site check (above), open ONE batch approval gate listing all picked events × all reachable game-action sites in tabular form: `# / event name / file:line / proposed parallel-call`. 3-way choice: *(a) **all** — wire parallel-call at every listed site; (b) **subset** — list which to wire (e.g., '1, 3, 5-8'); (c) **none** — skip parallel-call wiring entirely.* Bulk approval rolls per-site Apply confirmations into the batch decision — at coverage-gate `(a)` with 13 events × 5 sites avg, sequential gates would be 65; batch approval keeps that as 1 gate. On `(c)` skip-all OR if developer Skips every site at the batch: surface in closing-summary as "mirror generated but not wired — manual subscribe needed at the listed sites."

**If the developer chooses `(c)` skip-all at the parallel-call batch gate → withdraw the mirror methods** (rollback the in-Kinoa-base builder + Send pair). Do NOT ship dead code. Surface in closing summary: "Coverage-gate batch declined — mirror methods rolled back; re-run `/kinoa async-events --merge` if you change your mind."

**Exception — `session_start` mandatory event.** Already wired via `KinoaGameController.LogInAndOpenSessionAsync` startup flow (sample-shipped). Mirror always justified; no game-side parallel-call Apply needed.

**Exception — events triggered by SDK-internal lifecycle** (e.g., `error`, `tick`, `web_socket_opened`). SDK fires these without game-side caller; mirror not applicable.

**Exception — `install` predefined event (SDK auto-fires, NEVER mirror).** The SDK fires the `install` event automatically once per device-install during SDK initialization — detected via the `kinoa_is_first_launch_<GameID>` PlayerPrefs flag. The developer does nothing on the game side. Even when the game's own analytics dispatcher exposes a method named `OnGameInstall` / `OnAppInstall` / `OnFirstLaunch` / `OnInstallWithLink` / similar — **do NOT mirror it** into a Kinoa wrapper. Mirroring would either (a) double-fire `install` (SDK auto-fire + manual mirror = two `install` events per device, breaking Dashboard install-cohort analytics) or (b) silently no-op if the SDK guard rejects the second fire. Surface the discovered game-side install dispatcher in the closing summary's `Discovered but not mirrored` table with the note *"install auto-fired by SDK init — game-side mirror not applicable"* so the developer sees it was discovered and consciously excluded, not missed. **Dashboard side:** Phase 7 includes `install` in the manifest **unconditionally** (SDK-automatic — no call site needed; see `modules/13-dashboard-sync.md` §Sources) and the first `/kinoa dashboard-sync` publishes it NOT_IMPLEMENTED → ACTIVE, so install analytics flow from day one.

#### `KinoaGameEventsService` — adding `Send<NewEvent>EventAsync(...)` methods

**Only if** a corresponding builder was added to `KinoaGameEventBuildingService`. Call pattern follows the sample's existing events. Do not refactor other event methods' bodies.

**Default for custom-event mirrors of an existing analytics taxonomy is async** (`KinoaGameEventsService`) — fire-and-forget semantics where the call site doesn't need an SDK response. See `modules/05-events-sync.md` §"Merge Surfaces" for sync-vs-async criterion. Mirroring the same event into BOTH services is almost always wrong — pick one.

### Frozen (no in-place edits, except where body-extension applies)
- Existing predefined event methods' bodies (`SendPaymentEvent`, `SendLevelUpEvent`, `SendCustomEvent`, etc.) — only ADD new methods alongside
- `Kinoa.GameEvents.*` SDK call signatures
- Event security configuration handling
- Tick events configuration

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`modules/05-events-sync.md` §"Merge Surfaces"](05-events-sync.md#merge-surfaces) — sync and async events share the `KinoaGameEventBuildingService` builders; the sync-vs-async criterion is mutually-exclusive (the same event must NOT be mirrored into both services). Adding a Send method on either side requires evaluating the criterion against the call site. Treat the two modules as a single concern.
- [`modules/06-messaging.md` §"Merge Surfaces"](06-messaging.md#merge-surfaces) — async game events trigger In-apps that arrive via the Messaging WebSocket (handled by `KinoaMessagingService`). Adding new event builders / Send methods that are intended to trigger in-apps requires awareness of the messaging configuration (which event → which in-app on Dashboard, in-app feature configuration registration in 01-init, etc.).

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [04 - Game Events](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275783/04+-+Game+Events+latest+version) — full API reference for all event types, data models, security

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **Custom Event** | `KinoaGameEventBuildingService.OnXxxEvent(...)` builders + matching `KinoaGameEventsService.SendXxxEventAsync(...)` wrappers; raw `CustomEventData("event_name").AddCustomParameter("param_key", value)` calls | [Game Settings → Custom Events](https://dashboard.kinoa.io/game-settings/events/user) | Event name (the `CustomEventData(...)` literal) + every parameter key (each `AddCustomParameter("...", ...)` literal) must be registered. Parameter-name reuse rule applies — keys must mirror the game's existing analytics taxonomy verbatim. |
| **Predefined Event** | sample-shipped `Kinoa.GameEvents.Send<Predefined>Event(...)` calls (`SendPaymentEvent`, `SendLevelUpEvent`, `SendSessionStartEvent`, `SendInstallEvent`, etc.) | [Game Settings → Predefined Events](https://dashboard.kinoa.io/game-settings/events/predefined) | Pre-configured by default — no required registration. SDK-shipped fields auto-populated through inherited setters on the event-data class hierarchy (see §"Merge Surfaces" → "Base-class setter reuse") are part of the predefined entry's schema by default; do NOT re-register them as custom parameters. **Optional:** custom fields layered on top of a predefined event via `AddCustomParameter("key", value)` calls become additional parameters on that predefined event entry — register each on Dashboard for that specific event for it to be referenceable in Dashboard configuration. Same Dashboard parameter-type options as for Custom Events (see Notes below). |
| **Debug Event** | SDK-internal events fired automatically (`web_socket_opened`, `in_app_received`, `feature_settings_smart_request`, etc.) | [Game Settings → Debug Events](https://dashboard.kinoa.io/game-settings/events/debug) | Toggle each between "All Users" and "Only Debug". Test users flagged via `PlayerState.SetIsTester`. Optional per-game tuning. **Custom fields cannot be added to debug events** (unlike predefined events). |

### Notes
- **Unregistered events / parameter keys are still sent from the client** — values reach the SDK and propagate to the backend, but Dashboard cannot reference them in trigger rules, audiences, segmentations, or analytics reports until registered. Registration is therefore not required to NOT BREAK the client, but required for any Dashboard utility.
- Custom event taxonomy should mirror the game's existing analytics taxonomy verbatim (parameter-name reuse rule per §"Merge Surfaces"). Dashboard registration uses the SAME names used in code.
- Each custom parameter has a **Type** selected at Dashboard registration time (applies to both Custom Events and custom parameters layered onto Predefined Events). Type mismatches between code and Dashboard cause silent rejection of values for that parameter. Dashboard types → Game (C#) types:

  | Dashboard type | Game (C#) type |
  |---|---|
  | `Date` | `DateTime` |
  | `String` | `string` |
  | `Number` | numeric (`int` / `long` / `float` / `double` / `decimal`) |
  | `Boolean` | `bool` |
  | `Enumeration` | C# `enum` OR `string` |
  | `String Array` | `string[]` (or any `IEnumerable<string>`) |
  | `Number Array` | numeric array (`int[]` / `long[]` / `float[]` / `double[]` / `decimal[]`, or matching `IEnumerable<T>`) |

## Key APIs
All methods are `void` (fire-and-forget). Each takes an event data object and optional player state.
Every method below has a synchronous counterpart in `Kinoa.SyncGameEvents` (returns `Task<Response<SyncGameEventResponse>>`) — see 05-events-sync.md. Exception: `SendEvents` (batch) has no sync version.

**Predefined events** — pre-registered on Dashboard, ready for trigger rules. Only `session_start` and `payment` are mandatory; the rest are optional — use only those that match your game's events:
- `Kinoa.GameEvents.SendSessionStartEvent(StartSessionEventData, playerState)` — **mandatory**
- `Kinoa.GameEvents.SendPaymentEvent(PaymentEventData, playerState)` — **mandatory**
- `Kinoa.GameEvents.SendProgressionEvent(ProgressionEventData, playerState)`
- `Kinoa.GameEvents.SendLevelUpEvent(LevelUpEventData, playerState)`
- `Kinoa.GameEvents.SendWatchAdEvent(WatchAdEventData, playerState)`
- `Kinoa.GameEvents.SendInGamePurchaseEvent(InGamePurchaseEventData, playerState)`
- `Kinoa.GameEvents.SendTutorialEvent(TutorialEventData, playerState)`
- `Kinoa.GameEvents.SendCollectedResourceEvent(CollectedResourceEventData, playerState)`
- `Kinoa.GameEvents.SendSocialConnectEvent(SocialConnectEventData, playerState)`
- `Kinoa.GameEvents.SendSocialDisconnectEvent(SocialDisconnectEventData, playerState)`
- `Kinoa.GameEvents.SendSocialPostEvent(SocialPostEventData, playerState)`
- `Kinoa.GameEvents.SendErrorEvent(ErrorEventData)` — no player state needed
- `Kinoa.GameEvents.SendInAppCloseEvent(InAppCloseEventData, playerState)`
- `Kinoa.GameEvents.SendInAppClickEvent(InAppClickEventData, playerState)`
- `Kinoa.GameEvents.SendInAppImpressionEvent(InAppImpressionEventData)` — no player state needed

**Custom events** — you can use custom events exclusively instead of predefined ones if predefined names don't match your game's domain:
- `Kinoa.GameEvents.SendCustomEvent(CustomEventData, playerState)`

**Other:**
- `Kinoa.GameEvents.SendResetPlayerStateEvent(playerState)` — resets state on server
- `Kinoa.GameEvents.SendEvents(List<GameEventData>, playerState)` — batch multiple events in one request

## Overview
Async game events are fire-and-forget (`void`). In-app messages triggered by these events arrive asynchronously via WebSocket (see messaging module).

Events are queued locally (persist across sessions, ring buffer of 500 — new events keep appending, oldest are discarded) and sent when connection is available. Each event accepts the current player state; the SDK calculates and sends only the diff (except `SendResetPlayerStateEvent` which sends the full state). Events are the primary Player State synchronization mechanism with the server.

## Best Practices
- Update player state (level, balance, identifiers, etc.) BEFORE constructing and sending the event
- Predefined events have optional convenience setters for built-in fields: `SetPlace(string)`, `SetLevel(int)`, `SetDuration(long)`, `SetStep(int)`, etc. — use them to enrich analytics
- Use `AddCustomParameters(Dictionary<string, object>)` to attach arbitrary data to any event
- Use `SendEvents()` to batch multiple events in one request
- For payment events, use ISO 4217 currency codes. Dashboard displays payment analytics in USD (auto-converted from original currency)

## Configuration Notes (what's NOT in the sample)
- **Mandatory events:** `session_start` and `payment` are mandatory for implementation.
- **Custom events on Dashboard:** If you use custom events instead of predefined ones, you **must** register them on the Dashboard (Events tab) so their names and fields become available for building trigger rules (In-apps, Flows, etc.).
- **GameEventData base methods:** All events inherit `SetPlace`, `SetLevel`, `SetCustomParams`/`AddCustomParameters`, `SetVisibilityInHistory(bool)` (controls whether your client-triggered event appears in Dashboard history).
- **Custom parameter names must match Dashboard:** Parameter keys passed in `AddCustomParameters` must exactly match the field names declared on the Dashboard for that event. Trigger rules only work when client-side field names match Dashboard declarations.
- **Debug events:** SDK and backend automatically trigger internal debug events (e.g., `web_socket_opened`, `in_app_received`, `feature_settings_smart_request`). On the Dashboard Debug tab you can switch each debug event between "All Users" (sent from all users) and "Only Debug" (sent only from users with Debug checkbox enabled).
- **TutorialEventData:** Has two constructors — `TutorialEventData(TutorialAction action)` (enum: Start, Skip, Finish) and `TutorialEventData(string action)` (custom string action). Also supports `SetStep(int)`.
- **InApp event constructors:** `InAppCloseEventData`, `InAppClickEventData`, `InAppImpressionEventData` accept either `string inAppMessageID` or `InAppMessage` object directly.
- **CollectedResourceEventData:** Constructor takes optional `InAppMessage` — represents player collecting resources from an in-app.
- **PaymentEventData:** Also supports `AddSpent(Dictionary<string, decimal>)` to append to existing spent resources.
- **CustomEventData:** Also supports `SetSpent`/`SetReceived` for in-game resource tracking.
- **Cheater/Tester/Blocked flags:** Use `PlayerState.SetIsCheater(bool)`, `SetIsBlocked(bool)`, `SetIsTester(bool)` to mark the player. Send the updated state with any event — a dedicated custom event (e.g., "cheating_event"/"tester_event") or any other event that carries state.
- **Error events:** Do not require player state. Constructor takes `Exception`.
- **Batch events:** `SendEvents()` sends multiple events in a single request. Player State diff is optimized — calculated only for the first event, rest get null diff. Reduces processing time.
- **Game Events Security:** Enabled via `GameEventsSecurityConfiguration(true)` during SDK init. Events include sequence ID to prevent replay attacks.

## Common Mistakes
- Sending events with stale player state (always update state before sending)
- Not knowing that payment analytics on Dashboard are displayed in USD. Kinoa logs events in the original currency but also auto-converts to USD for Dashboard display
- **Using async API for `session_start`** — always use Sync API instead (see 05-events-sync). Sync returns the inbox state in the response. If you use async `session_start`, you must separately call `Kinoa.Messaging.GetInboxMessagesAsync()` (see `KinoaMessagingService.GetInboxMessagesAsync` in 06-messaging) to get the initial in-app messages — this is suboptimal (extra request + race condition window)
- Using async events when you need the in-app message response (use sync events instead)
- Not setting place/level/duration on events that support them (reduces analytics quality)
- Passing null player state for events that require it (only Error and InAppImpression can omit state)
