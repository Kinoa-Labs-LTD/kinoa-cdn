# Bundles

## Sample File(s)
- `Services/KinoaBundlesService.cs`

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces in `KinoaBundlesService.cs`. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa bundles --merge`.

### Editable surfaces

**Bundle key literals** — sample placeholder values (e.g., `"demoBundle"` or similar in `BundleKey` defaults / request parameters) replaced with the developer's actual bundle keys from Dashboard → Bundles.

Bundle keys follow the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates".

### Frozen (no in-place edits, except where body-extension applies)
- `Kinoa.Bundles.*` SDK call signatures — strict frozen
- `KinoaBundlesService.cs` method bodies (download / cache logic, response processing, resource access patterns) — **body extension allowed** per SKILL.md §"Frozen-scope philosophy" (preserve key moments: SDK call invocation, callback dispatch, response-status check, sample-shipped trace points; do not rewrite wholesale)

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [10 - Bundles](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119276020/10+-+Bundles+latest+version) — full API reference for bundle resources and data models

## Dashboard

### Dashboard dependencies — instance types

Dashboard prerequisites for this module. Closing-summary collection rules: SKILL.md §"Closing summary".

| Instance type | Code reference | Dashboard path | Verification rule |
|---|---|---|---|
| **Bundle** (per key) | every bundle-key literal passed to `Kinoa.Bundles.GetBundleResourcesAsync(bundleKeys, ...)` at `KinoaBundlesService` call sites | [Resources → Bundles](https://dashboard.kinoa.io/resources/bundles) | Each bundle key the client requests must be defined on Dashboard with its resource list (`ResourceKey`, `Amount`, optional `Body` metadata). Empty / missing bundles silently return empty resource dictionaries — `response.Data.BundleResourceBodiesDto` will not contain the key, no error is raised. |

### Notes
- Since SDK 2.9.0, Feature Settings and In-app messages with a Bundle type include `BundleResources` directly in the response — explicit `GetBundleResourcesAsync` calls are needed only when accessing bundles outside those contexts. Bundles delivered through FS / in-app responses are server-resolved against existing Dashboard entries — they cannot reference an unregistered key.
- Bundle resource bodies are opaque to Kinoa — `Body` is operator-controlled metadata. Document any expected shape on the game side.

## Key APIs
- `Kinoa.Bundles.GetBundleResourcesAsync(bundleKeys, cancellationToken)` — fetch resources for one or more bundle keys

## Overview
A **Bundle** is a named collection of resources (e.g., coins, gems, items) with amounts, defined by the operator on the Kinoa Dashboard. Bundles are referenced by key and can be attached to Feature Settings or In-app messages via a "Bundle Key" schema field.

**Since SDK 2.9.0**, Feature Settings and In-app messages with a Bundle type **already include** `BundleResources` in the response. Call `GetBundleResourcesAsync` only when accessing bundles outside of that context.

## Best Practices
- When bundles are part of Feature Settings / In-app messages, access them via `response.BundleResources` instead of a separate API call
- Provide a `CancellationToken` with a reasonable timeout (e.g., 5 seconds)
- Fetch multiple bundles in a single call by passing multiple keys
- Check `response.IsSuccessful()` and that `response.Data?.BundleResourceBodiesDto` is not null/empty before accessing data
- Handle empty bundle responses gracefully — the operator may not have configured resources yet

## Configuration Notes (what's NOT in the sample)
- **Bundle keys** are defined by the operator on the Kinoa Dashboard. The client must know the exact key to request a bundle.
- **Response data:** `response.Data.BundleResourceBodiesDto` is a `Dictionary<string, List<Resource>>` (key = bundle key, value = list of resources). `Resource` fields: `ResourceKey`, `Amount`, `Body` (optional metadata).

## Common Mistakes
- Making separate API calls for bundles already included in Feature Settings / In-app messages responses (SDK 2.9.0+)
- Not providing a cancellation token — requests may hang indefinitely
- Accessing `BundleResourceBodiesDto` without null-checking
- Passing an empty or null bundle keys list
