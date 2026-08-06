# Player Account and State

## Sample File(s)
- `Services/KinoaPlayerAccountService.cs`
- `Services/KinoaPlayerStateService.cs`
- `Data/CustomPlayerState.cs`

## Integration Notes
- **Use sample default Player State fields** (`CustomPlayerState` as-is). Do NOT ask about custom properties in wizard mode. **Custom field additions are exclusively a `--merge` workflow** — see §"Merge Surfaces" → field-selection gate. If the developer raises custom fields mid-wizard, defer them: *"I'll generate the sample baseline first; custom field additions go through `/kinoa player --merge` after."*
- **Summary note:** developer can extend `CustomPlayerState` later with their own game-specific fields.
- **Kinoa recovery methods — keep by default, strip only when developer explicitly opts out:** In `--auto`, when the wizard question is skipped, or when the wizard is answered "Yes", **keep** `LogInPlayerWithRecovery()`, `LogInPlayerByRelatedAccountsAsync()`, `GetRelatedAccountsAsync()`, and the `useKinoaRecovery` parameter in `KinoaPlayerAccountService.cs`. Strip them **only** when the wizard developer answers "No" — see the wizard question below.
- **Wizard question (Player Account):** ask the developer once — *"Use Kinoa's built-in Player-ID recovery mechanism (for restoring players on fresh installs)?"*
  - **Yes — use Kinoa recovery**: keep all recovery methods as-is. Do not modify the controller — the sample's existing `LogInPlayer` call stays untouched; developer decides when to pass `useKinoaRecovery: true` at their call sites.
  - **No — the game has its own recovery mechanism** (platform account restore, cloud save, custom account-linking): **strip** Kinoa recovery methods from generated code — remove `LogInPlayerWithRecovery()`, `LogInPlayerByRelatedAccountsAsync()`, `GetRelatedAccountsAsync()`, and the `useKinoaRecovery` parameter along with its `LogInPlayer` branch. Cleaner code; unused paths eliminated.
  - In `--auto`: skip the question entirely; keep methods as-is (safe default; developer can strip manually later if they decide).
- **Keep `GetServerPlayerStateAsync` as-is in `KinoaPlayerStateService.cs`** — do NOT strip. Although `OpenSessionAsync` returns server state at session-open time, this method is useful for explicit server-state refresh mid-session (long idle, cross-device sync, manual reconciliation scenarios).

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the editable surfaces below across `KinoaPlayerAccountService.cs`, `KinoaPlayerStateService.cs`, and `CustomPlayerState.cs`. All other code in these files stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa player --merge`.

### Editable surfaces

#### `CustomPlayerState` / `PlayerStateDictionary` — adding game-specific properties

The sample `CustomPlayerState` ships with `Foo` / `Bar` / `CustomDateProperty` placeholder properties. Replace or extend with game-specific fields that should be tracked on Dashboard (audiences, in-app triggers, Feature Settings conditions, segmentation).

**Sample-field strip — when ≥1 real game-derived field is added by the field-selection batch gate, comment out the sample placeholders (`Foo`, `Bar`, `CustomDateProperty`) in the same edit.** Frozen-scope philosophy permits comment-out (not delete) — preserve them as commented lines so re-running `--merge` retains discoverability. Without this strip, the placeholders ship `Foo="Foo"` / `Bar=null` / `CustomDateProperty=default(DateTime)` to Dashboard alongside the real fields, polluting per-player state with sample noise. No separate Modify gate — fold into the field-selection batch result. If the developer Skips all real-field additions (zero candidates picked), leave sample fields intact.

**Reminder to surface to developer in Phase 5 summary AND on each Apply:** every new field must also be registered on Dashboard with its Player Field Path (letters, numbers, `_`, `-`, `.` dot-separator for nested properties) — see §Dashboard above and §"Configuration Notes → Custom fields for Dashboard".

**Field-depth guidance — two-step discovery:**

**Step 1 (must-do first): enumerate the base `PlayerState` class fields.** The base class is in a compiled DLL (`KinoaCore.dll`) — you cannot Grep its C# source. Use the sibling XML docs file `KinoaCore.xml` that ships alongside the DLL in the package's `Runtime/` folder.

Locate the XML docs file using the same install-mode probe as samples-root discovery in SKILL.md §Generation Strategy:
1. Registry install (most real-developer projects): `Library/PackageCache/com.kinoa.sdk.core@*/Runtime/KinoaCore.xml` — the `@<hash>` suffix changes per version; resolve with Glob.
2. Embedded install (package source checked into `Packages/`): `Packages/com.kinoa.sdk.core/Runtime/KinoaCore.xml`.

Check (1) first; if no match, fall back to (2). Then Grep for `PlayerState` members on the resolved XML path:
```
grep -E 'member name="[PF]:Kinoa\.Data\.State\.PlayerState\.[^"]+"' <resolved-xml-path>
```
This lists every public property (`P:`) and field (`F:`) on the base class (the set is broad by design — session data, player identifiers, devices, purchases, ads, revenue, progress, balance, level, install/registration time, flags like `IsTester`/`IsBlocked`/`IsCheater`, etc.).

**Do NOT propose a custom field for anything already on the base** — base fields feed Dashboard audiences the same way custom ones do. If base has `Level`, use `state.Level` directly; don't add `CurrentLevel` / `PlayerLevel`. Explore nested types too (e.g., `Balance` may already expose the shape you'd otherwise re-mirror) before adding custom.

**Step 2: scan the game's save / data / global classes** and enumerate every field matching the four candidate categories below — **all four carry equal Dashboard utility, don't bias toward any one**, just don't pre-filter the discovery. Probe patterns (combine — single Grep pass):
- **Save / state data:** `*UserData`, `*PlayerData`, `*SaveSystem`, `*Global`, `*UserState`, `*PlayerState`
- **Economy / progression:** `Economy*`, `*Progression*`, `*Level*Data`, `*Stats*`
- **Monetization / IAP / ads:** `IAP*`, `Subscription*`, `*AdRemoval*`, `*NoAds*`, `*Premium*`, `*VIP*`, `*LTV*`, `*Payer*`, `*Monetization*`, `*Purchase*Service`, `*Billing*` — these often hold per-player monetization flags / purchase-history that are critical for Dashboard segmentation
- **Session lifecycle:** `*Session*Data`, `*SessionTracker`, `*ActivityTracker`, `*RetentionTracker`

The four candidate categories (equal priority — surface every match across all four):
1. **Currency / resource balances** — coins, gems, energy, tickets, hard/soft currency.
2. **Progression milestones** — level, XP, streak, completed chapters, current city/stage, journey progress.
3. **Monetization signals** — total spent, payer flag, days since last purchase, subscription state, LTV tier, ads-removed flag, premium status.
4. **Session counters** — total days played, inactive days, sessions-per-week, last-session timestamp.

If the game has no class matching the monetization probe patterns above, that's fine — surface zero monetization candidates rather than synthesize them. The point is that the probe scope **reaches** monetization-related classes when they exist; reference integrations consistently include monetization fields when the game has any IAP / subscription / ad-removal feature.

**Enumerate both settable AND read-only / derived / computed candidates.** Probe finds (a) settable fields/properties with public setters (Pattern B candidates — direct mutation-site wiring) AND (b) read-only properties, computed expressions, and derived signals reachable at session-open (Pattern A snapshot candidates — re-evaluated at hydration time, no setter to wire). Examples of (b): `IsPayer = noOfPurchasesDone > 0`, `LtvTier = Bracket(totalSpent)`, `CurrentCity` snapshot from a sibling state class, `DaysSinceLastPurchase = (now - lastPurchaseTime).Days`. Stopping at (a) only undercounts Dashboard-useful signals — reference integrations consistently include derived/aggregated fields when the underlying data exists.

**Auto-fire trigger — unconditional on every `--merge` reaching module 02.** Field-selection batch gate fires automatically (no developer prompt to start it) whenever the carve-out walk reaches module 02, regardless of whether sample placeholders were already replaced in a prior run. Step-1 (base-class filter) + Step-2 (game-source scan) always run; gate always opens with the candidates table. Predictable behavior — developer doesn't need to remember to re-invoke `/kinoa player --merge` explicitly to extend; `/kinoa --merge` (bare) covers it.

**Already-wired rows in the table.** When the scan re-runs after a previous `--merge` already applied some fields, those fields appear in the candidates table marked `(already wired)` in the Dashboard-utility column, with the proposed `# / Game source / Field name / Type / Category` columns still populated for reference. They are **excluded from the default selection set** — if developer picks `(a) all`, "already wired" rows are NOT re-applied (no churn). Developer must explicitly include them in `(b) subset` (e.g., "1, 3, 5") if they want a re-wire (rare — usually when Type or Category metadata changed).

Without the auto-fire trigger, runs inconsistently skip the highest-impact migration of the entire `--merge` workflow (game-state → `CustomPlayerState` mirroring), leaving Dashboard segmentation blind to player-side state the game already has reachable.

**Field-selection gate — single batch review (NOT per-field ping-pong).** Once Step 1 (base-class filter) and Step 2 (game source scan) are done, present **all candidates in one tabular review** and ask the developer to pick a subset — analogous to the analytics-mirror coverage gate in `modules/04-events-async.md` §"Merge Surfaces". Format:

> *"Pick which CustomPlayerState fields to add — (a) all / (b) subset (e.g., '1, 3, 4') / (c) none:*
>
> *| # | Game source | Field name (proposed) | Type | Category | Dashboard utility |*
> *|---|-------------|----------------------|------|----------|-------------------|*
> *| 1 | `<file:line>` | `<FieldName>` | `<type>` | `<category>` | `<e.g., audience: "...", trigger: "...", segmentation by ...>` |*
> *| 2 | ... | ... | ... | ... | ... |*
>
> *Each field you Apply requires Player Field Path registration on Dashboard → Players (snake_case form) AND a mutation-site update writing `KinoaPlayerStateService.Instance.PlayerState.<field> = newValue;` when game-side `<source>` changes."*

The developer's pick implicitly constitutes per-field Apply intent for each chosen item. Skill applies the chosen subset in one flow (constructor receives only the picked sources, hydration body wires them). **Avoid per-field yes/no ping-pong** — for projects with 10+ candidate fields it's tedious and produces decision fatigue.

The narrower the dev's subset, the closer the integration sits to a narrow Kinoa-relevant scope (just fields Dashboard actually segments / triggers / configures on); broader picks approach a full game-state mirror. There's no hard architectural commitment up front; the dev's per-gate choice determines scope. Default tilt: nudge toward narrower picks by surfacing Dashboard utility per field — fields without a clear utility shouldn't be added.

**Relation between `CustomPlayerState` and event data — not mutually exclusive.** A field like `Level` can validly live on **both** `CustomPlayerState.Level` (persistent snapshot, for audience segmentation) AND as a parameter on `level_start` / `level_end` events (context for THIS event instance). State describes "who the player is right now"; event data describes "what just happened, with the context specific to this event." Overlap is normal and often necessary for correct Dashboard behavior. **Do avoid duplication within a single event's own parameters** (e.g., don't put both `level` and `current_level` on the same event) — but state-vs-event overlap is fine.

#### `KinoaPlayerStateService.GetLocalPlayerStateAsync()` body

The TODO stub wiring to the developer's player-state source. Other methods frozen.

**Sibling-edit trigger — when new fields are added to `CustomPlayerState` (above), wire hydration inside `CustomPlayerState` itself, NOT by piling mapping logic into the service body.** As fields accumulate, inlining per-field reads in `GetLocalPlayerStateAsync` turns the service into a data-mapping dumping ground; keep the mapping where the DTO lives.

**Two shapes depending on source sync-ness:**
- **All sources are synchronous** (field reads, sync getters): add a parameterized constructor to `CustomPlayerState` that takes the source(s) as arguments and populates fields inside it. The service body becomes a one-liner returning `Task.FromResult(new CustomPlayerState(sources...))`. **Prefer this path whenever possible.**
- **Any source requires `await`** (cloud save, async file I/O, API call): use an async `PopulateAsync` instance method instead — you cannot `await` inside a constructor. The service creates the empty instance via the parameterless constructor, awaits `PopulateAsync(sources...)` on it, returns it.
  
  **Performance caveat — surface at the Modify gate before applying:** every `await` in `PopulateAsync` stalls the Kinoa session-open flow until the source resolves. Everything downstream (Kinoa session lifecycle, events, messaging, feature-settings download) waits on these reads. On a slow network or a failing cloud source, a session-open delay becomes user-visible (loading screens, deferred in-apps). Use async population only when a sync snapshot of the field genuinely isn't available; otherwise prefer a sync-available mirror (e.g., a locally-cached copy refreshed in the background, queried synchronously at session open).

**Static-class-initializer-order risk on sync sources** — even sync sources can read garbage if they depend on static class initializers that haven't fired yet. Common shape: a game has a `Global` / `Constants` / `*Manager` static class whose values are populated from `PlayerPrefs` inside a static constructor or a separate `Init()` call. If `KinoaSdkInitService.Initialize()` runs in `Start()` before that static initializer fires (or before `Init()` is called), a constructor reading `Global.someField` gets the type's default (`0` / `false` / `null`) — silent stale data shipped to Dashboard. **Detection:** at the Modify gate for hydration, check whether the source's static initialization is gated by a method called from an earlier scene / bootstrap. If unclear, surface at the gate: *"Your hydration source depends on `<StaticClass>` static state. Confirm it is fully populated by the time Kinoa init runs (check `Awake()` order, scene execution order, or one-time bootstrap)."* Options: (a) explicit init call before Kinoa init, (b) use a different field guaranteed initialized, (c) accept the default-value risk as known.

**Snapshot vs runtime mutation — known trade-off of the constructor pattern.** `GetLocalPlayerStateAsync` reads sources **once at session-open time**. Subsequent in-game mutations to the underlying source (e.g., the player buys No-Ads mid-session) do NOT propagate to `KinoaPlayerStateService.Instance.PlayerState` automatically — the snapshot is frozen.

**Canonical mutation + sync pattern:**
1. **Mutation (anywhere in the game):** when game state changes, write the new value directly to `KinoaPlayerStateService.Instance.PlayerState.<field> = newValue;` from the mutation site (your save / data class's setter, purchase-completion handler, etc.). This update is client-side and lives in memory until the next sync moment.
2. **Sync to Dashboard:**
   - **Initial sync at session open** = `Kinoa.GameSession.OpenSessionAsync` request — sends the **full** local `PlayerState` (no diff calculation; this is the baseline sync point at session start).
   - **Ongoing sync** = any game event after session open (`session_start`, custom events, predefined events — sync or async via `KinoaSyncGameEventsService` / `KinoaGameEventsService`). Each event carries the **diff** of `PlayerState` since the last sync. **Game events are the canonical state-synchronization mechanism for ongoing changes.** `session_start` itself is just another event that carries the diff (not a special full-state push).

**Implication:** if a dev changes `PlayerState.<field>` client-side but never fires an event afterwards, the change stays unsynced. In practice this is rarely an issue — games regularly send analytics events (payment, level_start/end, custom events) and each one carries the state diff. For very-rarely-eventing flows (e.g., a settings toggle in a screen with no event coverage), the dev may fire a dedicated lightweight event after the mutation.

Surface at the Modify gate when applying constructor-pattern hydration: *"This snapshot is read once at session-open and pushed via `OpenSessionAsync` (full state, baseline sync). If `<source>` mutates mid-session, write the new value to `KinoaPlayerStateService.Instance.PlayerState.<field>` from your mutation site — the next game event (any sync or async send) carries the diff to Dashboard."*

**Keep the parameterless constructor in both cases** — required for JSON deserialization of state returned by the SDK.

**Pick one ongoing-sync pattern for the whole `CustomPlayerState` — (A) Pre-event refresh / (B) Mutation-site writes / Modify (describe).** Single Modify gate at hydration time, applied uniformly to all hydrated fields.

- **(A) Pre-event refresh** — inside `Send<X>EventAsync` body-extension, read sources and write to `KinoaPlayerStateService.Instance.PlayerState.<field>` before the SDK call. When (A) is picked, auto-apply `RefreshPlayerStateFromGame()` helper call to **every existing `Send<X>EventAsync` method** in both `KinoaSyncGameEventsService` + `KinoaGameEventsService` — literal symmetric coverage, no per-method gate. Default tilt when: sources are sync-cheap, mutation sites are scattered or hard to hook, the field set includes derived / computed values (timestamp diffs, totals, percentages) with no natural mutation site.
- **(B) Mutation-site writes** — write `KinoaPlayerStateService.Instance.PlayerState.<field> = newValue;` from each game-side setter / state-change handler at the moment the underlying source mutates. Default tilt when: mutation sites are well-defined, few, and reachable.

Don't mix per field — it's noisy to maintain and reviewers can't tell where a given field's truth lives. If pattern (B) is chosen but a specific field has no reachable mutation site (e.g., a derived `InactiveDays` from session-timestamp diff), surface that field as Unresolved at the closing summary — suggest a focused follow-up to wire it via (A) in a session_start body-extension exception. Don't quietly slip it in alongside the (B) edits.

**After the A/B choice:**

- **Pattern A → auto-apply to every `Send<X>EventAsync` method present at session end (existing OR added this session), NO gate.** Generate the `RefreshPlayerStateFromGame()` helper once on the shared base AND insert the one-liner call automatically in **every** `Send<X>EventAsync` method in both `KinoaSyncGameEventsService` + `KinoaGameEventsService`. Coverage MUST extend to overloads / State-2 wrappers added during the same `--merge` session — when module 04 adds a new `SendXEvent(...)` overload after this Pattern A apply, the new method body MUST include the `RefreshPlayerStateFromGame()` call at the top, same as the original. No per-method gate fatigue, no asymmetric coverage between original-and-new methods, no promise-without-delivery — literal symmetric application as the rationale comment implies.
- **Pattern B → single batch gate with auto-discovery (avoid per-site gate fatigue). Auto-discovery first, then batch approval.** For each picked field in the field-selection batch (Step 2 candidates → developer's `(b) subset` pick), Grep the source class for setters / `set` accessors / mutation methods on the underlying field. Examples:
  - Source `BlockGemsUserData.SecuredData.HasBoughtNoAds` → Grep `BlockGemsUserData.cs` for `HasBoughtNoAds = ...` AND for public methods that change `SecuredData` (`AddItem(...)`, `Save()`, `SubscriptionData.UpdateSubscription(...)`).
  - Source `Global.noOfPurchasesDone` → Grep `Global.cs` for `noOfPurchasesDone =`, `noOfPurchasesDone++`, `noOfPurchasesDone +=`.
  - Source `UserStateService.UserModel.MapProgress.EpisodeNumber` → Grep `UserModel.cs` / `MapProgressStateModel.cs` for `EpisodeNumber =` setters.
  
  Then open the batch gate listing all discovered mutation sites in a table: *"Apply Pattern B mutation-site writes at which sites?* — table columns `# / file:line / source mutation / proposed Kinoa write` — `(a) all / (b) subset (e.g., '1, 3, 5') / (c) none`."* If zero mutation sites found for a picked field, surface in closing-summary `Unresolved` as: *"Pattern B chosen but no reachable mutation site found for `<field>` (sourced from `<file:line>`). Either the field has no setters (read-only computed value — switch to Pattern A pre-event refresh for this field as the documented (A) exception above), OR mutation sites use indirect access (e.g., `JsonConvert.PopulateObject(this, ...)` bulk replace). Re-run `/kinoa player --merge` after surfacing reachable mutation sites."*

Without batch approval, the developer faces 10-30 sequential per-edit gates — gate-fatigue territory; without the auto-discovery step in Pattern B, mutation-site enumeration falls to manual post-merge work and Pattern B "ships a promise without delivery" (rationale comment naming sites without actual writes landing).

**`session_start` has no special status** — it's one event among many for PlayerState sync. Body-extension permission applies symmetrically to every `Send<X>EventAsync` method (see [`modules/05-events-sync.md` §Frozen](05-events-sync.md#frozen-no-in-place-edits)).

Propose the source list based on where each field was discovered in Step 2 above. **Without this wiring, `CustomPlayerState` ships with fields defaulting to zero/false/null on every session open** — Dashboard audiences and in-app triggers silently see nothing.

#### `KinoaPlayerAccountService.GetLoggedInPlayerId()` / `LogInPlayer()` body

The `// TODO: implement with your auth` block. Other methods in the file stay frozen.

**Core criterion (must-hold, non-negotiable):** the resulting implementation MUST return a **non-null, non-empty** ID **every time it runs**, including the truly-fresh first-launch new-player case. An impl that can return null breaks every subsequent SDK call (session open, events, messaging, feature settings).

Note: the `PlayerPrefs.GetString("ActivePlayerID", null)` line in the sample stub is **example-only** — a conditional placeholder for "some internal source of Player ID." Do not take it literally in the proposed body. The real sources in a real game are: the game's own auth / user-service (preferred), or `Kinoa.Player.ID` (SDK-persisted, valid as a fallback on non-fresh launches), or Kinoa recovery, or a newly-generated ID as the terminal fallback.

**About `Kinoa.Player.ID`**: the SDK persists the last active Player ID across app launches. So:
- On a truly-fresh first launch (never set before) → `Kinoa.Player.ID` IS null.
- On any subsequent launch → `Kinoa.Player.ID` returns the previously set ID.
It is valid as a **fallback source**, but NOT as the **terminal fallback** — because on the fresh-first-launch path it's null. The terminal fallback must always produce a new ID when every source fails.

**Best practice: use the same Player ID the game already uses to identify the player**, so IDs align across Kinoa, the game's backend, and the game's auth services. Do not mint a Kinoa-only ID that diverges from what the game uses elsewhere.

**Required decision tree when proposing the body:**
- **Game has built-in auth or an internal user-ID source** (Firebase Auth, Google Play Services, Game Center, Unity Gaming Services, Social.localUser, a custom `UserService.UserId` / `AuthService.CurrentUserId`, etc.) → use it as the primary path. If it does not resolve, chain to `Kinoa.Player.ID` as a non-terminal fallback. If that too is null (fresh first launch), the terminal path **generates a new ID** — `Guid.NewGuid().ToString()` is the common shape; any string form is acceptable as long as the game uses the SAME ID on its own side going forward.
- **Game has NO built-in auth mechanism at all** → in this case (and only this case) the Kinoa recovery methods (`LogInPlayerWithRecovery`, `GetRelatedAccountsAsync`) become the relevant layer. Surface them to the developer. The terminal fallback in the no-auth-no-recovery-match branch IS still new-ID generation — never null.

**Multi-candidate disambiguation gate — when ≥2 plausible Player-ID sources exist.** Some games have multiple identity layers — a local/display "Player" used in UI (e.g., `PlayerPrefs.GetString("playerId")`, `PlayerService.PlayerId`, social-display ID) and a backend identity used in server requests (e.g., `GameState.userId`, `AuthService.CurrentUserId`, cognito/Firebase UID, AWS user ID). These can have different values. **The right answer for Kinoa Player ID is whichever ID the game already uses with its own backend** — so the game's backend and Kinoa share the same player identity, unified across systems (this is the Best Practice rule restated for the multi-candidate case). When the discovery probe finds ≥2 plausible candidates, do NOT silently pick the first hit — open a Modify gate enumerating all candidates with source file:line + brief role description: *"Found candidate Player ID sources: (a) `<source A>` at `<file:line>` — used for `<role>`; (b) `<source B>` at `<file:line>` — used for `<role>`; (c) ... Which ID does your game already use with its own backend?"* The developer's answer drives `GetLoggedInPlayerId` wiring.
- **Race-condition trap — extends to Unity's `UnityEngine.Social` / Google Play Games / Game Center:** if the auth API is async (e.g., `SignInAnonymouslyAsync()` on Unity Gaming Services, `PlayGamesPlatform.Instance.Authenticate(...)`, `Social.localUser.Authenticate(...)` — all of which are async even when they look sync at call sites) and may not have completed by the time `LogInPlayer()` runs, propose `await`-ing the sign-in OR subscribing to the auth-completion event and deferring the Kinoa init — NOT reading an uncompleted `CurrentUserId` / `Social.localUser.id` / `localUser.authenticated` and shipping whatever it is.
  
  **Detection heuristic:** before wiring `UnityEngine.Social.localUser.id` / `PlayGamesPlatform.Instance` / any platform auth getter as the primary `GetLoggedInPlayerId()` source, Grep the game's bootstrap for an actual **auth call** — `.Authenticate(...)` or `.SignIn*(...)` — NOT the platform-config call. **`PlayGamesPlatform.Activate()` alone is NOT a race source** — it just registers the platform implementation; the race source is the `.Authenticate(...)` call that follows it (sometimes in a separate method or button handler). If the matching `.Authenticate(...)` / `.SignIn*(...)` call is fire-and-forget (no await, no callback-to-init) — the race IS live. Surface it at the Modify gate: *"Your game calls `Social.localUser.Authenticate` in `<file>:<line>` but doesn't await it before Kinoa init — if Kinoa init runs first, `Social.localUser.id` will be empty. Options: (a) await auth before Kinoa init, (b) hook `Kinoa.Player.SetPlayerId(...)` into the auth-completion callback as a separate post-init write, (c) fall back to `Kinoa.Player.ID` + terminal Guid and accept that fresh-first-launch IDs won't link to the platform ID until Kinoa recovery or a later session."*

**Anti-pattern to reject:** `return platformId ?? Kinoa.Player.ID;` as the terminal expression — this returns null when both are null (fresh first launch with failed platform auth). The correct chain is `platformId ?? Kinoa.Player.ID ?? <newly-generated-id>`. Every proposed body must be able to produce a non-null, non-empty ID from at least one code path, including the all-probes-failed path.

**Anti-pattern to reject — `FindObjectOfType<>()` / reflection lookups from inside `GetLoggedInPlayerId()` body.** Reaching into the scene via `UnityEngine.Object.FindObjectOfType<HostMono>()` (or any equivalent reflection-based scene scan) to access constructor-injected game services is forbidden — every call pays a per-call full-scene scan, the lookup returns `null` when the host isn't yet alive at Kinoa init time, and the resulting rationale comment ends up with `// TODO: verify <host> is alive when Kinoa init runs` admitting an unresolved lifecycle race. Use the DI fork below instead.

**Rationale comment must cite the full chain.** When `GetLoggedInPlayerId()` body is wired with a non-terminal source (game UID + `Kinoa.Player.ID` fallback, no inline `Guid.NewGuid()`), the in-scope rationale comment MUST explicitly state how the non-null guarantee is preserved through `LogInPlayer()`'s terminal `Guid.NewGuid()` fallback (line ~55 in the sample). Without this citation a future reader of `GetLoggedInPlayerId()` alone cannot tell that returning `null` is intentional and handled upstream — it looks like a missing fallback bug. Required template wording (paraphrasable): *"Returns `null` on the all-probes-failed path; non-null guarantee is preserved by `LogInPlayer()`'s terminal `Guid.NewGuid()` fallback when this method returns null/empty."* This is a hard documentation requirement, not optional.

**DI-only codebase fork — when game data is reachable only via constructor-injected services (no static singletons / no `*Manager.Instance` / no `*Service.Current`).** Some projects use Zenject / VContainer / hand-rolled DI (`ICarServices`-style) where the canonical access for game state is `services.<X>Service` from a constructor-injected reference, NOT a static getter. `KinoaPlayerAccountService` is itself a `KinoaSingleton<T>` per the sample's frozen-scope architecture, so it cannot directly access constructor-injected game services. When the discovery probe finds the Player-ID source only behind DI, open a Modify gate enumerating two options:
- **(a) Game-side push from the DI host** — leave `GetLoggedInPlayerId()` returning `Kinoa.Player.ID` (sample-shipped, with the rationale comment chain citation per the rule above), and add an `Edit` on the **game side** at the host's init / login-completion point: `Kinoa.Player.ID = services.GameStateService.GameState.userId;` (or whatever the game's canonical access is). **No reflection, no race** — runs once at the moment the game knows the ID. Per parallelism principle, this is a game-side Edit (in scope) — surface as its own Apply gate alongside the Player-ID gate. **Default tilt** — pick this unless the developer explicitly opts into (b).
- **(b) DI-adaptation of `KinoaPlayerAccountService`** per Code Transformation Rule 6 — restructure the singleton to accept constructor-injected services and align the Kinoa layer with the project's DI scheme. **Legitimate path** when the developer wants architectural consistency across services. **Cost:** changes the class signature and every call site (`KinoaPlayerAccountService.Instance.*` becomes `services.<X>.*`); may break already-integrated samples and require sweeping the entire Kinoa target base. **Requires explicit developer consent** — never default to it. Surface the cost honestly: *"This will change the class signature and update N call sites across the Kinoa base — confirm you want to migrate the entire service layer to the DI scheme."*

Surface the gate verbatim with concrete file:line references for the host MonoBehaviour / DI service container, so the developer sees the trade-offs at the point of decision. Default-tilt narration: *"(a) is the lowest-risk path — game-side init hook reachable from outside Kinoa code, no Kinoa-side restructure. (b) is the architectural-consistency path — pick it only when you intentionally want the Kinoa layer to match your project's DI scheme everywhere."*

**`null` vs empty-string trap:** many real platform-ID sources (`PlayerPrefs.GetString(key, null)` — returns the `null` default literally, or `""` if omitted; `PlayfabLoginHandler.PlayfabID` and similar game-auth wrappers that return `string.Empty` before login; platform-SDK getters that return `""` before initialization) return **empty string**, not `null`, on the not-yet-resolved case. The null-coalescing operator (`??`) does NOT trigger on empty strings — `"" ?? "fallback"` returns `""`, not `"fallback"`. Guard with **`string.IsNullOrEmpty(...)`** in the condition, not `??` in the chain. Correct pattern: `return !string.IsNullOrEmpty(platformId) ? platformId : (!string.IsNullOrEmpty(Kinoa.Player.ID) ? Kinoa.Player.ID : Guid.NewGuid().ToString());` — or split across if-statements for readability.

**Sibling-edit requirement — re-evaluate `LogInPlayer()` after editing `GetLoggedInPlayerId()` (and vice versa).** These two methods form a chain: `GetLoggedInPlayerId()` produces a candidate ID; `LogInPlayer()` consumes it and must ensure the non-null, non-empty guarantee on every path including the all-probes-failed path. Editing one without tracing the other is a common failure mode:
- If you edit `GetLoggedInPlayerId()` to wire a new source (e.g., platform auth ID), trace the resulting value through `LogInPlayer()` and verify the terminal `Guid.NewGuid()` fallback still fires on the empty path. If the new source always returns empty on fresh launch and `LogInPlayer()` no longer has a generative terminal fallback, the chain is broken — fix before moving on.
- If you edit `LogInPlayer()` body, re-inspect `GetLoggedInPlayerId()` for shape changes it must handle (e.g., if `LogInPlayer` now expects a specific ID format, `GetLoggedInPlayerId` must produce it).

**At each Apply of an edit to either method, open a companion Apply gate for the sibling** — *"You edited `<method>`. Review `<sibling>` in the same turn?"* — so the developer sees both sides of the chain in one review window.

### Frozen (no in-place edits, except where body-extension applies)
- Other methods in `KinoaPlayerAccountService.cs` (recovery flow, account linking, `GetRelatedAccountsAsync` body, etc.)
- Other methods in `KinoaPlayerStateService.cs` (state submission, change tracking, `GetServerPlayerStateAsync`, etc.)
- `CustomPlayerState` parameterless constructor (required for JSON deserialization)
- SDK API call signatures (`Kinoa.Player.LogIn*`, `Kinoa.Player.SetPlayerId`, etc.)

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [02 - Player State](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275752/02+-+Player+State+latest+version) — full API reference for Player management, state creation, naming policy, custom fields

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **Player Field Path** (per `CustomPlayerState` field) | every public property on `CustomPlayerState` (sample-shipped `Foo` / `Bar` / `CustomDateProperty` + game-specific additions); every assignment to `KinoaPlayerStateService.Instance.PlayerState.<field>` from mutation sites; nested properties addressed via dot path (e.g., `state.Balance.Coins`) | [Players](https://dashboard.kinoa.io/players) | Each custom field must be registered as a Player Field Path on Dashboard for use in audiences / in-app triggers / Feature Settings conditions / segmentation. Allowed characters: letters, numbers, `_`, `-`, `.` (dot is the separator for nested properties). No spaces. Path uses the SnakeCaseLower form of the C# property name (e.g., `CustomString` → `custom_string`). Base-class `PlayerState` fields (level, balance, identifiers, install/registration time, flags like `IsTester`/`IsBlocked`/`IsCheater`, etc.) are pre-registered — do NOT re-register them. The Dashboard field type must match the C# property type per the table below — type mismatches cause silent rejection of values. <br><br> **Dashboard type → Game (C#) type mapping:** <br> `number` → numeric (`int` / `long` / `float` / `double` / `decimal`) <br> `boolean` → `bool` <br> `string` → `string` (short / regular) <br> `date` → `DateTime` <br> `enumeration` → C# `enum` OR `string` (whichever is more convenient on the game side) <br> `long string` → `string` (used when the value is large free-form text) <br> `version` → `string` (semantic-version-shaped value, but typed as string in code) |

### Notes
- Unregistered fields are still sent from the client and reach the server, but Dashboard cannot reference them in audiences / triggers / Feature Settings filters / segmentation until registered. Registration is therefore optional for client correctness, mandatory for any Dashboard utility.
- Field-path registration is per-field. The dev picks the candidate subset at the field-selection gate (see §"Merge Surfaces" → `CustomPlayerState`); each picked field is then registered separately on Dashboard.
- The Players view is also useful for QA — look up a player by ID to inspect live state, audiences, activity, and events history; verify custom-field serialization against the server-stored state.

## Key APIs
- `Kinoa.Player.ID` (get/set) — active player identifier; persisted across sessions
- `Kinoa.Player.GetStateAsync<T>(playerId)` — gets player state from server (rarely needed — `OpenSessionAsync` already returns server state in response)
- `Kinoa.Player.GetRelatedAccountsAsync(playerSearchIDs)` — finds accounts linked to given IDs (for player recovery)
- `Kinoa.Player.DeletePlayerAsync(playerId)` — deletes a player from Kinoa
- `Kinoa.Player.ApproveStateChangesAsync()` — approves operator-initiated state changes (unblocks future events)
- `Kinoa.Player.SetStateChangedByOperatorHandler(handler)` — registers callback for operator state changes
- `Kinoa.GameEvents.SendResetPlayerStateEvent(playerState)` — resets state (async/fire-and-forget)
- `Kinoa.SyncGameEvents.SendResetPlayerStateEventAsync(playerState)` — resets state (sync, returns response)
- `PlayerState.SetLevel()`, `.SetBalance()`, `.SetProgress()` — predefined state setters
- `PlayerState.PersonalInfo.SetCountry().SetCity()` — personal info builder chain
- `PlayerState.PersonalInfo.SetLanguageCode()` — set preferred language
- `PlayerState.PlayerIdentifiers.SetFacebookId()` etc. — social identity builder

## Overview
Players are created automatically on the first `OpenSessionAsync()` call — no need to call `CreateAsync()` (deprecated). Set `Kinoa.Player.ID` before opening a session. The ID persists across app launches.

Player State is a server-synced object containing player progression (level, balance, identifiers, personal info, custom fields). You extend `PlayerState` with a custom subclass for game-specific properties. The **game is the source of truth** — always pass the latest local state to `OpenSessionAsync()`. The merged result (including server-side `CalculatedFields` and `ActivityStats`) is returned in the response.

## Best Practices
- Set `Kinoa.Player.ID` before calling `OpenSessionAsync()`
- **Use your own player ID as the Kinoa Player ID** — this is the best practice. If your game already has an authentication mechanism and internal Player ID, use it directly with Kinoa. `LogInPlayer()` is the base pattern for this.
- `LogInPlayerWithRecovery()` / `GetRelatedAccountsAsync()` — optional, rarely needed. Only for cases when the game has no mechanism for restoring Player ID on new devices. Most games have their own recovery flow.
- Keep player state updated locally before sending events (events carry state snapshots and SDK calculates diff — only updated properties are sent to server to reduce traffic)
- Always call `ApproveStateChangesAsync()` after handling operator state changes before sending new events

## Configuration Notes (what's NOT in the sample)
- **Serialization:** Player State properties use `SnakeCaseLower` naming policy (e.g., `CustomString` → `"custom_string"`). Dictionary keys are serialized as-is (no naming policy).
- **Custom fields:** Use `[JsonInclude]` and `[JsonPropertyName("...")]` to customize property names. Use `[JsonStringEnumConverter]` for enum naming policy.
- **Custom types:** You can use any custom type in PlayerState without a converter — standard serialization works out of the box. A custom JSON converter (`JsonUtils.AddCustomConverter()` before `SDK.Initialize()`) is only needed when you require custom serialization/deserialization logic (e.g., `CustomBool` serialized as 0/1 instead of true/false). In that case they work in pair — the converter registration in Init and the field in PlayerState. See `CustomBool.cs` + `KinoaCustomJsonConverterSample.cs` in samples.
- **Threading:** Do NOT use Unity APIs (e.g., `SystemInfo.deviceUniqueIdentifier`) in PlayerState constructor or field initializers — SDK deserializes on a background thread. Use a separate method like `SetUnityProperties()` called from the main thread.
- **CalculatedFields:** `PlayerState.CalculatedFields` contains server-computed values from your Google Bucket file. `ActivityStats` are calculated on Kinoa backend. Both are synced on session open.
- **ChangedByOperator:** Two ways to detect operator changes: (1) check `PlayerState.ChangedByOperator` field, or (2) register handler via `SetStateChangedByOperatorHandler()`. Sample uses Option 2.
- **Custom fields for Dashboard:** Custom Player State fields are used on Dashboard for building Audiences, In-app triggers, Feature Settings conditions. Dashboard supports: numbers, boolean, string, date, enumeration types.
- **Enum serialization:** Default enum policy is `SnakeCaseLower`. Use `[JsonStringEnumConverter(typeof(DefaultFallbackStringEnumConverter), JsonNamingPolicy.CamelCase, true)]` for custom enum naming. Use `[JsonPropertyName("...")]` to override individual property names.
- **Player Field Path:** Allowed characters for Dashboard fields: letters, numbers, `_`, `-`, `.` (dot is separator for nested properties). No spaces.
- **PlayerStateDictionary:** Alternative to typed `CustomPlayerState` — use `PlayerStateDictionary` if you don't have a predefined schema. Requires replacing `CustomPlayerState` → `PlayerStateDictionary` in: `KinoaPlayerStateService`, `KinoaGameSessionService`, `KinoaGameEventBuildingService`, `KinoaGameController`. See commented example in `KinoaPlayerStateService.cs` for dictionary-based `LogPlayerState`. Do not generate this variant by default — only when the developer explicitly asks.
- **Localization language:** Set via `PlayerState.PersonalInfo.SetLanguageCode()`. Saved to local storage; backend returns localized content (in-apps, pushes) based on this language.

## Important Notes
- **Player creation is automatic** on first `OpenSessionAsync()`. `CreateAsync()` is obsolete.
- **Operator state changes:** When an operator changes state via Dashboard, the handler fires. You must merge local + remote state and call `ApproveStateChangesAsync()` before sending new events.
- **Reset state:** Use `SendResetPlayerStateEvent()` to overwrite the complete server state with your local version after any merge/update.

## Common Mistakes
- Accessing Unity APIs in `PlayerState` constructor/field initializers (threading error)
- Forgetting to call `ApproveStateChangesAsync()` after operator state changes (blocks future events)
- Not setting `Kinoa.Player.ID` before opening a session
- Player state is optional in most events (except reset only). You can always pass it — SDK calculates the diff automatically. Or omit it if state hasn't changed
- Using `CreateAsync()` directly — obsolete, players are created on session open
