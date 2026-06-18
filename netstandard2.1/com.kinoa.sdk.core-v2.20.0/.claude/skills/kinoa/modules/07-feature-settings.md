# Feature Settings

## Sample File(s)
- `Services/KinoaFeaturesSettingsService.cs`
- `Data/FeatureSettingsData.cs`
- `Data/DailyBonusSettings.cs`
- `Data/WheelOfFortuneSettings.cs`

All 4 files must be included during integration.

## Integration Notes
- **Always generate both sample schemas** — `DailyBonusSettings` + `WheelOfFortuneSettings` (plus `FeatureSettingsData` base). Do NOT ask for a custom schema.
- **Summary note:** instruct the developer to replace `DailyBonus` / `WheelOfFortune` with their actual **Feature Settings Keys** (each FS entry on the Dashboard has its own key; multiple keys can share the same schema), and adjust the derived DTO classes accordingly. Note: Feature Settings Key ≠ Feature Schema Key used in messaging — see 06-messaging module.
- **Always include:** Logging region, Default Parameters region, Local Settings Management region from `KinoaFeaturesSettingsService`.
- **SmartDownloadAsync and all other methods** — import as-is from the sample.
- **Dynamic FS updates (optional):** If the game needs to detect and apply server-side FS changes not only on startup but also during gameplay (e.g., operator published new reward config while player is in a session), ask the developer to choose between:
  - `DownloadIfChecksumChangedAsync` (recommended) — manual, call at defined game moments. **Exclude** Checksums & Long Polling region except `GetChecksumsAsync`.
  - `ConfigureChecksumLongPolling` — background polling. **Exclude** `DownloadIfChecksumChangedAsync`.
  - Both — include all.
  - Neither — **exclude** both `DownloadIfChecksumChangedAsync` and Checksums & Long Polling region. Only include the methods the developer selected.
- **Summary note:** For MVP, only `SmartDownloadAsync` is required. All other methods are optional and depend on your integration scenario.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the editable surfaces below across `KinoaFeaturesSettingsService.cs`, `FeatureSettingsData.cs` (and its derived DTOs), and `KinoaGameController.DownloadFeatureSettingsAsync()`. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa feature-settings --merge`.

### Editable surfaces

#### Sample Feature Settings DTOs (`DailyBonusSettings`, `WheelOfFortuneSettings`, `FeatureSettingsData` polymorphic base)

Renaming, re-typing properties, replacing `$type` discriminators to match Dashboard Feature Settings keys; full class replacement when the Dashboard shape diverges from the sample.

**Adding new DTOs** derived from `FeatureSettingsData` (e.g., `LevelRewardSettings`, `StarterPackSettings`, etc.) to match additional **Dashboard Feature Settings** entries — welcomed and in-scope. Register each new derived type via the appropriate `[JsonDerivedType]` attribute on the base class.

**Game config / economy discovery probe — mandatory pre-walk for this module.** The sample DTOs (`DailyBonusSettings`, `WheelOfFortuneSettings`) are placeholders for game-specific schemas. Without this probe, the integration ships with sample-default DTOs that don't reflect the game's actual tunable surface, and the dev has to discover the FS-mirroring opportunity manually. Scan the game's existing config / data layer (standard skill scope — entire project minus Kinoa target base + Library/Packages/KinoaPackages — game configs typically live under `Assets/Scripts/<feature>/`, `Assets/Scripts/Design/`, `Assets/Scripts/Configs/`, or feature-specific `*/Model/` folders). Probe patterns:
- `*Config`, `*Settings`, `*Data` (e.g., `ShopConfig`, `LevelSettings`, `DesignData`, `RemoteConfig*`)
- `*Economy*`, `*Pricing*`, `*Rewards*`, `*Pack*Data`, `*Bundle*Data`
- Files under `Configs/`, `Data/`, `Design*/`, `Economy*/`, `RemoteConfig*/`, `*/Model/` directories

For each config class with **structured tunable properties** (≥3 typed fields representing operator-tunable parameters — currency amounts, durations, cooldowns, prices, reward sizes, schedules, etc.), surface as candidate FS-DTO mirror and **open a Modify gate per candidate**:

> *"Found candidate game config: `<file:line>` `<ClassName>` with fields:*
> *- `<field1>`: `<type1>`*
> *- `<field2>`: `<type2>`*
> *- ... ;*
>
> *This shape may mirror a Dashboard Feature Settings schema. Mirror as a new DTO derived from `FeatureSettingsData`?*
> *(a) **Apply** — create `<ClassName>Settings : FeatureSettingsData` with matching field shape + `[JsonDerivedType(typeof(<ClassName>Settings), "<dashboard-fs-key>")]` registration on the base. Dashboard-context 3-way choice applies to the discriminator string.*
> *(b) **Skip** — game uses local config, no Dashboard FS adoption planned for this class.*
> *(c) **Modify** — describe the adaptation (rename fields, drop subset, regroup, etc.).*"

If Apply: generate sibling DTO file alongside existing sample DTOs in `Data/`, follow same `[JsonInclude]` / `[JsonPropertyName]` patterns; pair with FS-key Modify gate at `KinoaGameController.DownloadFeatureSettingsAsync()` call site. Reference integrations consistently mirror at least one game-specific FS schema (shop items, interstitial cooldowns, level rewards, etc.); leaving only sample `DailyBonusSettings` / `WheelOfFortuneSettings` is a strong signal that the discovery probe was skipped.

**FS payload consumer probe — surface in closing summary (no Apply gate, body wiring is dev-domain).** Probe game-side code for existing methods that consume FS data after download. Awareness signal only — skill does not auto-wire consumer body (game-side state shape, field mapping, and projection logic are dev-domain decisions). Probe patterns:
- Method-name fingerprints: `Apply*FeatureSettings(...)`, `*ApplyKinoa*Settings(...)`, `On*FeatureSettings*Downloaded(...)`, `Use*FeatureSettings(...)`, `OnRemote*Config*Updated(...)`, `OnConfigUpdated(...)`.
- Class names with FS consumer pattern: `*ConfigApplier`, `*SettingsApplier`, `RemoteConfigConsumer`, `*FeatureSettingsHandler`.
- Direct Grep: `\bLocalFeatureSettings\b` AND `\bKinoaFeaturesSettingsService\b` references in non-Kinoa game code (`Assets/Scripts/**/*.cs` excluding `Assets/Scripts/Kinoa/`).

**If consumer found** — surface in closing-summary `Dashboard prerequisites` section: *"Found FS consumer `<ClassName>.<Method>()` at `<file:line>`. Verify it reads from `KinoaFeaturesSettingsService.Instance.LocalFeatureSettings` and hydrates game state correctly."*

**If no consumer found** — surface in closing summary as Unresolved: *"Feature Settings DTOs generated but no game-side consumer detected. To wire FS into game state, add a method like `ApplyKinoaFeatureSettings()` on your config class that reads from `KinoaFeaturesSettingsService.Instance.LocalFeatureSettings` and hydrates your in-memory game config; re-run `/kinoa feature-settings --merge` after."*

`$type` discriminator values follow the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates".

#### `KinoaGameController.DownloadFeatureSettingsAsync()` — Feature Settings keys

The `key` literal inside each `FeatureSettingsSmartDownloadRequestParams(...)` constructor — match Dashboard Feature Settings keys. The `version` parameter and `getDefault` / failed-download-strategy / `compressData` flags also editable per Dashboard configuration.

FS keys follow the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates". The `LogInAndOpenSessionAsync` orchestration around this call (when `DownloadFeatureSettingsAsync` runs in the startup flow) stays frozen — see `modules/12-controller.md` §"Merge Surfaces".

### Frozen (no in-place edits, except where body-extension applies)
- `Kinoa.FeatureSettings.*` SDK call signatures — strict frozen
- `KinoaFeaturesSettingsService.cs` method bodies (`SmartDownloadAsync`, `DownloadAsync`, `GetCachedAsync`, checksum / long-polling logic, built-in fallback handling, response processing, cache lifecycle / merge strategy) — **body extension allowed** per SKILL.md §"Frozen-scope philosophy" (preserve key moments: SDK call invocation, callback dispatch, response-status check, sample-shipped trace points; do not rewrite wholesale)

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`modules/12-controller.md` §"Merge Surfaces"](12-controller.md#merge-surfaces) — `KinoaGameController.DownloadFeatureSettingsAsync()` is the call site that issues FS requests. The orchestration around it (when FS download runs in startup flow, retry sequencing) is governed by the controller.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [07 - Features Settings](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119275943/07+-+Features+Settings+latest+version) — full API reference for download methods, caching, checksum polling, built-in fallback, and data models

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **Feature Settings entry** (per key) | every `key` literal inside `FeatureSettingsSmartDownloadRequestParams(...)` / `FeatureSettingsDownloadRequestParams(...)` constructors at `KinoaGameController.DownloadFeatureSettingsAsync()` and any other call site; every `[JsonDerivedType(typeof(<DTO>), "<discriminator>")]` attribute on the polymorphic `FeatureSettingsData` base | [Feature Settings](https://dashboard.kinoa.io/new-feature-settings) | Each Feature Settings key requested by the client must have a corresponding entry on Dashboard with: schema reference, version, scheduling (start / end / recurrence), filter columns, A/B distribution, audience targeting, default fallback. The `$type` discriminator string in `[JsonDerivedType]` must match the FS key — when the requested key is not registered on Dashboard, the response comes back with `FeatureSettingsResponseStatus.KeyNotFound` (always check `setting.Status == Ok` before accessing `setting.Data`). |
| **Feature Schema** | the Feature Schema referenced by each FS entry above (selected at FS-entry creation on Dashboard); the property layout of every derived `FeatureSettingsData` DTO (`DailyBonusSettings`, `WheelOfFortuneSettings`, game-added classes) | [Game Settings → Feature Schemas](https://dashboard.kinoa.io/game-settings/new-feature-schemas) | Every property the client expects (annotated with `[JsonInclude]` / `[JsonPropertyName]` on the derived DTO) must exist on the matching Feature Schema with the same name and compatible type. Multiple FS keys may share one schema. **Feature Schema key ≠ Feature Settings key** — the schema is the shape; the FS entry is one instance using that shape. The schema key is also used for In-app Feature Configurations in [`modules/06-messaging.md` §Dashboard](06-messaging.md#dashboard) (`InAppFeatureConfiguration.Register<T>(schemaKey)`). |

### Notes
- `FeatureSettingsResponse.Source` reveals where data came from (`Server` / `Cache` / `BuiltIn`) — use it to verify Dashboard reach in QA when expected server-side updates aren't observed.
- Built-in fallback files (`Default Feature Settings.zip` exported from Dashboard, unpacked into `Assets/StreamingAssets/Kinoa/`) must be re-exported any time FS schema versions change — stale built-in files cause version-mismatch warnings at runtime.
- Operator-defined filter values from the Configuration table propagate as `IncludeFilters` (when opt-in) or as the always-present `Filters` (player-state values used for matching) — both bypass any client-side registration step.
- Dynamic integration via `GetInfoAsync(schemaKey)` enumerates Dashboard FS entries sharing a schema without code changes; the schema must still be registered as above.

## Key APIs
- `Kinoa.FeaturesSettings.SmartDownloadAsync<T>(requestParams, cancellationToken, onProgress)` — download with checksum comparison; returns cached/built-in if unchanged (recommended)
- `Kinoa.FeaturesSettings.DownloadAsync<T>(requestParams, cancellationToken, onProgress)` — direct download from server
- `Kinoa.FeaturesSettings.DownloadIfChecksumChangedAsync<T>(requestParams, cancellationToken, onProgress)` — download only settings whose checksum differs from provided value
- `Kinoa.FeaturesSettings.GetBuiltInAsync<T>(requestParams)` — load from StreamingAssets (offline fallback)
- `Kinoa.FeaturesSettings.GetCachedAsync<T>(requestParams)` — load from local cache
- `Kinoa.FeaturesSettings.GetChecksumsAsync(requestParams)` — get server checksums without data
- `Kinoa.FeaturesSettings.GetBuiltInMetadataAsync(requestParams, cancellationToken)` — get built-in metadata (key, version, checksum — no data)
- `Kinoa.FeaturesSettings.GetCachedMetadataAsync(requestParams, cancellationToken)` — get cached metadata
- `Kinoa.FeaturesSettings.ConfigureChecksumLongPollingAsync(settings, callback, cancellationToken, tickDelayMs)` — background polling for checksum changes
- `Kinoa.FeaturesSettings.GetInfoAsync(schemaKey)` — get available Feature Settings info by schema key (for dynamic integration)

## Overview
Feature Settings provides server-configurable game settings (e.g., daily bonus rewards, wheel of fortune prizes) with polymorphic deserialization, versioning, caching, and checksum-based change detection. Settings are organized by schema key and version.

### Polymorphic DTO
Define a polymorphic base class with `[JsonPolymorphic]` and `[JsonDerivedType]` attributes. The `$type` discriminator must match the Feature Settings key defined on Kinoa Dashboard. Concrete settings classes inherit from the base and use `[JsonInclude]` + `[JsonPropertyName]` for property mapping.

### Smart Download Workflow
Smart Download is the recommended method — single network call that combines checksum validation and data retrieval:
1. SDK sends local checksums (from cache/built-in) to server
2. Server compares checksums — returns only changed settings
3. SDK uses server data for changed settings, local data for unchanged
4. Updated settings are auto-cached
5. Old cache is auto-cleaned when schema version changes

### Built-in Feature Settings (Offline Fallback)
Built-in settings are distributed inside the game build for fully offline gaming:
1. Export "Default Feature Settings.zip" from Kinoa Dashboard
2. Unpack into `Assets/StreamingAssets/Kinoa/` (e.g., `DailyBonus.json`, `DailyBonus.metadata.json`)
3. Use `GetBuiltInAsync<T>()` to load from StreamingAssets

## Best Practices
- Use `SmartDownloadAsync` as the primary download method — compares checksums and falls back to cache/built-in AUTOMATICALLY
- Use `FeatureSettingsFailedDownloadStrategy.GetCachedOrBuiltIn` for resilient offline behavior
- Provide a `CancellationToken` with a timeout (e.g., 10 seconds)
- After SmartDownload, data may come from server, cache, or built-in — check `Source` property to know the origin
- Replace settings on client (UI) only when `Status == FeatureSettingsResponseStatus.Ok`
- **Prefer `DownloadIfChecksumChangedAsync` over `ConfigureChecksumLongPollingAsync`** — more controlled: call at defined game moments (e.g., loading screen) to check and download only changed settings. Long polling runs on a timer and may affect performance during gameplay. Do not use both for the same settings
- For long polling: avoid tracking the same settings in multiple requests; cancel old `CancellationTokenSource` before starting a new one
- Use `GetInfoAsync(schemaKey)` for dynamic integration — discover available new Feature Settings based on the same schema without code changes

## Configuration Notes (what's NOT in the sample)
- **SmartDownload (best practice) single-call optimization:** Combines checksum validation + data retrieval in one request. Bandwidth-efficient — only downloads changed settings. Returns all requested settings from all sources (server, cache, built-in) regardless of which were updated.
- **GetDefault behavior:** If `getDefault: true`, the default Configuration is always returned. If `getDefault: false` but the relevant Feature Configuration for the player is not found, the default Configuration is returned anyway.
- **CompressData:** Reduces network traffic but adds decompression time. Only use for large response payloads. Data is stored in compressed form in cache if compressed data was requested.
- **IncludeFilters (opt-in):** `FeatureSettingsDownloadRequestParams.IncludeFilters` — when `true`, response data contains **operator-defined filter values** from the Configuration table (e.g., `"filter: Level:from"` = 0, `"filter: Level:to"` = 10). Use `"filter: "` prefix + `:from`/`:to` suffixes in `[JsonPropertyName]`. See `DailyBonusSettings` sample.
- **Filters (always in response):** `FeatureSettingsResponse.Filters` — contains **actual Player State values** used to match the player against the Configuration table (e.g., `Level` = 5). Different from `IncludeFilters` above.
- **Old cache auto-cleanup:** When SDK detects that the locally cached Feature Schema version is outdated, it automatically removes old cache data.
- **Segmentation properties:** `FeatureSettingsResponse` contains `Audiences` (audience inclusion), `UserLists` (user list inclusion), `AbTestDistribution` (A/B test group assignment) — same pattern as In-app messages.
- **BundleResources:** `FeatureSettingsResponse.BundleResources` (`Dictionary<string, List<Resource>>`) — bundle resources are included directly in the response. No need for a separate `Kinoa.Bundles.GetBundleResourcesAsync` call.
- **Scheduling:** `FeatureSettingsResponse.StartTime` and `EndTime` — Unix timestamps (ms) for scheduled Feature Configuration availability. If recurrence is applied to the FS, these represent the interval of the current recurrence window.
- **Source tracking:** `FeatureSettingsResponse.Source` (`FeaturesSettingsSourceType`) — indicates where data came from: `Server`, `Cache`, or `BuiltIn`.
- **Dynamic integration via GetInfoAsync:** Operators can create new Feature Settings using the same schema without code changes. Returns `SchemaFeaturesSettingsInfo` (IDs, keys, names, versions) to dynamically request settings via download methods.

## Important Notes
- **Polymorphic `$type` must match Feature Settings key.** If `$type` discriminator in `[JsonDerivedType]` does not match the key used in request params, deserialization will produce base type with no data.
- **`[JsonInclude]` is required** for properties with `private set` — without it, properties will not deserialize.
- **SmartDownload returns data even on connection failure** — if `FeatureSettingsFailedDownloadStrategy.GetCachedOrBuiltIn` is set, local data is returned. Check `IsConnectionError()` to detect this case.
- **Feature Settings field access is demonstrated in the sample.** The `KinoaFeaturesSettingsService` sample shows how to process responses, access settings data, filters, segmentation, and bundle resources.
- **Sample DTOs are for demonstration only.** `FeatureSettingsData`, `DailyBonusSettings`, `WheelOfFortuneSettings` are example models — replace with your actual Feature Schema data models. The `$type` discriminator and `[JsonDerivedType]` must match your Feature Settings keys on the Dashboard.

## Common Mistakes
- Missing `[JsonPolymorphic]` and `[JsonDerivedType]` attributes on the base DTO — deserialization will fail silently
- Using a `$type` discriminator value that does not match the Feature Settings key defined on Kinoa Dashboard
- Not using `[JsonInclude]` on properties with `private set` — they will not deserialize
- Not checking `setting.Status == FeatureSettingsResponseStatus.Ok` before accessing `setting.Data`
- Tracking the same Feature Settings in multiple long polling requests; forgetting to cancel old `CancellationTokenSource` before starting a new one
- Not handling `IsConnectionError()` — SmartDownload still returns cache/built-in data even on connection failure
- Not providing built-in files — download "Default Feature Settings.zip" from Kinoa Dashboard and unzip to `Assets/StreamingAssets/Kinoa/`
