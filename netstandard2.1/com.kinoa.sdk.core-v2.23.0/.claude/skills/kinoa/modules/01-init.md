# SDK Initialization

## Sample File(s)
- `Services/KinoaSdkInitService.cs`

## Integration Notes
- **Tick events — DO NOT ask the developer.** Always enabled with the sample config (default: 30s interval, as in `TickEventsConfiguration.GetDefault()`). Include as-is.
- **Ask only for:** `GameID`, `GameToken`, log level. Everything else (network config, retry strategy, time config, language config, security config) — take from sample as-is.

- **Log-level wizard question — restricted option set.** Offer only **`Trace` (recommended)**, **`Debug`** (optional), **`Info`** (optional). Do NOT surface `Warning` / `Error` / `Fatal` / `None` as wizard picks — those are runtime tuning levels, not integration-time choices. After the developer picks, the skill MUST print a one-line chat reminder (informational, not a gate): *"⚠ For production release (after final integration testing is complete), switch the log level to `Kinoa.SDK.SetLogLevel(LogLevel.None)` to avoid spamming production logs. Trace/Debug/Info are integration & QA-time settings only."* This reminder lands once per `--auto` / wizard generation in the Phase 5 summary alongside the GameID/GameToken / Dashboard prerequisites — same place as other post-generation TODOs.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces in the generated Kinoa base. All other code in `KinoaSdkInitService.cs` stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa init --merge`.

### Editable surfaces

**`KinoaSdkInitService` initialization parameters:**

- **`GameID` / `GameToken` placeholder literals** (`"YOUR_GAME_ID"` / `"YOUR_GAME_TOKEN"` in samples). **Dashboard-only values** — they come exclusively from Kinoa Dashboard → Game Settings → Integration. Do NOT Grep / Glob / discover them in client code; do NOT propose to read them from `PlayerPrefs`, env vars, or anywhere inside the project. Apply the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates". The only legitimate sources at the Real-value gate: (a) developer pastes values directly, (b) developer points to a separate Dashboard-credentials config they own — in which case surface the need as a Phase 5 / closing-summary reminder, not as a discovery item. **Exception — legacy Kinoa integration detected.** When Phase 0 collision check (see SKILL.md §"Phase 0") detects a legacy parallel Kinoa integration with hardcoded credentials (e.g., `KINOA_GAME_TOKEN = "..."` literal in `AnalyticsConstants.cs`, or `KinoaInitializer.Init(token, url)` arguments), surface those literals at this gate as a **Reuse legacy** option: *"Found legacy Kinoa credentials at `<file>:<line>` — reuse for the new integration?"* This is the only project-internal source for credentials and is gated by Phase 0 detection — does not relax the Dashboard-only rule for projects without legacy Kinoa.

- **`InAppFeatureConfiguration.Register<T>(schemaName, schemaVersion)` lines** — adding new registrations for developer-introduced derived `InAppFeatureConfiguration` types, adjusting schema names / versions to match Dashboard. Registration lines are wired here BEFORE `SDK.Initialize()` per the SDK's init-time registration contract. Schema names follow the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates". The derived types themselves are governed by `modules/06-messaging.md` §"Merge Surfaces". **Gate prompt MUST include this clarifying line:** *"In-app Feature Schema: register only if your in-apps use feature configuration in the template."*

### Frozen (no in-place edits)
- `Kinoa.SDK.Initialize(...)` call and its argument structure (network config, retry config, tick events config, security config, time config, language config)
- Retry strategy defaults (linear vs exponential — both shown as architectural alternatives per Rule 7, dev picks one, do not edit defaults)
- `IsInitialized` guard logic
- Init order (`Register<T>` calls always before `Initialize`)
- **Log level** (`Kinoa.SDK.SetLogLevel(...)` call) — set once during wizard / `--auto` generation per the Integration Notes rule above; `--merge` does NOT propose changes here. If the developer wants to switch level post-merge (e.g., to `LogLevel.None` for production release), they edit `KinoaSdkInitService.cs` directly — that's a deployment-tuning decision, not a Phase 6 merge surface.

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`modules/06-messaging.md` §"Merge Surfaces"](06-messaging.md#merge-surfaces) — `InAppFeatureConfiguration.Register<T>(schemaName)` lines reference derived `InAppXxxFeatureConfiguration` types defined and modified in the messaging module. Adding/changing a Register line here usually pairs with type changes there.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [com.kinoa.sdk.core (latest version)](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275521/com.kinoa.sdk.core+latest+version)
- [01 - SDK Initialization](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275687/01+-+SDK+Initialization+latest+version) — full API reference for all configuration classes, parameters, enums

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **GameID** | `KinoaSdkInitService` — `"YOUR_GAME_ID"` placeholder literal in the `GameSecrets(...)` argument to `Kinoa.SDK.Initialize(...)` | [Game Settings → Integration](https://dashboard.kinoa.io/game-settings/integration) | Game-specific identifier copied verbatim from Dashboard. **Never source from client code** — `PlayerPrefs`, env vars, file scans are all out of bounds. Developer pastes the value into the init call (or a separate Dashboard-credentials config they own — surfaced as a closing-summary reminder). |
| **GameToken** | `KinoaSdkInitService` — `"YOUR_GAME_TOKEN"` placeholder literal in the `GameSecrets(...)` argument | [Game Settings → Integration](https://dashboard.kinoa.io/game-settings/integration) | Same as GameID — Dashboard-only value, paired with GameID. Both must come from the same Dashboard project. |

### Notes
- Both values must be set before `Kinoa.SDK.Initialize(...)` runs — empty / null / placeholder strings cause init to fail.
- `InAppFeatureConfiguration.Register<T>(schemaName)` lines reference In-app Feature Schemas; those instances are governed by [`modules/06-messaging.md` §Dashboard](06-messaging.md#dashboard) (Schema-key registration) — not by this module.

## Key APIs
- `Kinoa.SDK.Initialize(gameSecrets, networkConfig, tickConfig, securityConfig, timeConfig, langConfig)` — initializes the SDK (async, call once at app start)
- `Kinoa.SDK.SetLogLevel(LogLevel)` — sets logging severity (Trace, Debug, Info, Warning, Error, Fatal, None)
- `Kinoa.SDK.SetLogOption(KinoaLogOption)` — sets log formatting options (NoStacktrace, NoTimestamp, NoSeverity, NoGameId)
- `Kinoa.SDK.SetLogOptionForLevel(LogLevel, KinoaLogOption)` — sets log options for a specific severity level
- `Kinoa.SDK.Version` — returns current SDK version string
- `Kinoa.SDK.AllowPii(bool)` — allows SDK to collect and process PII (e.g., device ID) at runtime, if it was set to `false` during initialization
- `JsonUtils.AddCustomConverter(JsonConverter)` — registers custom JSON converter (call BEFORE Initialize)
- `InAppFeatureConfiguration.Register<T>(schemaName, schemaVersion)` — version-specific registration of In-App feature config type (call BEFORE Initialize)
- `InAppFeatureConfiguration.Register<T>(schemaName)` — version-agnostic registration (fallback) of In-App feature config type (call BEFORE Initialize)

## Overview
SDK initialization is the first step in integrating Kinoa. It configures network behavior, retry strategies, heartbeat events, time source, and language resolution. Methods called before `Initialize()` completes are queued and replayed after init finishes (deferred execution pattern).

## Best Practices
- Call `Initialize()` only once at application startup; guard against duplicate calls
- Use exponential retry strategy for general network resilience
- Set `GameSecrets` third parameter (`allowPii`) to `true` to enable device identification
- Validate that `GameID` and `GameToken` are not empty before initializing
- The following must be called BEFORE `Initialize()` — they will not take effect after:
  1. `Kinoa.SDK.SetLogLevel()` / `Kinoa.SDK.SetLogOption()` — logging configuration
  2. `JsonUtils.AddCustomConverter()` — custom JSON converters (if needed)
  3. `InAppFeatureConfiguration.Register<T>()` — In-App feature config types

## Configuration Notes (what's NOT in the sample)
- **RetryStrategy**: also supports `Linear` (const delay via `retryDelay` param). Sample uses `Exponential`.
- **RetryReason**: also supports `ConnectionError`, `FailedRequest`. Sample uses `AlwaysRetry`.
- **TickEventsConfiguration**: also has `GetDefault()` (30s) and `GetDisabled()`. **Disabled by default.** Without ticks, Player Audience recalculation only happens on `session_start`.
- **NetworkConfiguration**: `WebClientType` param defaults to `UnityWebRequestClient`, must use it on WebGL.
- **TimeConfiguration**: uses server time from TimeAPI service; falls back to device time if offline. Auto-updates on init and when game returns from background.
- **LanguageConfiguration**: default `true` (auto-resolve from device). Saved to local storage; resets after cache clear. Set manually via `PlayerState.PersonalInfo.SetLanguageCode()`.
- **Log options**: can be combined with bitwise OR or passed as array. Can be set per severity level via `SetLogOptionForLevel()`.

## Important Notes
- **Await Core SDK init before Push init.** Push initialization will fail if Core SDK hasn't completed initialization.
- **Deferred execution:** Methods called on `Kinoa.SDK` before `Initialize()` completes are queued and replayed after init finishes.
- **NuGet conflicts**: The package bundles System.Text.Json and JsonDiffPatch. If your project already uses these, resolve by keeping the minimum compatible version.

## Common Mistakes
- Calling `Initialize()` multiple times (guard with a boolean flag)
- Registering JSON converters or InAppFeatureConfiguration AFTER `Initialize()`
- Using `HttpWebRequestClient` on WebGL (must use `UnityWebRequestClient`)
- Initializing Push before Core SDK init completes
- Forgetting tick events are disabled by default
