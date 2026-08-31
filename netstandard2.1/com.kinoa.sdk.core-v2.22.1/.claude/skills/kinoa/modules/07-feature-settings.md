# Feature Settings

## Sample File(s)
- `Services/KinoaFeaturesSettingsService.cs`
- `Data/FeatureSettingsData.cs`
- `Data/DailyBonusSettings.cs`
- `Data/WheelOfFortuneSettings.cs`

All 4 files must be included during integration.

## Integration Notes
- **Always generate both sample schemas** — `DailyBonusSettings` + `WheelOfFortuneSettings` (plus `FeatureSettingsData` base) as the working example. Do NOT ask for a custom schema during first generation — to build the developer's REAL schemas from their data, use the CSV import path in §"Feature Schema source & Dashboard mirroring".
- **Summary note:** instruct the developer to replace `DailyBonus` / `WheelOfFortune` with their actual **Feature Settings Keys** (each FS entry on the Dashboard has its own key; multiple keys can share the same schema), and adjust the derived DTO classes accordingly. Note: Feature Settings Key ≠ Feature Schema Key used in messaging — see 06-messaging module.
- **Always include:** Logging region, Default Parameters region, Local Settings Management region from `KinoaFeaturesSettingsService`.
- **SmartDownloadAsync and all other methods** — import as-is from the sample.
- **Dynamic FS updates (optional):** If the game needs to detect and apply server-side FS changes not only on startup but also during gameplay (e.g., operator published new reward config while player is in a session), ask the developer to choose between:
  - `DownloadIfChecksumChangedAsync` (recommended) — manual, call at defined game moments. **Exclude** Checksums & Long Polling region except `GetChecksumsAsync`.
  - `ConfigureChecksumLongPolling` — background polling. **Exclude** `DownloadIfChecksumChangedAsync`.
  - Both — include all.
  - Neither — **exclude** both `DownloadIfChecksumChangedAsync` and Checksums & Long Polling region. Only include the methods the developer selected.
- **Summary note:** For MVP, only `SmartDownloadAsync` is required. All other methods are optional and depend on your integration scenario.

## Feature Schema source & Dashboard mirroring

Feature Settings mirror to the Dashboard as a **schema** (typed columns) + a **setting** (keyed instance) + a **configuration** (the values). This module builds the *code side* — the `FeatureSettingsData` DTO and the download wiring — so that **Phase 7** (`/kinoa dashboard-sync`, see [`modules/13-dashboard-sync.md`](13-dashboard-sync.md)) can mirror it onto the Dashboard. **This module never calls the Dashboard API** — it produces code + the manifest inventory only; the Dashboard create/publish runs consumer-side in Phase 7.

### Two keys — do NOT conflate
- **Feature Settings key** — the `$type` discriminator on `[JsonDerivedType(typeof(<DTO>), "<key>")]` and the `key:` literal in the download params. This is what the client downloads by and what deserialization matches (see the `FeatureSettingsData.cs` header comment and §"Important Notes").
- **Feature Schema identity** — the Dashboard schema (the typed-column shape) the setting binds to. In code it derives from the **DTO class name with its C# suffix stripped** — drop a trailing `Settings`/`Data`/`Config`/`Configuration`/`Dto` so the code-ism doesn't leak into the Dashboard: `WheelOfFortuneSettings` → schema name **`WheelOfFortune`**. In the common 1:1 case this equals the FS key (`WheelOfFortune`), matching the convention that a setting's key defaults to its schema name; **offer the developer an override** (and for many-keys-share-one-schema, name the schema after the shared shape). **An override needs a code carrier to survive manifest regeneration** — write it as an XML doc comment on the DTO class (`/// FS schema: <Name>`); Phase 7 reads that, never a hand-edited manifest. **Strip-collision:** if two classes strip to the same name (`ShopConfig` + `ShopSettings` → `Shop`), keep the full class names as schema names and tell the developer. **One schema can freely back many Feature Settings keys** — reuse the SAME DTO class, registering it again on the base with a different key:
  ```csharp
  [JsonDerivedType(typeof(WheelOfFortuneSettings), "WheelOfFortune")]
  [JsonDerivedType(typeof(WheelOfFortuneSettings), "WheelOfFortune_Promo")]
  ```
  ### Setting name + key (pinned 2026-08-06 — the resources-style scheme)

On the Dashboard a SETTING carries BOTH identifiers (server DTO `{key, name, description, schemaId}`): `key` — the runtime lookup id (the `$type` discriminator; measured byte-for-byte from code, never recased) and `name` — the human display label (the executor used to paper over with name=key, so dashboards showed raw keys). A SCHEMA has NO display name (historic) — its `name` IS its identity; nothing changes there.
- **Name carrier:** `/// FS name: <text>` XML doc comment on the FS key const when one exists (e.g. `KinoaFeatureSettingsKey.ShopItems`), else at the download-wiring site of the key. Module 13 measures it into the manifest's `settings[].name`; the merge-plan page prefills new-setting names (humanized key — `shop_items` → `Shop Items`) for approval, and the approved name is written back as this carrier when it differs from the key — identity writes NO line (the planner falls back to name=key).
- **Authored-key casing (supersedes the key=schema-name default for NEW keys):** when the skill AUTHORS a new FS key (no code literal exists yet — e.g. a CSV-sourced setting), the key defaults to `SnakeCaseLower` (`shop_items`) and the human form goes to `name` (`Shop Items`) — the same taxonomy as resources and authored event params. Keys already in code are measurements: byte-for-byte whatever their casing (existing `"DailyBonus"` beside a new `shop_items` is a legal mix — the game's own literals win; disclose the mix in a note, never "fix" it).
- **The sync creates, never mutates:** the setting's name is applied at CREATE only — the sync's helpers deliberately wrap no update call (dashboard-side edits are the operator's surface); when an existing setting's live display name differs from the code carrier, the planner emits a visibility WARNING — nothing is mutated (the operator edits on the dashboard if desired).

### Schema identity — convention pinned, casing provisional (2026-08-06)

The schema's single dashboard field is functionally a KEY whatever its UI label: it is unique and the planner reconciles by it byte-for-byte. The dashboard UI CAN rename a schema (corrected 2026-08-06 — an earlier revision claimed no rename API; settings bind by `schemaId`, so a renamed schema shows its new name on them immediately), but the SYNC never renames anything, and a one-sided rename breaks the byte-for-byte match either way: renamed in code only → the planner finds no live schema under the new name and plans a CREATE-duplicate; renamed on the dashboard only → the code's name goes dangling the same way. A schema rename must land on BOTH sides — mirror a dashboard-side rename code-first (class rename or the `/// FS schema:` carrier) before the next sync. Pinned regardless of the UI-label outcome:
- **Derivation for NEW schemas:** the stripped DTO class name (drop trailing `Settings`/`Data`/`Config`/`Configuration`/`Dto`), in **PascalCase as-is** — `ShopOffersSettings` → `ShopOffers`. This is the formalized status quo, and the CSV flow's `--name` takes the same value.
- **Override carrier:** `/// FS schema: <Name>` on the DTO class (unchanged) — the only way to deviate from the derivation.
- **Existing schemas are measurements — byte-for-byte forever** (`DailyBonus` stays `DailyBonus`); a convention mix within one game is legal and disclosed in a page/plan note, never "fixed".

So the producer mirrors **one DTO class = one schema**, with **one-or-more FS keys per schema** (the manifest's `settings[]` may point several keys at the same `schema_name`).

### In-app reuse — closing-summary note (NOT automatic)
The producer does **not** generate any in-app type or registration. If the developer plans to use the same model **directly inside an In-app message body**, surface this once in the closing-summary Dashboard-prerequisites:
> *"To reuse `<Feature>Settings` inside an In-app feature configuration, duplicate it as `InApp<Feature>FeatureConfiguration : InAppFeatureConfiguration` with the same fields, and register it in `KinoaSdkInitService` before `SDK.Initialize()`: `InAppFeatureConfiguration.Register<InApp<Feature>FeatureConfiguration>(schemaName: "<schema name>", schemaVersion: <version>)`."*

Why a separate class: the bases differ (`FeatureSettingsData` vs `InAppFeatureConfiguration`) and in-app deserialization matches `$type` against `{schemaName}_v{schemaVersion}`, not the FS settings key — so a `FeatureSettingsData` subclass cannot be registered via `Register<T>`. (A planned SDK change will unify FS + in-app registration into one registry so the model needn't be duplicated — until then this stays a manual, opt-in note.) See [`modules/06-messaging.md`](06-messaging.md) / [`modules/01-init.md`](01-init.md).

### Schema source — data-import (primary) vs code-scan (aid)
When the developer wants a **real** Feature Settings schema (beyond the shipped samples), open a gate:

- **(a) Import a data source — RECOMMENDED.** The developer supplies the real economy/settings **data** (a flat **CSV**). Building the schema from data is more reliable than inferring it from code, because game-code models are often a fragment of a larger data source. Flow:
  1. Infer: `python "<plugin-root>/skills/kinoa-csv-schema-infer/kinoa_csv_schema_infer.py" infer --csv "<path>" --name "<SchemaName>" --emit full`. **Resolve `<plugin-root>` the same way as the telemetry helper:** glob `~/.claude/plugins/cache/kinoa/kinoa-dashboard/*/skills/kinoa-csv-schema-infer/kinoa_csv_schema_infer.py` and invoke by the fully-resolved literal path (the helper ships in the marketplace plugin Phase 1 installs; if the glob is empty, the plugin isn't installed — run the Phase-1 bootstrap first). Then **present the `review[]` table in chat and gate via `AskUserQuestion`** ("Types look right?" — options: *Accept as inferred* / *Override columns* with the overrides listed via Other, e.g. `sku=bundle_key`); on overrides, **re-run the helper** with the collected `--type col=TYPE` flags (don't hand-patch the output — the `chosen_type`/`note` fields come from the helper).
  2. Map inferred types to the operator's 5 FS column types (table below) — **everything maps** (`object` / arrays / nested → `string`); there is no FS `unsupported_by_cli` bucket.
  3. Generate `<Feature>Settings : FeatureSettingsData` — one `[JsonInclude]` + `[JsonPropertyName]` property per mapped column (C# type per the table) — and register `[JsonDerivedType(typeof(<Feature>Settings), "<FS key>")]` on the base. **`bundle_key` columns need an in-code carrier:** for every column whose final type is `bundle_key`, emit an XML doc comment naming the kind on the property (e.g. `/// Booster bundle SKU. FS kind: bundle_key.`) — the C# type is plain `string`, so without this declaration the Phase-7 manifest regeneration would silently downgrade the column to `string` (module 13 recovers `bundle_key` only from an explicit in-code declaration).
  4. Scaffold **placeholder `IncludeFilters` properties** so the developer can wire config-table filters later: at least **3** — a single-value `filter: <PlayerField>` plus a range pair `filter: <PlayerField>:from` / `filter: <PlayerField>:to` (a configuration table allows **up to 5** filters). Generate them with **nullable** types (`float?` / `string?` — a non-nullable `float` would throw on the `null` these deserialize to until an operator config actually carries filters); the developer later replaces each `<PlayerField>` placeholder with the actual **player-field name** — custom, predefined, or calculated — chosen as a filter on the configuration table. **The placeholders are independent** — a config filter column is either exact-match or a range, never both, so the single-value prop and the range pair will normally end up naming DIFFERENT player fields (the shared `<PlayerField>` token is just scaffold text). Pattern per the `DailyBonusSettings` sample (`[JsonInclude]` + `[JsonPropertyName("filter: <PlayerField>[:from|:to]")]`), nullable types aside. **These props are response-decoding surface only — they are NOT schema columns and never reach the manifest** (filters live at the configuration level; module 13 §Sources excludes `filter: `-prefixed props from `schemas[].fields`).
  5. **Persist the seed data** — copy the developer's CSV verbatim to `kinoa-sdk-dashboard-sync-workspace/<schema name>.csv` at the project root (the existing dashboard-sync workspace; data can be large, so it stays a file, never embedded in the manifest). **Creating the workspace here (before any Phase-7 run) means the gitignore lines may not exist yet — ensure `.gitignore` covers `kinoa-sdk-dashboard-sync-workspace/` now** (same idempotent append as Phase 7 step 1). Phase 7 mirrors these rows into the Dashboard **default** configuration (the operator edits/segments afterward); the consumer reads it at apply, then cleans the workspace — re-syncs don't re-seed (the setting already exists). The CSV header must equal the schema's **data** column names (it does by construction — the schema was inferred from this very CSV). We mirror the values the developer gave — never invent data.
  6. Wire the download (see "Download wiring" below).
- **(b) Scan code for candidates — AID ONLY.** The config/economy discovery probe (see §"Merge Surfaces"). Use it to *see candidates / the full picture*, but treat its output as a starting point, not the authoritative schema — confirm the real shape against a data source before mirroring.

> **CSV only for now.** Flat-JSON and other formats (Firebase exports, spreadsheets, …) are a planned extension; today the data source must be a flat CSV with clear headers and types. No nested objects.

### Type mapping (CSV-inferred → Dashboard FS column type)
The operator can pick from a **closed set of 5** column types in the FS UI; their **API values are lowercase**: `integer`, `number`, `string`, `boolean`, `bundle_key` (UI labels: Integer / Decimal / String / Boolean / BundleKey). The `create-schema` API is looser — a live probe (2026-06-26) accepted all 11 lower-level `SchemaColumnType` values — but that is a **backend gap**: types like `long_string` / `date` / `object` have no operator-UI representation, so a schema using them is unfillable. **Map everything down to the operator's 5; nothing is left unsupported:**

| `kinoa-csv-schema-infer` type | FS column type (API value) | Generated C# property type |
|---|---|---|
| `integer`, `long` | `integer` | `int` / `long` |
| `number` | `number` | `double` |
| `boolean` | `boolean` | `bool` |
| `string`, `long_string`, `date`, `version`, `enumeration`, `object`, arrays/nested | `string` | `string` |
| bundle-key (via `--type …=bundle_key` / in-code declaration) | `bundle_key` | `string` |

`object` / arrays / nested → `string` (the raw JSON ships as a string value, per the operator model). There is **no FS unsupported bucket** — every column maps to one of the 5. **`bundle_key` columns carry TWO constraints on the seeded values:** (1) **format** — a Bundle key must start with a letter and may contain only letters, digits, `_` and `-` (NO dots, spaces, or other punctuation: `booster_speed` ✓, `bundle.booster.speed` ✗); (2) **existence** — the backend validates every seeded value against existing Bundle keys, so the Phase-7 seed import is REJECTED (`422 "[col] is invalid bundle key"`) when a value is malformed OR simply not created yet. **Validate the column's values against the format at the review-table gate** (right when `--type col=bundle_key` is chosen) — a format violation means the developer's data needs fixing NOW, not at sync time. Surface the existence half in the closing summary's Dashboard prerequisites: *"create the Bundles first, or the default config publishes empty and the seed needs a manual re-import."*

### Download wiring — keep ALL FOUR sites in sync
Each feature's `(key, version)` pair appears in **four** places; editing one without the others leaves stale defaults the runtime falls back to:
- `KinoaGameController.DownloadFeatureSettingsAsync()` — the live request;
- `KinoaFeaturesSettingsService.DefaultSmartDownloadParams()`;
- `KinoaFeaturesSettingsService.DefaultDownloadParams()`;
- `KinoaFeaturesSettingsService.DefaultLocalRequestParams()`.

**A NEW schema always wires as `version: 1`** — Phase 7 creates the Dashboard schema at version 1, so that's what the code must request. When adding or renaming a feature, set the SAME `key` (= the `$type` discriminator) and numeric `version` in all four — **and remember the key's fifth home: the `[JsonDerivedType(typeof(<DTO>), "<key>")]` registration on the base** (a key renamed at the request sites but not in the registration deserializes to the bare base with no data). The Phase-7 producer reads these to populate the manifest and **warns on any divergence** across the four sites (a mismatch makes the Dashboard setting disagree with the runtime fallback).

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the editable surfaces below across `KinoaFeaturesSettingsService.cs`, `FeatureSettingsData.cs` (and its derived DTOs), and `KinoaGameController.DownloadFeatureSettingsAsync()`. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa feature-settings --merge`.

### Editable surfaces

#### Sample Feature Settings DTOs (`DailyBonusSettings`, `WheelOfFortuneSettings`, `FeatureSettingsData` polymorphic base)

Renaming, re-typing properties, replacing `$type` discriminators to match Dashboard Feature Settings keys; full class replacement when the Dashboard shape diverges from the sample. **When replacing/removing the sample DTOs, also adjust the `KinoaFeaturesSettingsService` Logging region** — `LogSettingsData` hard-casts to `DailyBonusSettings`/`WheelOfFortuneSettings`, so a wholesale DTO replacement without touching those casts breaks the compile (the casts are an editable surface alongside the DTOs).

**Adding new DTOs** derived from `FeatureSettingsData` (e.g., `LevelRewardSettings`, `StarterPackSettings`, etc.) to match additional **Dashboard Feature Settings** entries — welcomed and in-scope. Register each new derived type via the appropriate `[JsonDerivedType]` attribute on the base class.

**Gate surface — the merge-plan page when available** (SKILL.md Phase 6 §"Merge-plan page"): the probe's candidates render there as editable **schema rows** (name + columns) and **setting rows** (key → schema binding via dropdown; existing DTOs/keys read-only), and the page's confirmed `feature_settings` section is the candidate choice — the CSV data-import path below remains the recommended source for the REAL schema shape (the page authors keys/candidates; the CSV review table still validates inferred column types against data).

**Game config / economy discovery probe — mandatory pre-walk for this module.** This probe is a **discovery aid** (surface candidates / the full picture), NOT the authoritative schema source — prefer the CSV data-import path (§"Feature Schema source & Dashboard mirroring") to build the real schema from data, since code models are often a fragment of a larger data source. **When the developer has already designated a data source** (named a CSV / asked for a specific feature), do NOT open per-candidate Modify gates — run the scan, list the top candidates once in the closing summary as "Skipped — re-run `/kinoa feature-settings --merge` to mirror later", and proceed with the designated source (per-candidate gating is for the undirected walk, where discovery IS the point). The sample DTOs (`DailyBonusSettings`, `WheelOfFortuneSettings`) are placeholders for game-specific schemas. Without this probe, the integration ships with sample-default DTOs that don't reflect the game's actual tunable surface, and the dev has to discover the FS-mirroring opportunity manually. Scan the game's existing config / data layer (standard skill scope — entire project minus Kinoa target base + Library/Packages/KinoaPackages — game configs typically live under `Assets/Scripts/<feature>/`, `Assets/Scripts/Design/`, `Assets/Scripts/Configs/`, or feature-specific `*/Model/` folders). Probe patterns:
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

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary". **Phase 7 (`/kinoa dashboard-sync`, [`modules/13-dashboard-sync.md`](13-dashboard-sync.md)) now automates creating the schema + setting (+ a published empty default config) from the code — see §"Feature Schema source & Dashboard mirroring".** The rows below remain the manual fallback (sync skipped / unavailable) and the post-sync verification rules; scheduling / filters / A/B / audiences / data values stay operator-owned either way.

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **Feature Settings entry** (per key) | every `key` literal inside `FeatureSettingsSmartDownloadRequestParams(...)` / `FeatureSettingsDownloadRequestParams(...)` constructors at `KinoaGameController.DownloadFeatureSettingsAsync()` and any other call site; every `[JsonDerivedType(typeof(<DTO>), "<discriminator>")]` attribute on the polymorphic `FeatureSettingsData` base | [Feature Settings](https://dashboard.kinoa.io/new-feature-settings) | Each Feature Settings key requested by the client must have a corresponding entry on Dashboard with: schema reference, version, scheduling (start / end / recurrence), filter columns, A/B distribution, audience targeting, default fallback. The `$type` discriminator string in `[JsonDerivedType]` must match the FS key — when the requested key is not registered on Dashboard, the response comes back with `FeatureSettingsResponseStatus.KeyNotFound` (always check `setting.Status == Ok` before accessing `setting.Data`). |
| **Feature Schema** | the Feature Schema referenced by each FS entry above (selected at FS-entry creation on Dashboard); the property layout of every derived `FeatureSettingsData` DTO (`DailyBonusSettings`, `WheelOfFortuneSettings`, game-added classes) | [Game Settings → Feature Schemas](https://dashboard.kinoa.io/game-settings/new-feature-schemas) | Every property the client expects (annotated with `[JsonInclude]` / `[JsonPropertyName]` on the derived DTO) must exist on the matching Feature Schema with the same name and compatible type. Multiple FS keys may share one schema. **Feature Schema key ≠ Feature Settings key** — the schema is the shape; the FS entry is one instance using that shape. The schema key is also used for In-app Feature Configurations in [`modules/06-messaging.md` §Dashboard](06-messaging.md#dashboard) — if the schema is reused inside an In-app body (opt-in, see §"In-app reuse"), register the In-app type via the version-specific `InAppFeatureConfiguration.Register<T>(schemaName, schemaVersion)` (matches `$type` = `{schemaName}_v{schemaVersion}`); the single-arg `Register<T>(schemaName)` is the version-agnostic fallback. |

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
