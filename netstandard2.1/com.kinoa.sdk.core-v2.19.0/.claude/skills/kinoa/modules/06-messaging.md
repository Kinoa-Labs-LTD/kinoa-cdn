# In-app Messaging

## Sample File(s)
- `Services/KinoaMessagingService.cs`
- `Services/KinoaUiService.cs` — stub UI service (see Code Transformation Rules)
- `Data/InAppDailyBonusFeatureConfiguration.cs` — include as-is during integration (example of In-app Feature Configuration model)

## Integration Notes
- **Always generate `InAppDailyBonusFeatureConfiguration` as-is.** Do NOT ask for a custom In-app Feature Schema.
- **Feature Schema Key vs Feature Settings Key — they are different:**
  - **Feature Settings Key** (used in `KinoaFeaturesSettingsService`) — the specific FS entry on the Dashboard. Multiple Feature Settings keys can share the same schema.
  - **Feature Schema Key** (used in `InAppFeatureConfiguration.Register<T>(schemaKey)`) — the schema identifier that enables correct deserialization of the Feature Configuration embedded in the In-app body.
- **Summary note:** the developer must register the **Feature Schema Key** used by their In-apps (via `InAppFeatureConfiguration.Register<T>()` before SDK init) and replace `DailyBonus` references with their actual schema key.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the editable surfaces below across `KinoaMessagingService.cs`, `KinoaUiService.cs`, and any `InAppXxxFeatureConfiguration.cs` derived DTOs. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa messaging --merge`.

### Editable surfaces

#### `KinoaUiService` — Tier-1 complex carve-out (highest blocking impact for production)

**This is the largest hand-off in the entire `--merge` flow.** Compared to most carve-outs (placeholder substitution, single field add, single body wire), `KinoaUiService` requires N method bodies to be replaced AND adapter wiring to a real game-side UI class. Real reference integrations spend ~30–60 minutes here, and frequently spawn 10+ supporting files on the game side (popup view classes, click-action handlers, image-download helpers, condition-checkers, ScriptableObject prefab maps). The merge agent MUST signal this complexity to the developer up front, walk a structured decision tree, and — when the dev defers — surface a **specific** Unresolved-items list, not a generic "design popup class first."

The sample ships `KinoaUiService` with `Debug.Log` / no-op stubs intended for UI replacement. Stub method bodies are editable, AND **adding new methods** that expose real UI behavior to other Kinoa services is in-scope.

**Architectural rule (hard):** UI implementation lives **only** in `KinoaUiService` — it is the single facade between Kinoa services and the game's UI layer. Business-logic services (`KinoaMessagingService`, `KinoaSyncGameEventsService`, etc.) keep calling through `KinoaUiService.<Method>(...)` — they must never inline popup/modal/dialog instantiation, scene-layer references, or Unity UI code. If a Kinoa service needs a new UI capability, expose it as a new public method on `KinoaUiService` and call that method from the service. Keeping this boundary means the game's UI implementation choices (popup queue, modal stack, IPopup interface, …) stay confined to `KinoaUiService` and never leak into services whose responsibility is SDK orchestration.

**No `InAppGameObject` intermediate object — work directly with the game's UI types.** Earlier sample versions shipped a `Data/InAppGameObject.cs` minimal stub as an intermediate wrapper. **It is no longer distributed in the package — do NOT create it, do NOT reference it from `KinoaUiService`.** Stub bodies operate directly on the game's existing popup / modal / dialog types (the class the developer's UI layer already uses, e.g., `PopUp`, `Dialog`, `BaseUIGroup`, `KinoaOneCtaPopupView`, etc.).

**Decision tree at start of `--merge` for this module — pick exactly one branch and document the choice:**

1. **Existing popup / modal class fits — adapt directly.** Game has a class like `PopUp`, `Dialog`, `BaseUIGroup`, `IPopup`, etc. that already represents in-app modal UI. `KinoaUiService` stub bodies become thin adapters that:
   - Construct or fetch the game's popup instance for the `InAppMessage`.
   - Map `InAppMessage` content (template type, custom params, buttons, images, texts, milestones, countdown, etc.) onto the popup's existing render API.
   - Wire SDK-fired button-action types (CollectResource / ShowAd / Billing / Close / DeepLink / Custom — see `InAppCustomTemplateData.Buttons[i].ClickConfiguration`) to the game's existing handlers (IAP, ad SDK, resource grant, deep-link router, close, custom-name dispatcher).
   - Call `KinoaMessagingService.UseInboxMessageEligibilityAsync(...)` after a click is consumed.
   
2. **Game has a UI facade but shape differs from `InAppMessage` — `KinoaUiService` becomes an adapter layer.** Game has `UIManager` / `PopupManager` / `DialogService` that orchestrates UI but doesn't have a popup class shaped for in-app messages. `KinoaUiService` stub bodies translate `InAppMessage` → the facade's expected input (e.g., a `DUIData` bag, a `PopupConfig` object, a render-args struct), then enqueue / invoke through the facade. The facade owns lifecycle; `KinoaUiService` owns translation.
   
3. **No popup infrastructure at all — Skip + Unresolved.** Game has only HUD elements / scene-bespoke UI / no modal-popup pattern. Do NOT invent a popup class; do NOT force `KinoaUiService` to instantiate raw `GameObject`s or `Canvas` trees. Skip the carve-out, surface to Unresolved with a **specific** items list (see below), and invite the developer to design the popup architecture first, then re-run `/kinoa messaging --merge`.

**Discovery probe at start of merge — Grep the game for popup / dialog / modal patterns:**
- Class names: `PopUp`, `Popup`, `Popups`, `Dialog`, `Modal`, `Sheet`, `Overlay`, `BaseUIGroup`, `UIGroup`, `IPopup`, `IDialog`.
- Manager facades: `PopupManager`, `PopUpManager`, `PopupsManager`, `UIManager`, `DialogService`, `DialogQueue`, `*PopupController`, `*Popups` (game-action facade pattern with `Show*` methods).
- View base classes: `*PopupView`, `*DialogView`, `*ModalView`.
- ScriptableObject configs: `*PrefabConfig`, `*PrefabSO`, `PopupConfig`.
- **Method-name fingerprints** (any class with these is a Branch-1-eligible adapter target): `ShowPopup(...)`, `Show*InApp(...)`, `Show*Dialog(...)`, `DisplayMessage(...)`, `OpenDialog(...)`, `Open*Popup(...)`. Game-action facades that already accept domain-typed args (e.g., `ShowKinoaOneCtaInApp(InAppMessage)`) signal a real adapter target — do not default to Branch-3 stub when such methods exist.

**In-app handler hand-off probe (Tier-1 fast-path — no glue layer needed).** Beyond popup-manager `Show*` patterns, ALSO Grep for **method signatures accepting `Kinoa.Data.Messaging.InApp.InAppMessage` directly** as a parameter — these are existing game-side adapter handlers that the SDK can hand off to without inventing a new adapter shape. Probe patterns:
- Class name patterns: `*Manager`, `*Service`, `*Handler`, `*Dispatcher`, `*EventManager`, `*EventsManager` (e.g., `InGameEventsManager`).
- Method-name fingerprints accepting `InAppMessage`: `Handle*InApp*(InAppMessage ...)`, `On*InAppReceived(InAppMessage ...)`, `Process*InApp*(InAppMessage ...)`, `*HandleKinoa*(InAppMessage ...)`, `Display*InApp*(InAppMessage ...)`, `Show*Kinoa*(InAppMessage ...)`, `Receive*InApp*(InAppMessage ...)`.
- Direct Grep: `\b(InAppMessage|Kinoa\.Data\.Messaging\.InApp\.InAppMessage)\b` in method parameter lists across `Assets/Scripts/**/*.cs` (excluding `Assets/Scripts/Kinoa/`).

**Hit-count-driven action — auto-wire on exactly 1 match, gate only on ambiguity:**

- **Exactly 1 hit** → **auto-wire** `KinoaUiService.CreateGameInApp` to delegate to the found `<ClassName>.<Method>(InAppMessage)` directly. No Modify gate fires for the wiring decision itself — the choice is unambiguous (one clear Tier-1 adapter target, no glue layer needed). The standard per-edit Apply confirmation still fires for the actual `Edit` that lands the wire, so the developer sees the change before it commits. If the target is `#if <SYMBOL>`-gated, the hybrid `#if`-wiring rule below applies automatically.
- **≥2 hits** → fire ambiguity-resolution Modify gate enumerating all candidates: *"Pick the Tier-1 adapter target — (a) `<ClassName1>.<Method1>(InAppMessage)` at `<file:line1>` / (b) `<ClassName2>.<Method2>(InAppMessage)` at `<file:line2>` / ... / (skip) wire via popup-manager fallback instead."* Developer picks one, then auto-wire proceeds.
- **0 hits** → no Tier-1 fast-path; fall through to popup-manager Branch 1/2 or Branch 3 (Skip + Unresolved) per the decision tree above.

Hand-off probe takes precedence over popup-manager `Show*` probes when both exist — direct `InAppMessage` acceptance is closer to the SDK's contract surface than a domain-typed popup wrapper.

**Template-specific method name → mandatory Unresolved surface.** When the auto-wired handler method's name encodes a specific in-app template (e.g. `*OneCta*`, `*TwoCta*`, `*Banner*`, `*Toast*`, `*Fullscreen*`, `*Modal*`), the wire is still applied as-is (blanket forward), but the closing summary MUST include ONE line under `Unresolved`: *"`<ClassName>.<MethodName>` — method name implies template-specific routing; verify in-app template-key (`(inAppMessage.Data as InAppCustomTemplateData)?.TemplateKey`) matches before shipping (the SDK ships only `<predefined-template-keys>` as predefined; other templates are game-custom and may need separate handlers in `KinoaUiService.CreateGameInApp`)."* No code guard is inserted — developer decides whether to add template-key discrimination based on which templates are registered on Dashboard.

**`#if <SYMBOL>`-gated hand-off target — wire under matching gate AND warn in closing summary (hybrid default).** When the detected hand-off target is inside a `#if <SYMBOL> ... #endif` block (e.g., `#if KINOA`, `#if KINOA_ENABLED`), `KinoaUiService.CreateGameInApp` cannot call it unconditionally — `<SYMBOL>` may be off in some build configurations, making the target invisible to compilation. Detection: when reading the target file/method, check whether the method declaration is enclosed in a `#if`/`#endif` pair (Read the file, scan upward from the method's line for an unmatched `#if`).

**Default behavior:**

1. **Wire the call inside a matching `#if <SYMBOL> ... #endif` block in `KinoaUiService.CreateGameInApp`:**
   ```csharp
   public void CreateGameInApp(InAppMessage inApp, ...)
   {
   #if KINOA
       services.Controller.Views.Popups.ShowKinoaOneCtaInApp(inApp);
   #endif
   }
   ```
   `KinoaUiService` itself stays unconditional (compiles in every build config); the active wire fires only when `<SYMBOL>` is defined. When `<SYMBOL>` is off, the method body is empty — in-apps silently no-op rather than compile-error.

2. **Surface explicit warning in closing-summary `Unresolved` section:**
   *"⚠ Tier-1 hand-off target `<ClassName>.<Method>` at `<file:line>` is inside `#if <SYMBOL>` block. `KinoaUiService.CreateGameInApp` is wired with a matching `#if <SYMBOL>` block; in-apps will display ONLY when `<SYMBOL>` is defined in the build configuration. Verify `<SYMBOL>` is in `ProjectSettings.asset` → `scriptingDefineSymbols` for target build configurations (Editor / iOS / Android). When `<SYMBOL>` is off, in-apps silently no-op (no error, but no display)."*

3. **Modify-gate prompt mentions the `#if` dependency at gate time** so the developer is aware skill is reading legacy build flags: *"Found in-app handler `<ClassName>.<Method>(InAppMessage)` at `<file:line>` inside `#if <SYMBOL>`. I'll wire `KinoaUiService.CreateGameInApp` to delegate under matching `#if <SYMBOL>` block, AND surface a build-define dependency warning in closing summary. Apply / Skip / Modify (route through different handler or remove the `#if` gating manually first)?"*

This hybrid produces a working integration when `<SYMBOL>` is defined (no manual follow-up needed if build config already includes it), makes the build-define dependency explicit so the developer can verify their config, and avoids silent data loss via the closing-summary warning about no-display behavior when `<SYMBOL>` is off.

If hits exist → branch 1 or 2 likely viable; surface candidate classes to the developer in the decision-tree gate (Apply / Skip / Modify per skill rule). If no hits in either probe → branch 3.

**When Branch 1 or 2 is chosen — single batch gate for the carve-out items, not per-method sequential gates.** Branch-1 / Branch-2 adoption requires wiring 5+ `KinoaUiService` methods (`CreateGameInApp`, `RemoveGameInApp`, `ReplaceGameInApp`, `ClearGameInApps`, `TryDisplayInApp`) + 5 click-action handlers (`CollectResource`, `ShowAd`, `Billing`, `Close`, `DeepLink`) + `UseInboxMessageEligibilityAsync` = 11+ items. Open ONE batch approval gate listing all carve-out items in tabular form: `# / method-or-handler / file:line in KinoaUiService.cs / brief description`. 3-way choice: *(a) **all** — wire every listed item against the chosen adapter target; (b) **subset** — list which to wire (e.g., '1-3, 5, 8' for `CreateGameInApp` + `RemoveGameInApp` + `ReplaceGameInApp` + `TryDisplayInApp` + `Billing` click-action); (c) **deferred** — Skip + Unresolved with the full list (equivalent to branch-3 outcome, even though adapter target was found).* Bulk approval rolls per-method Apply confirmations into the batch decision; sequential per-method gates at 11+ items would be gate-fatigue territory. Items not picked at the batch surface in closing-summary `Unresolved` so the developer has an explicit follow-up checklist.

**When Skip + Unresolved is chosen, surface the specific Unresolved items (not generic "design popup class first")**:
- `KinoaUiService.CreateGameInApp(InAppMessage)` — instantiate the popup for a new in-app.
- `KinoaUiService.RemoveGameInApp(InAppMessage)` — close / destroy the popup tied to a removed in-app.
- `KinoaUiService.ReplaceGameInApp(InAppMessage)` — swap content of an active popup with a replacement in-app.
- `KinoaUiService.ClearGameInApps()` — close all active in-app popups (e.g., on scene change or session reset).
- `KinoaUiService.TryDisplayInApp(InAppMessage)` — gating logic (lobby-only, scene-allow-list, condition checks) before showing a popup.
- Click-action handlers per `InAppCustomTemplateData` button-action types: `CollectResource` (grant rewards), `ShowAd` (gate on rewarded-ad ready, then grant on completion), `Billing` (route to game's IAP service), `Close`, `DeepLink` (route to game's screen / scene / shop / level-map), `Custom` (dispatch by `InAppCustomClickConfiguration.CtaName` to a game-defined handler — Share / Invite / Copy / etc., names are operator-defined on the dashboard).
- `KinoaMessagingService.UseInboxMessageEligibilityAsync(message, cancellationToken)` wiring — call after a click is consumed; remove from UI on `Deleted = true`.
- (Optional, advanced pattern from references) Offline eligibility-debt cache — game-side save field tracking pending eligibility consumptions for replay after reconnect.

These items go under closing-summary `### Unresolved items` with file path `Kinoa/Services/KinoaUiService.cs` so the developer has a checklist for the follow-up `/kinoa messaging --merge` run.

#### Sample In-app Feature Configuration DTOs

`InAppDailyBonusFeatureConfiguration`, any other `InAppXxxFeatureConfiguration : InAppFeatureConfiguration` — renaming, re-typing, full replacement to match Dashboard In-app Feature Schema keys. **Creating new derived classes** (e.g., `InAppWheelOfFortuneFeatureConfiguration : InAppFeatureConfiguration`) is also in-scope, alongside their registration in `KinoaSdkInitService.InAppFeatureConfiguration.Register<T>(schemaName)` (governed by `modules/01-init.md` §"Merge Surfaces").

Schema keys / `$type` discriminators / Register'd schema names follow the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates".

### Frozen (no in-place edits, except where body-extension applies)
- `Kinoa.Messaging.*` SDK call signatures — strict frozen
- In-app trigger configuration handling — strict frozen
- `KinoaMessagingService.cs` method bodies (`InitializeAsync`, `OnInAppReceived` / `OnCommandReceived` handling, response processing, eligibility queries, inbox interactions) — **body extension allowed** per SKILL.md §"Frozen-scope philosophy" (preserve key moments: SDK call invocation, callback dispatch, response-status check, sample-shipped trace points; do not rewrite wholesale)
- `KinoaUiService.cs` **Button Click Handling region** (`HandleInAppButtonClickAsync` entry, `RouteByClickConfigAsync` dispatcher, `IsKnownCustomTemplateKey` allowlist, `TryUseEligibilityAsync` server helper, `GrantRewards` economy stub) — **dispatcher structure frozen**: do NOT remove `case`s from `RouteByClickConfigAsync`, do NOT rename the methods, do NOT change the consume+grant pattern, do NOT remove the `// Shared tail` line after the switch.

  **Skill does NOT propose implementations for TODO blocks inside `RouteByClickConfigAsync` switch cases specifically** (ad / IAP / deep-link / soft-currency / reward-preview code). These are game-domain decisions tied to the developer's ad SDK, IAP layer, deep-link router, soft-economy, and reward-preview UI — the skill has no basis to choose between AdMob / Unity Ads / IronSource, between UnityPurchasing / a custom IAP wrapper, etc. **Closing-summary surfacing**: every `--merge` run that generates or modifies `KinoaUiService.cs` surfaces a single line under `### Unresolved items` referencing this method: *"Fill TODO blocks in `KinoaUiService.RouteByClickConfigAsync` switch cases (ad / IAP / deep-link / soft-currency / reward-preview integration). Skill does not auto-propose — developer chooses implementation per game-domain stack."* **Note:** this no-propose rule is scoped to `RouteByClickConfigAsync` only. TODO blocks elsewhere in the sample (other `KinoaUiService` methods, `KinoaMessagingService`, `KinoaSdkInitService`, etc.) follow the standard frozen-scope philosophy and ARE editable / auto-proposable per their respective rules.

  New game-custom Dashboard-defined `template_key`s go to `IsKnownCustomTemplateKey` allowlist (one-liner per key from `KinoaInAppTemplateConstants`); do NOT add per-template inline routing in `HandleInAppButtonClickAsync`.
- `Assets/Scripts/Kinoa/Constants/KinoaInAppTemplateConstants.cs` — **mandatory canonical sample** (imported from `com.kinoa.sdk.core` UPM sample `Kinoa Constants`). Ships predefined `TemplateKeySimple = "simple"` and `TemplateKeyOneCtaPredefined = "one_cta_predefined"` declarations. **Frozen for the 2 predefined Kinoa keys** (rename / removal forbidden — these match server-side discriminators). **Extend with game-custom Dashboard-defined `template_key`s** as `public const string TemplateKeyXxx = "<dashboard_key>";`, then add a matching arm to `KinoaUiService.IsKnownCustomTemplateKey`. All `KinoaInAppTemplateConstants.TemplateKey*` references inside the sample (e.g., `IsKnownCustomTemplateKey` switch arms, any `data.TemplateKey ==` comparisons) must reference the constant by name, not duplicate the string literal — single source of truth.

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`modules/01-init.md` §"Merge Surfaces"](01-init.md#merge-surfaces) — `InAppXxxFeatureConfiguration` derived classes are registered in `KinoaSdkInitService.InAppFeatureConfiguration.Register<T>(schemaName)` BEFORE `SDK.Initialize()`. Adding new derived classes here pairs with a corresponding Register-line edit in 01-init.
- [`modules/04-events-async.md` §"Merge Surfaces"](04-events-async.md#merge-surfaces) — async game events trigger In-apps that arrive via the Messaging WebSocket and are processed here. Understanding which events fire which in-app triggers (Dashboard config) requires awareness of the async event taxonomy defined in 04 — so when working on messaging in-app handling, the relevant async event builders must be loaded.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [06 - In-app Messaging](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275836/06+-+In-app+Messaging+latest+version) — full API reference for initialization, commands, In-app messages, templates, inbox management, eligibility, milestones, feature configurations, security, and data models

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **In-app Configuration** | every game event sent via `Kinoa.GameEvents.Send*Event(...)` / `Kinoa.SyncGameEvents.Send*EventAsync(...)` that is intended to trigger an in-app; external-link / push-notification creation paths via `Kinoa.Messaging.CreateInAppMessageAsync(...)` | [Communications → In-Apps](https://dashboard.kinoa.io/communications/in-app) | In-apps are triggered by events (NOT requested by key) — every event meant to trigger an in-app must have a matching in-app configured here with: trigger event, capping / eligibility, template, content, scheduling, recurrence, audience filters, A/B distribution. |
| **In-app Custom Template** | every `InAppCustomTemplateData.TemplateKey` literal accessed in `InAppMessage.Data` cast paths within `KinoaMessagingService` / `KinoaUiService` (sample-shipped processing) | [Game Settings → In-App Custom Templates](https://dashboard.kinoa.io/game-settings/in-apps) | Each `TemplateKey` referenced in the SDK must match a custom template registered here with its element structure (buttons, images, texts, custom fields, feature configuration slots, milestones shape). The `One CTA Template` is Kinoa-shipped and works out of the box for games already using custom templates. |
| **In-app Feature Schema** | every `InAppFeatureConfiguration.Register<T>(schemaName, schemaVersion)` line in `KinoaSdkInitService` (governed by [`modules/01-init.md` §"Merge Surfaces"](01-init.md#merge-surfaces)); every derived `InAppXxxFeatureConfiguration : InAppFeatureConfiguration` class in `Data/` (e.g., `InAppDailyBonusFeatureConfiguration`) | [Game Settings → Feature Schemas](https://dashboard.kinoa.io/game-settings/new-feature-schemas) | The `schemaName` passed to `Register<T>` must match a Feature Schema entry registered here; the schema shape (property names, types) must match the derived class. Unregistered types deserialize as base `InAppFeatureConfiguration` with raw `ExtensionData` — no data is lost, but typed access via `GetFeatureConfigurations<T>()` returns nothing. **In-app Feature Schema key ≠ Feature Settings key** — the schema key is shared across multiple FS entries (see [`modules/07-feature-settings.md` §Dashboard](07-feature-settings.md#dashboard)). |

### Notes
- Insecure messages (failed checksum / sequence validation) are silently dropped by the SDK — Dashboard configuration cannot bypass this. Per-message security defaults to enabled; can be tuned via `InAppSecurityConfiguration` at `Kinoa.Messaging.Initialize(...)`.
- External Link and Push Notification triggers configure additional fields on the In-app entry (External Link tab / Pushes tab) — verify those tabs at registration time when the client uses `CreateInAppMessageAsync` paths.
- Configuration Filters, audience inclusion (`Audiences`), user-list inclusion (`UserLists`), and A/B distribution (`AbTestDistribution`) on the In-app entry are populated server-side at message resolution — no client-side registration step.

## Key APIs
- `Kinoa.Messaging.Initialize(inAppSecurityConfiguration?)` — initializes messaging (call after SDK init (see 01-init)). Optional `InAppSecurityConfiguration` param; default enables all security validations
- `Kinoa.Messaging.OnInAppReceived` — event (`Action<InAppMessages>`) for incoming WebSocket In-app messages
- `Kinoa.Messaging.OnCommandReceived` — event (`Action<CommandMessage>`) for incoming WebSocket Command messages
- `Kinoa.Messaging.GetInboxMessagesAsync(isViewed?)` — fetches inbox messages; optional `bool? isViewed` filter: `null` (default) = all, `false` = only unviewed, `true` = only viewed
- `Kinoa.Messaging.AcknowledgeInboxMessageViewAsync(message)` — marks a single inbox In-app as viewed; SDK mirrors `IsViewed = true` on the passed-in instance if its UUID appears in `response.Data.AcknowledgedUuids` (server's list of confirmed transitions)
- `Kinoa.Messaging.AcknowledgeInboxMessagesViewAsync(messages)` — marks multiple inbox In-apps as viewed in one request; SDK mirrors `IsViewed = true` on every passed-in instance whose UUID is in `response.Data.AcknowledgedUuids`. Idempotent — server includes already-viewed UUIDs in the response too. Only server-rejected UUIDs (e.g., expired, unknown, foreign player) are omitted — corresponding instances stay with `IsViewed = false`
- `KinoaMessagingService.AcknowledgeInboxMessageViewOptimisticallyAsync(message)` — *sample-side helper, not SDK*: flips `IsViewed = true` locally via `InAppMessage.SetIsViewed(true)` immediately, then dispatches `Kinoa.Messaging.AcknowledgeInboxMessageViewAsync` in the background. Best for tap / impression handlers where UI lag is unacceptable; failures self-heal on the next `GetInboxMessagesAsync`
- `Kinoa.Messaging.DeleteInboxMessageAsync(message)` — deletes a single inbox message
- `Kinoa.Messaging.DeleteInboxMessagesAsync(messages)` — deletes multiple inbox messages
- `Kinoa.Messaging.DeleteAllInboxMessagesAsync()` — deletes all inbox messages
- `Kinoa.Messaging.UpdateInboxMessageAsync(message)` — updates a single inbox message (custom params, metrics, countdown timer)
- `Kinoa.Messaging.UpdateInboxMessagesAsync(messages)` — updates multiple inbox messages
- `Kinoa.Messaging.UseInboxMessageEligibilityAsync(message, cancellationToken)` — consumes eligibility (auto-deletes when eligibility reaches 0)
- `Kinoa.Messaging.CollectMilestonesAsync(message, milestoneIndexes, cancellationToken)` — collects milestone rewards
- `Kinoa.Messaging.CreateInAppMessageAsync(externalLink)` — creates In-app by external link
- `Kinoa.Messaging.CreateInAppMessageAsync(inAppByPushCreationParams)` — creates In-app triggered by push notification
- `InAppMessage.IsViewed` — server-driven `bool` indicating whether the In-app was acknowledged as viewed; resets to `false` server-side whenever a new `Command` is dispatched for that instance (reminder / replaced / score-changed / milestones-progress-changed / missions-progress-changed / instance-update)
- `InAppMessage.FeatureConfigurations` — `List<InAppFeatureConfiguration>` of inline feature configurations attached directly on the In-app (populated when the Dashboard's Feature Configuration Mode is set to **Define manually**). Mutually exclusive with `FeatureSettings`.
- `InAppMessage.FeatureSettings` — `List<InAppFeatureSetting>` of embedded Feature Settings referenced by the In-app (populated when the Dashboard's Feature Configuration Mode is set to **Use existing Feature Settings**). Each entry carries `Key`, `ConfigurationName`, `Filters` (actual player-state values used by the server to pick the row), and `Data: List<InAppFeatureConfiguration>` resolved via the same `$type` registry. Mutually exclusive with `FeatureConfigurations`. The row's dashboard-configured filter criteria live on the derived configuration items (`filter: *` JSON properties), separate from the player-state echo in `Filters`.
- `InAppMessage.GetFeatureConfigurations<T>()` — returns typed feature configuration entries merged from BOTH `FeatureConfigurations` (inline) and `FeatureSettings[].Data` (embedded), in that order.Source-agnostic.
- `InAppMessage.GetFeatureConfigurations<T>(string featureSettingKey)` — typed configs from one specific embedded Feature Setting. Use when multiple Feature Settings share the same Feature Schema and must be disambiguated by FS key
- `InAppMessage.GetFeatureSetting(string key)` — returns the single embedded `InAppFeatureSetting` with the specified FS registration key, or `null` if absent. Use when the caller needs the FS metadata (`Key`, `ConfigurationName`) alongside the data
- `InAppMessage.GetFeatureSettings<T>()` — returns the subset of `FeatureSettings` filtered by the derived type `T` of configurations they carry. Use to enumerate distinct FS instances grouped by carried Feature Schema
- `InAppMessage.BundleResources` — `Dictionary<string, List<Resource>>` of server-resolved bundle resources, keyed by bundle key. Populated for every Bundle-type schema field referenced by attached Feature Configurations / Feature Settings; lookup as `inApp.BundleResources[bundleKey]`. See Configuration Notes below and [`modules/08-bundles.md`](08-bundles.md)

## Overview
The Messaging module handles real-time In-app messages delivered via WebSocket and server-side inbox management. In-app messages must be configured on the Kinoa Dashboard before they can be received.

### Inbox vs Non-Inbox
Messages are categorized by storage type:
- **Inbox** — stored server-side, has a defined Countdown Timer. Available via `GetInboxMessagesAsync()` until countdown expires, persists across app restarts.
- **Non-Inbox** — not stored in inbox, no Countdown Timer. Displayed once upon triggering and then disappears.

To trigger In-app messages from a previous game session's events, enable **"Support offline mode"** on the Dashboard when configuring the In-app.

### Message Templates
Templates are selected during In-app creation on the Dashboard:
- **Simple** — Kinoa's predefined simple template (`InAppSimpleTemplateData`)
- **Custom** — game-specific custom template defined by the game team (`InAppCustomTemplateData`)
- **One CTA Template** — Kinoa's predefined custom template with a single Call-To-Action button. Works out of the box for games already integrated with custom templates — no additional SDK changes required. Operator configures CTA content/rewards/triggers on Dashboard; developer ensures correct UI rendering

### Command Messages
There are two distinct concepts of "commands" in the Messaging module:
1. **Command as a separate WebSocket message type** (`CommandMessage` via `OnCommandReceived`) — a standalone message, not an In-app. Instructs the game to perform an action.
2. **Command inside an In-app message** (`InAppMessage.Command`) — an instruction attached to an inbox In-app (e.g., `InAppReplacedCommand`, `InAppReminderCommand`), indicating what happened to that In-app.

Standalone Command message types:
- `ReloadP2PCommand` — new P2P events received for the active player; retrieve the list of inbox P2P events
- `RemovedInboxInAppsCommand` — In-app messages removed from inbox; remove from UI. Contains `InApps` list with `Uuid` and `MessageId` per removed item

## Best Practices
- Always call `Kinoa.Messaging.Initialize()` after SDK init (see 01-init)
- Subscribe to `OnInAppReceived`/`OnCommandReceived` from a single entry point to avoid duplicate handlers. If re-subscribing is needed, always unsubscribe first (`-=` before `+=`) to prevent duplicates
- Separate processing of Inbox vs Non-Inbox messages — they have different lifecycles
- For Inbox messages via WebSocket, handle all In-app command types: `InAppReplacedCommand`, `InAppReminderCommand`, `InAppScoreChangedCommand`, `InAppMilestonesProgressChangedCommand`, `InAppInstanceUpdateCommand`. `InAppScoreChangedCommand` and `InAppMilestonesProgressChangedCommand` have a `DisplayOnProgressChange` flag — use it to decide whether to re-show the In-app or just refresh data silently
- For `RemovedInboxInAppsCommand`, remove the In-app game objects from the UI
- Check `message.Capping?.EligibilityLimit` before calling `UseInboxMessageEligibilityAsync`
- After `UseInboxMessageEligibilityAsync`, check `response.Data.Processed` to determine if reward can be granted, update local eligibility with `message.SetLocalEligibility(response.Data.ActualEligibility)`, and remove the UI element if `response.Data.Deleted` is true
- Handle `ResponseErrorCode.InAppNotFound` in eligibility responses — the message may have expired or been deleted server-side
- Use `message.SetMilestonesStatusAsCollected(response.Data.Collected)` after collecting milestones; give rewards from `milestonesData.Steps[index].Button.Resources`
- Send analytics events at the appropriate UI lifecycle points: `SendInAppImpressionEvent` (on display), `SendInAppCloseEvent` (on close), `SendInAppClickEvent` (on CTA click)
- Two approaches to remove In-app after reward collection: (1) `DeleteInboxMessageAsync` if no Eligibility configured, (2) `UseInboxMessageEligibilityAsync` if Eligibility is configured (auto-deletes at 0)
- Call `AcknowledgeInboxMessage(s)ViewAsync` after the player views an In-app (e.g., impression event) or closes an inbox panel showing a batch of in-apps. The server resets `IsViewed = false` on each new `Command` (reminder / replaced / score-changed / milestones-progress-changed / missions-progress-changed / instance-update), so re-acknowledge each time. Use `GetInboxMessagesAsync(isViewed: false)` to drive a lobby "unread" badge / inbox "new" indicator
- **Two integration paths for acknowledge-view** — `AcknowledgeInboxMessageViewAsync(message)` for server-confirmed response (`await`-based, default; SDK mirrors `IsViewed` on the passed-in instance via `response.Data.AcknowledgedUuids`), OR `AcknowledgeInboxMessageViewOptimisticallyAsync(message)` for tap / impression handlers where UI lag is unacceptable (flips `IsViewed` via `message.SetIsViewed(true)` immediately + dispatches server sync; failures self-heal on the next `GetInboxMessagesAsync`). The sample exposes both as separate methods on `KinoaMessagingService`. **No Modify gate in `--merge`** — this is a per-call-site UX choice the developer makes during integration, not a skill-driven decision

## Configuration Notes (what's NOT in the sample)
- **Obsolete API:** `SetMessageHandlers(commandCallback, inAppCallback)` is obsolete. Use `OnInAppReceived` / `OnCommandReceived` events instead.
- **Security verification:** The SDK verifies In-app integrity by comparing server-side and SDK-side checksums and sequence IDs. Insecure messages are silently dropped.
- **Custom parameters with images:** `InAppMessage.CustomParams` supports image values in addition to primitives. Image values are automatically deserialized as `InAppCustomFieldOfTypeImage`. Same pattern works for element-level `CustomFields` on buttons, images, texts, and custom elements.
- **Configuration Filters:** `InAppMessage.ConfigurationFilters` contains Player State fields used to match against the Configuration table. `InAppMessage.ConfiguredFilters` contains filter definitions from the Configuration table columns. Both are populated server-side when the In-app is resolved.
- **Segmentation properties:** `InAppMessage.Audiences` — audience inclusion, `InAppMessage.UserLists` — user list inclusion, `InAppMessage.AbTestDistribution` — A/B test group assignment. These work alongside Configuration Filters for server-side personalization.
- **Feature Configurations — two delivery modes (mutually exclusive, chosen on the Dashboard via Feature Configuration Mode per In-app configuration):**
  - **Define manually** → server populates `InAppMessage.FeatureConfigurations` (inline list).
  - **Use existing Feature Settings** → server populates `InAppMessage.FeatureSettings` (list of `InAppFeatureSetting` wrappers; each wrapper carries `Key`, `ConfigurationName`, `Filters` (actual player-state values used to pick the row), and a `Data` list whose items follow the same `$type` registry). Each data item carries the row's payload **and** its dashboard-configured filter criteria via `filter: *` JSON properties — distinct from the player-state echo in `Filters`.

  Registration is unchanged for both modes: register types before `SDK.Initialize()` (see 01-init) via `InAppFeatureConfiguration.Register<T>(schemaName, schemaVersion)`. Exact version match takes priority over version-agnostic. Unregistered types deserialize as base `InAppFeatureConfiguration` with raw data in `ExtensionData` — no data is lost.

  See Key APIs above for the runtime accessors (`GetFeatureConfigurations<T>()`, `GetFeatureConfigurations<T>(featureSettingKey)`, `GetFeatureSetting(key)`, `GetFeatureSettings<T>()`).
- **Bundle fields in Feature Configuration schemas → `InAppMessage.BundleResources`:** when a Feature Configuration schema declares a field of **Bundle** type (e.g., `BundleKeyField` in a derived `InAppXxxFeatureConfiguration`), the server resolves the referenced bundle and ships its resources alongside the In-app at `InAppMessage.BundleResources[bundleKey]` (`Dictionary<string, List<Resource>>`). This applies to bundle fields under both delivery modes (inline `FeatureConfigurations` and embedded `FeatureSettings`). The dictionary aggregates resources for every distinct bundle key referenced by the In-app, so a multi-FS In-app whose schemas reference multiple bundles will carry one entry per bundle key. Read the bundle key value from the typed feature config, then index into `BundleResources` — no separate `Kinoa.Bundles.GetBundleResourcesAsync(...)` call is required (SDK 2.9.0+). See [`modules/08-bundles.md`](08-bundles.md) for the `Resource` shape and `Body` (operator-controlled, opaque to Kinoa) semantics.
- **CountdownTimer behavior:** Use `IsExpired` to check base countdown expiry. Use `EndTimestampWithExtraLifeTime` for the full lifespan end (countdown + configured extra days for reward collection).
- **Milestones post-expiration:** After countdown timer expires, milestones In-app remains in inbox for additional days configured in Dashboard (Game Settings > In-Apps > "Allow Extra Time to Collect Reward"). During this period: collecting unclaimed rewards is enabled, but progression is halted, reminders are disabled, replacements are not allowed. Auto-delete occurs after configured period.
- **Milestones progress behavior:** On the initial In-app trigger, `InAppMilestonesProgressChangedCommand` will be `null`. Progress value is **not reset** after reaching a milestone (e.g., milestones at 20, 40, 60 — after reaching 20, Progress stays at 20; 20 more points needed for the next).
- **External Link trigger:** Configure on Dashboard > In-app > External Link tab. Distribute the auto-generated token via Deep Links. Call `CreateInAppMessageAsync(externalLink)` to create the In-app.
- **Push Notification trigger:** Configure on Dashboard > In-App > Pushes tab. Link push Click Action to "Trigger In-App". Call `CreateInAppMessageAsync(InAppByPushCreationParams)` to create the In-app.

## Important Notes
- **Security is enabled by default.** Insecure messages (failed checksum or sequence validation) are silently dropped by the SDK.
- **UI implementation is client-side.** The sample uses `KinoaUiService` as a demo reference, but each game implements its own UI logic for displaying, replacing, and removing In-apps (content loading, layout, animations, etc.).
- **Non-inbox messages have no eligibility limitations.** `UseInboxMessageEligibilityAsync` is not needed for non-inbox messages.
- **In-app Feature Configuration registration must happen before `SDK.Initialize()`** — only if you use Feature Configurations passed inside In-app messages. See module 01-init for `InAppFeatureConfiguration.Register<T>()`.
- **Milestones In-app is triggered like any regular In-app** (via WebSocket or Sync API) — the difference is the additional `Feature` field on the custom template data.
- **In-app field access is demonstrated in the sample logging methods.** The `LogInAppDetails` method and its helpers in `KinoaMessagingService` show how to access all In-app message fields (templates, buttons, images, texts, customs, milestones, countdown timer, capping, segmentation, feature configurations, embedded feature settings, bundle resources, security, etc.). For Feature Configurations / Feature Settings, the sample demonstrates all four access paths: the raw `FeatureConfigurations` / `FeatureSettings` collections, the source-agnostic `GetFeatureConfigurations<T>()`, the key-lookup `GetFeatureSetting(key)`, and the type-filtered `GetFeatureSettings<T>()`. These are purely data access examples — not business logic. Always include Log methods when generating integration code.
- **WebSocket and Sync API share the same In-app processing logic.** Both channels deliver the same categories of inbox changes (new, replaced, reminder, progression score, milestones progress, instance update). The difference is the source: WebSocket In-apps carry instructions as `InAppMessage.Command` in the message body, while Sync API delivers them via `InboxDetails` categories in the response (see 05-events-sync). The sample `KinoaMessagingService` mirrors the `KinoaSyncGameEventsService` processing structure (`ProcessNewInApps`, `ProcessReplacedInApps`, etc.).

## Common Mistakes
- Duplicate event handlers from multiple subscriptions to `OnInAppReceived`/`OnCommandReceived` — subscribe from a single entry point, or unsubscribe first (`-=` before `+=`)
- Not checking `message.Capping?.EligibilityLimit` before calling `UseInboxMessageEligibilityAsync` — messages without eligibility are limited by countdown timer only
- Not implementing behavior for all In-app command types (`InAppReplacedCommand`, `InAppReminderCommand`, `InAppScoreChangedCommand`, `InAppMilestonesProgressChangedCommand`, `InAppInstanceUpdateCommand`) — the game UI must react to each command. Note: Sync API delivers instructions via `InboxDetails` categories (see 05-events-sync), while WebSocket In-apps carry instructions (commands) in the In-app message body (`InAppMessage.Command`)
- Not removing In-app from UI when `UseInboxMessageEligibilityAsync` returns `Deleted = true`
- Not checking `response.Data.Processed` — if `false`, the eligibility limit is reached and reward should NOT be granted
- Not handling `ResponseErrorCode.InAppNotFound` which indicates the message was deleted or expired server-side
- Casting `message.Data` without checking the template type — use pattern matching (`InAppSimpleTemplateData`, `InAppCustomTemplateData`)
- Casting `customTemplateData.Feature` without null-checking — not all Custom template messages have a Feature
- Using the obsolete `SetMessageHandlers` API instead of `OnInAppReceived`/`OnCommandReceived` events
- Ignoring `InAppCountdownTimer.IsExpired` vs `EndTimestampWithExtraLifeTime` — the base timer may be expired but the In-app can still be usable during the extra-life period (for milestones reward collection)
