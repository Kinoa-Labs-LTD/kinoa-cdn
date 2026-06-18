# Translations

## Sample File(s)
- `Services/KinoaTranslationsService.cs`

## Integration Notes
- **Use sample default groups and language** (`English` + groups `""`, `"ui"`, `"store"`) as-is. Do NOT ask for custom groups/languages in wizard mode. **Custom language / group customization is exclusively a `--merge` workflow** — see §"Merge Surfaces" (mandatory Modify gate for `Language.*` value, Dashboard-context 3-way for non-empty group keys). If the developer raises custom values mid-wizard, defer them to `/kinoa translations --merge`.
- **Summary note:** instruct the developer to replace the default groups and language with their actual values from the Kinoa Dashboard (Localization → Translations).
- **Always include:** Local Collection region and Logging region from `KinoaTranslationsService`.
- **Import all 3 download methods by default** (`SmartDownloadAsync`, `DownloadAsync`, `GetCachedAsync`) — as-is from the sample.
- **Summary note (methods):** For MVP, only `SmartDownloadAsync` is required — it handles incremental merge, cache reuse, and lazy loading. `DownloadAsync` and `GetCachedAsync` are optional and depend on your integration scenario (full cache reset / offline-first startup).

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces in the generated Kinoa base. All other code in `KinoaTranslationsService.cs` and Translations-related call sites stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa translations --merge`.

### Editable surfaces

**`KinoaTranslationsService` request params** (`SmartDownloadAsync` / `DownloadAsync` / `GetCachedAsync` arguments + `DefaultRequestParams()` body):

- **Language value** (sample: `Language.English`) — **critical request parameter.** Open a mandatory Modify gate at every occurrence; never silently retain the sample default. Three options:
  - **Apply specific value** — name the `Language.*` enum entry to substitute.
  - **Apply dynamic mapping** — point to the game's language source (e.g., `GameSettings.CurrentLanguage`, `LocalizationManager.CurrentLocale`); the skill wires an expression (possibly via a helper method if enum mapping is needed).
  - **Skip** — leave sample default `Language.English` as-is per generic Skip semantics in SKILL.md §"Dashboard-context gap at Modify gates". Surface in closing-summary Unresolved AND flag the runtime impact (see below).
  
  Silent retention without confirmation ships a broken request for any non-English game.
  
  **Runtime-impact note when language is Skip'd:** the sample default `Language.English` is not just a placeholder — it's a runtime value. `KinoaTranslationsService.SmartDownloadAsync()` and `KinoaGameController.DownloadTranslationsAsync()` will **execute at startup with `Language.English`** even after Skip — for non-English games this means real translation requests fail or return wrong-language content. The TODO marker preserves discoverability in `git diff`, but does NOT prevent the request from firing in production. Surface this explicitly in the closing-summary Unresolved entry with wording: *"Translation language Skip preserves sample default `Language.English` at runtime — non-English games WILL ship with English requests until resolved. Either re-run `/kinoa translations --merge` with a Real value, or comment out the `KinoaGameController.DownloadTranslationsAsync()` call site (per `§Frozen-scope philosophy` comment-out permission) until the language is configured."*

- **Translation group keys** — default `""` (empty string) is a legitimate value when no Dashboard-side grouping is configured. Do NOT force a Modify gate for `""` sites. For sample sites with **non-empty** placeholder groups (`"ui"`, `"store"`, etc.), apply the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates".

**`KinoaGameController.DownloadTranslationsAsync()`** (if generated):
- Same language and group-key surfaces as above — share the placeholder/value with the service-side default if both reference the same Dashboard concept.

### Frozen (no in-place edits)
- `LocalTranslations` collection management
- Download strategy logic (`SmartDownloadAsync` / `DownloadAsync` / `GetCachedAsync` body)
- Cache behavior, caching policy, cancellation token defaults
- Response processing (`ReplaceTranslations`, `LogTranslations`)
- Helper methods (`EnsureCancellationToken`, etc.)
- Method signatures of all `Kinoa.Translations.*` calls

### Cross-module dependencies

Load these modules into context when working on surfaces of this module:
- [`modules/12-controller.md` §"Merge Surfaces"](12-controller.md#merge-surfaces) — `KinoaGameController.DownloadTranslationsAsync()` is the call site that issues Translation requests. Language and group-key edits propagate to the controller's call site (parallel concerns — both occurrences must agree).

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [12 - Translations](https://kinoa.atlassian.net/wiki/spaces/KW/pages/749535234/12+-+Translations+latest+version) — full API reference for download strategies, cache behavior, and data models

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **Translation Language** | the `Language.*` enum value passed to `SmartDownloadAsync` / `DownloadAsync` / `GetCachedAsync` request params; every dynamic mapping point (e.g., `GameSettings.CurrentLanguage` → `Language.*`) wired at the Modify gate; also `KinoaGameController.DownloadTranslationsAsync()` call site | [Game Settings → Localization](https://dashboard.kinoa.io/game-settings/localization) | Each `Language` value the client requests must be configured on Dashboard with its language-code mapping. Missing languages return empty translation sets (status: not OK at the language level) and the per-language cache file `{language}.translations.json` is deleted. |
| **Translation Group** (per key) | every group-key literal in request-params group lists (sample defaults: `""`, `"ui"`, `"store"`); every dynamic group reference at lazy-load call sites | [Game Settings → Localization](https://dashboard.kinoa.io/game-settings/localization) | Group keys partition translations by screen / feature for lazy loading. Each non-empty group key requested must be defined on the Dashboard for the matching language. The default key `""` (empty string) is legitimate — Dashboard rows with no group fall under it. |
| **Translation Row** | rows referenced by code as `LocalTranslations[<row-key>]` (or equivalent local-collection lookup) at any UI / business-logic call site that consumes localized strings | [Game Settings → Localization](https://dashboard.kinoa.io/game-settings/localization) | Each translation row key the game looks up must exist on Dashboard for the requested language and group. Missing rows return empty / null at lookup time — handle gracefully on the game side or surface as a content-team task. |

### Notes
- Language Skip during `--merge` preserves the sample default `Language.English` at runtime — non-English games WILL ship with English requests until the language is configured (see §"Merge Surfaces" runtime-impact note).
- Group-level removal on Dashboard removes the group from cache on next `SmartDownloadAsync`; full language removal deletes `{language}.translations.json`.
- `SmartDownloadAsync` preserves non-requested groups in cache; `DownloadAsync` removes them — pick deliberately.
- Translation rows are the lowest-level instance; they're not surfaced separately in a registration flow (registration happens implicitly by adding rows on Dashboard) but each lookup at runtime depends on Dashboard reach.

## Key APIs
- `Kinoa.Translations.SmartDownloadAsync(requestParams, cancellationToken, onProgress)` — incremental merge; downloads only changed/missing groups (recommended)
- `Kinoa.Translations.DownloadAsync(requestParams, cancellationToken, onProgress)` — full overwrite; re-downloads requested groups and replaces cache
- `Kinoa.Translations.GetCachedAsync(requestParams, cancellationToken)` — cache-only, no network

## Overview
Translations provides server-configurable localized text organized by **language** and **translation groups**. Groups partition translations (e.g., by screen or feature) for lazy loading.

**Cache model: per-language JSON file `{language}.translations.json`, managed automatically by the SDK in sync with the server.

### Smart Download vs Download
- **SmartDownloadAsync (recommended)** — incremental merge. Sends local checksums; the server returns **only** changed/missing groups, while unchanged groups are served from the local cache — reducing network traffic. Groups not in the request are **preserved** in cache. Supports lazy/on-demand group loading.
- **DownloadAsync** — full overwrite. Re-downloads requested groups; groups not in the request are **removed** from cache. Use for full sync/reset.
- **GetCachedAsync** — no network; returns only cached data.

In all methods: groups removed on the server are removed from cache; if a language is removed, `{language}.translations.json` is deleted.

## Best Practices
- Use `SmartDownloadAsync` as the primary method — minimizes bandwidth and preserves non-requested groups
- Use `DownloadAsync` only when a full sync/reset is explicitly required
- Use `GetCachedAsync` for offline/startup scenarios requiring instant results
- Provide a `CancellationToken` with a reasonable timeout (e.g., 10 seconds)
- Organize translations into groups and lazy-load per game screen (e.g., load `shop` group on shop screen)
- Filter by `TranslationResponseStatus.Ok` at both language and group levels before using data
- For SmartDownload, process response data **before** checking response status — partial data may be available on partial failure
- Check `group.Value.Source` to know whether data came from server or cache
- Use `onProgress` for large translation sets

## Configuration Notes (what's NOT in the sample)
- **Default group key** is `""` (empty string) — Dashboard rows with no group (empty *Group* column)
- **Cache lifecycle is automatic** — SDK syncs with server; removed groups/languages are auto-cleaned
- **Dashboard location:** Localization → Translations

## Important Notes
- **SmartDownload preserves non-requested groups**; Download removes them. Use Download deliberately.
- **Language removal on the server deletes the entire `{language}.translations.json`** — applies to both methods.
- **GetCachedAsync returns empty before any download** — cache is populated only after a successful Smart/Download call.
- **Sample request params are for demonstration only.** Replace `Language.English` and group keys `""`, `"ui"`, `"store"` with your actual values.

## Common Mistakes
- Using `DownloadAsync` when `SmartDownloadAsync` would suffice — forces full re-download, removes non-requested groups
- Not filtering by `TranslationResponseStatus.Ok` at both language and group levels
- Not handling null/empty `response.Data.Translations`
- Using `GetCachedAsync` on first run before the cache is populated
- Not providing a cancellation token — risks indefinite waits
- Requesting all groups upfront instead of lazy-loading — defeats the purpose of groups
