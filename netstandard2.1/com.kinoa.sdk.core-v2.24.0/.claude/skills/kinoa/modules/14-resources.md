# Resources (game items catalogue → Dashboard resource templates)

Builds and maintains the game's **resources catalogue** — the sellable / awardable items (weapons, boosters, chests, cosmetics, event rewards, IAP goods) that mirror onto the Kinoa Dashboard as **resource templates** (lifecycle `DRAFT → ACTIVE → DEPRECATED`). This module owns the *code side only*: discovery in game code, the developer confirmation gate, and the durable catalogue class `KinoaResources.cs`. The Dashboard create/activate runs **consumer-side in Phase 7** (`/kinoa dashboard-sync` → the plugin's `kinoa-sdk-dashboard-sync` skill, manifest `schema_version: 3`); this module **never calls the Dashboard admin API**.

Entered via **`/kinoa resources --merge`** — the flow is inherently merge-shaped: re-scan → gate existing + new → rebuild the catalogue — **and as the final mirrorable-surface step of a full `/kinoa --merge` walk** (after modules 02/04/07 — so clients discover resources without knowing the standalone command; the same flow, fired by the walk). **Plain `/kinoa resources` (no `--merge`) does NOT enter this flow**: it is module install only — ensure `Data/KinoaResources.cs` exists (create the empty sample stub + stable `.meta` GUID if missing), then point to `--merge` for discovery. Mirrors the player-fields discipline (module 02: custom fields are exclusively a `--merge` workflow) and keeps a full `/kinoa` run from building the resources page twice (once at module install, again at the merge walk). Excluded from `all` generation and `--auto` — the confirmation gate is interactive by design (a resources catalogue registered without human review registers junk items on the Dashboard). The run closes with the Dashboard Sync gate offering the **scoped** sync (`/kinoa dashboard-sync resources` — only the resources surface is measured and synced; see module 13 §"Scoped sync runs").

## Sample File(s)

- `Data/KinoaResources.cs` — TODO-scaffolded empty catalogue class, **copied as-is at Phase 4 step 0** (Utilities + Constants — always) together with `KinoaGameEventConstants.cs`, so every integration carries the carrier file from day one. Ships with the doc-comment grammar in comments; holds zero resources until the `/kinoa resources` gate fills it. The module's only write target is that copied file: `<Kinoa target base>/Data/KinoaResources.cs`.

## What a "resource" is (and is not)

A **resource** is any item that can be **sold or awarded as a prize**. It registers on the Dashboard as a resource template: `name`, `key` (the `resourceKey`, `^[a-zA-Z][a-zA-Z0-9_-]*$`), optional `description`, and typed `fields`. A field is **minimally `{name, field_type}`** — `field_type ∈ number|string|boolean|date|enumeration`; `default`, `description`, and (for enumeration) `enumeration_values` are per-field extras (all fully optional). The field names are the keys the resource's JSON (`body`) is composed of. **Soft currency and consumables (coins, lives, energy) ARE resources when the game sells or awards them** (user decision 2026-08-03): the catalogue registers the awardable ITEM (`coin` — what a bundle/prize grants as key + amount), while the player's BALANCE stays a player field (`wallet.gold`, module 02) — the two coexist by design. ALWAYS carry currency candidates to the confirmation page, flagged in `description` (*"currency — the balance itself lives in player state; keep if bundles/prizes award it"*); never silently drop them — the developer decides at the gate.

### Template shapes — recognition rules

Two valid shapes, both first-class end-to-end (manifest → planner → helper):

1. **Key-only template** (`fields: []`) — the resource is granted/sold as **key + amount**: the template is just the identity (key + human name), and the amount accompanies the key at the usage site (bundle item / prize). Recognized from bare id enums, SKU string lists, name tables with no per-item attributes. Emit it with empty `fields` — do NOT invent fields to "enrich" it.
2. **Typed-fields template** — the resource is **key + data fields**, and the field keys form the resource's JSON. Each field: key (name), type, optional `default`, optional `description` (+ `enumeration_values` for enums). Infer one field per attribute via the type mapping below; the developer retypes/drops/adds on the confirmation page.

| Attribute in code/data | `field_type` |
|---|---|
| `int`, `long`, `float`, `double`, `decimal` (and nullables) | `number` (resources have ONE numeric type — no integer/decimal split like FS) |
| `bool` | `boolean` |
| `string` (free-form) | `string` |
| `DateTime` / date-like values | `date` |
| C# `enum`, or a property/column whose observed values form a small closed set (`common/rare/epic`) | `enumeration` + the values as `enumeration_values` |
| collections / nested objects / dictionaries | no resource field type exists — propose `string` (raw JSON ships as a string value) and say so in the ROW's `note` (field descriptions are dashboard data, not analysis carriers) so the developer decides at the gate; never emit a type outside the closed 5 |

**Quantity is not a field.** An amount/count attached to an item at its usage site (`reward: chest x3`, price tables, stack sizes in a grant) describes the *grant/sale instance*, not the item type — the Dashboard models counts on the bundle/prize side. Do not lift `amount`/`count`/`quantity` columns into the template's fields; a count that is genuinely an attribute of the item itself (e.g. `capacity` of a chest) is a normal `number` field. When ambiguous, propose it flagged in the row's `note` and let the gate decide.

**Bundle / store-commerce boundary (pinned 2026-08-05 — a demo-d run proposed `promo_offer` + `pricelist_product`, and its earlier catalogue had accumulated `boost_pack`/`continues_pack`/`exchange_pack`):** a resource template describes ONE player-ownable item. Two recurring class shapes are NOT resources and are never proposed:
1. **Bundle-shape** — a container granting OTHER resources with quantities: `rewardTypes` + `rewardQuantities` arrays, `ProductPack`/`IRewardPack` ancestry, a `cost` + quantity-of-another-item pair (`BoostPack`, `ContinuesPack`, `PromoOffer`). That is Kinoa's **Bundles** entity — NOT supported by this integration iteration; proposing it as a resource template mis-models it and collides with the future Bundles support.
2. **Store-commerce shape** — shop slots / pricelist products / SKU placements: rows referencing an offer or a store package id (android/iOS product ids live at this level) and carrying placement/targeting metadata — `position`, `promoOfferId`, payer targeting, min/max level (`PricelistProduct`, the slot half of `ExchangePack`). Store plumbing whose PURCHASE grants resources — not an ownable item at all.

Both kinds go to the run's **Discovered-but-not-proposed table** (module 02's ledger mechanic, resources edition) with an explicit reason — *"bundle candidate — Bundles support arrives in a future iteration"* / *"store-commerce metadata, not a player-ownable item"* — so nothing is silently lost and the future Bundles iteration inherits a ready candidate list. The shop/IAP catalogue SOURCES below stay discovery inputs: sweep them for the ITEMS they sell/grant, never to propose the pack/slot itself.

**Template `body` is formed FROM the fields — never hand-authored.** The template DTO's `body` is not an independent map: it is the resource's JSON, composed of the template's `fields` (the fields define its shape and values). The SDK flow therefore emits **`fields` only** — the manifest carries them, the sync creates with `--fields-json`, and the HELPER composes the body from the fields as `{"<name>": "${<name>}"}` placeholders (the server stores `body` verbatim and does not derive it from the fields). Never pass `--body` from the sync flow (the helper's `--body` passthrough exists for operator CLI sessions).

## Flow (`/kinoa resources --merge`)

Telemetry (best-effort, standard contract — a failed post never aborts; fires only for the `--merge` catalogue run — a plain `/kinoa resources` stub-ensure posts nothing): resolve the plugin telemetry helper by glob `~/.claude/plugins/cache/kinoa/kinoa-dashboard/*/skills/kinoa-sdk-dashboard-sync/kinoa_webhook.py` and fire `phase-start --phase "Resources — catalogue gate"` as the run's first action, a `qa` post after the gate answer, and `phase-end --summary "confirmed=N added=A updated=U removed=R"` at the end. Helper missing → skip telemetry silently, continue.

### 1. Read the existing catalogue

`Glob` for `KinoaResources.cs` under the Kinoa target base (`Assets/Scripts/Kinoa/**` or the detected base) — Phase 4 step 0 copies the empty scaffold there on every generation, so an integrated project normally has it. If present, parse it per §"KinoaResources.cs format" — these are the **already-confirmed** entries (the fresh scaffold parses to zero). If absent (pre-resources integration), copy the sample `Data/KinoaResources.cs` from the Samples root now (same cp-as-is rule as step 0). A parse error (malformed `field:` spec, duplicate key) is surfaced to the developer with the offending line — never silently dropped or "fixed".

### 2. Discovery scan — code is the primary source

The client's items live in their code/data, not in a CSV. Use `Glob` + `Grep`, broadly — every game names these differently:

- **Shop / store / IAP catalogues** — files/classes named `shop`, `store`, `iap`, `product`, `catalog`, `offer`, `pack`, `bundle`; product id lists, price tables, SKU definitions. Swept for the ITEMS they sell/grant — the pack/slot/offer itself is excluded (§Bundle / store-commerce boundary).
- **Reward / prize tables** — `reward`, `prize`, `loot`, `drop`, `chest`, `crate`, `gift`, `daily`, `battlepass`, `season`; loot tables, quest/achievement payouts.
- **Item definitions** — `item`, `equipment`, `gear`, `weapon`, `skin`, `cosmetic`; ScriptableObjects, enums of item ids, data assets (JSON/CSV/`.asset`) listing items and attributes.

Per candidate capture: human **name**, proposed **key** (slug of the id/name, must match the key pattern), short **description**, **source** (`path:line` — provenance the developer can verify), and inferred **fields** from the item's attributes (`attack: int` → `number`, `rarity` enum → `enumeration` with the member values, `tradable: bool` → `boolean`). Candidates whose key already exists in `KinoaResources.cs` are NOT re-proposed as new — the existing entry carries them.

**Item definitions are typed candidates — enums alone are half the sweep (pinned 2026-08-05 — a demo-d run mined `RewardType`/`CollectableType` and shipped an all-key-only catalogue, missing `Boost.cs`):** after the reward/collectable enums, sweep the item-DEFINITION classes (`Model/Game`-style: `Boost`, item/equipment/skin definitions) — a definition class carrying per-item attributes (a type enum, unlock level, flags) is a first-class TYPED-fields candidate, and it is exactly what the fields mechanism exists for. When the same item surfaces both as a key-only enum member (`RewardType.JOKER`) and inside a typed definition (`Boost` with `boost_type = JOKER`), propose BOTH rows with cross-referencing notes (*"also modelled as `boost` — merge or keep separate"*) — merging is the developer's page decision (select-first), never the producer's silent call.

**Description vs note (pinned 2026-08-05 — live pages shipped 200+-char analysis blobs in the description input):** `description` is the DASHBOARD description — pre-filled, one sentence, WITHIN the 100-char server cap the page enforces (an over-limit pre-fill renders red and forces the developer to hand-trim; never ship one). Everything else is a decision input, not entity data — LOW-CONFIDENCE flags, keep-if/drop-if advice, typing rationale (*"typed string because the enum members were not enumerated"*), same-item-modelled-twice observations — and goes in the row's `note` key, which the page renders as muted text beside the row (the same mechanic events and player-fields rows use). One row, two audiences: the input holds what the dashboard will store, the note holds what the developer needs to decide.

**Cross-references go by KEY, never by row number (pinned 2026-08-06 — a demo-d run's note pointed at "row #17" while the twin row shipped as id 18):** a note referencing another candidate names its `key` (*"also modelled as `boost` — merge or keep separate"*, *"inventory side of `lives_nolimit`"*), never a page row number or payload id. Numbers are authoring bookkeeping — they shift when candidates are added or dropped between numbering and hand-back, so a numeric reference rots before the developer reads it; keys are stable and searchable on the page.

**Notes are decision inputs ONLY — no narration (pinned 2026-08-06, the resources edition of the events-page row-notes rule; a demo-d `boost` note ran five sentences of which two were decisions):** every sentence in a note must change what the developer decides — a retype offer with the member list, a merge-or-keep cross-reference, a keep-if/drop-if call, a bundle-vs-item dilemma. Producer narration does NOT ship: "TYPED row measured from …" (visible from the row itself), mapping mechanics ("`X.name` is carried as the template name, not as a field"), restating a field's own description, or how the run derived anything. If removing the sentence changes no decision, remove the sentence.

**Sentinel members are excluded from observed-values lists (pinned 2026-08-06 — a run listed `NONE` among retype candidates):** `NONE`/`Default`-class sentinels are not dashboard enum members — leave them out of the offered list with a one-word flag (*"NONE is a sentinel, excluded"*), never as a candidate value.

**Do NOT pre-gate discovery in the terminal.** The confirmation page (step 3) IS the review step — carry all candidates (even low-confidence ones, flagged in `note` — never in `description`, see the description-vs-note pin above) straight to it. The only stop-and-ask: discovery found **nothing at all** AND the catalogue is empty — then `AskUserQuestion`: point me at the item/shop/reward definitions, or confirm the game has no resources (skips the module).

### 3. Confirmation gate — the human-in-the-loop step

The gate renders on the **merge-plan page** (SKILL.md Phase 6 §"Merge-plan page") with ONLY the `resources` key present in the payload — the page shows just the Resources card (no other sections, no other add-buttons) — the same page the merge walk fires per module; `/kinoa resources` is simply its resources-only invocation. Assemble the payload with `generated_at` = this run's UTC timestamp, `game_id`, and `resources` rows (unique non-null `id` each): existing entries from `KinoaResources.cs` with `"existing": true` (here meaning *already in the confirmed catalogue*, not "on the Dashboard" — the page renders them **read-only**; editing an already-confirmed entry is code-first: hand-edit the class, the next run measures it) followed by new candidates (`"existing": false`, fully editable):

```json
{
  "payload_version": 1,
  "generated_at": "<ISO 8601 UTC>",
  "game_id":      "<GameID literal or null>",
  "resources": [
    {"id": 1, "name", "key", "description", "note"?, "source", "existing": true|false,
     "fields": [{"name", "field_type", "default"?, "enumeration_values"?, "description"?}]}
  ]
}
```

Resolve the generator by glob `~/.claude/plugins/cache/kinoa/kinoa-dashboard/*/skills/kinoa-sdk-dashboard-sync/generate_merge_plan_page.py` and invoke by the fully-resolved literal path:

```bash
echo '<payload-json>' | python "<resolved path>" \
    --output ./kinoa-merge-plan-<YYYYMMDD-HHMMSS>.html
```

The page is self-contained, auto-opens in the browser (`opened_in_browser: false` → surface the absolute path), and lets the developer **rename, retype, drop, and add** new resources and their fields with live key validation (the resource-key regex + duplicates, vocabularies parity-tested against the sync planner). The browser sandbox can't write to the filesystem — ask via `AskUserQuestion` how the confirmed plan comes back: **Downloaded file** (`~/Downloads/kinoa-merge-plan-confirmed-<page timestamp>.json` — `Read` it) or **Copied JSON** (pasted into chat — parse directly). The hand-back's `resources` section uses the same field names as the manifest (`key`, not `resourceKey` — no mapping needed). Suggest `.gitignore`-ing `kinoa-merge-plan-*.html` / `kinoa-merge-plan-confirmed-*.json` (local artifacts; the durable output is `KinoaResources.cs`).

**Freshness gate:** the hand-back's `page_generated_at` must equal the `generated_at` this run stamped into the payload. Mismatch (or missing `page_generated_at` alongside an old `confirmed_at`) = an earlier run's export sitting in `~/Downloads` — don't apply it; say which run it came from and ask for a re-export from the currently open page.

**Fallback — chat gate** (plugin not installed, or headless with no hand-back path): render the candidates as a markdown table in chat (`#`, key, name, fields summary `name:type[*]`, source) and gate via `AskUserQuestion` — **Accept all** / **Edit** (developer lists changes in Other, e.g. `drop 3; rename 2 key=booster_speed; 4 rarity=enumeration(common,rare,epic)`) / **Abort**. Apply edits, re-render, re-gate until accepted. The fallback exists so a missing plugin never blocks the catalogue — but when the plugin is available, the page is the gate; don't substitute the chat table for it. **The fallback is never silent:** when it's taken because the plugin is missing, say so in one line before the table — *"the `kinoa-dashboard` plugin isn't installed — continuing with the chat gate; for the interactive editing page, install it (`claude plugin marketplace add Kinoa-Labs-LTD/integration-skills` → `claude plugin install kinoa-dashboard@kinoa`) and re-run `/kinoa resources --merge`"* — no stop, no extra question, just visibility.

### 4. Validate the confirmed list — the review-gate contract

The confirmed list is canonical (only these entries enter the catalogue, exactly as edited) — but it is validated, never trusted blind:

- every `key` matches `^[a-zA-Z][a-zA-Z0-9_-]*$` (the server enforces the same pattern — catching it here turns an opaque 4xx at sync time into a fix now);
- keys are unique **byte-for-byte**, and case-variant near-duplicates (`Sword` vs `sword`) are flagged as errors too — the Dashboard would hold two near-identical templates;
- **names are unique within the catalogue too** — the server enforces template-NAME uniqueness across ALL statuses, DEPRECATED included (live-verified 2026-07-23: `422 "[name] name already exists"`); two catalogue entries sharing a name means the second create fails at sync. Collisions with LIVE templates are the consumer's job to warn about (the producer has no dashboard listing) — but a rename-recovery for a deprecated key must change **both the key and the display name** (the retired record still holds both);
- every `field_type` ∈ `number|string|boolean|date|enumeration` (closed set — matches the sync helper's vocabulary);
- `enumeration` fields carry non-empty `enumeration_values`;
- field names are non-empty and unique within their resource.

Violations → list them and ask the developer to fix (re-open the page or correct inline). **Never silently munge** — no auto-slugging, no recasing, no dropping of offending entries without the developer seeing it.

### 5. Write `KinoaResources.cs` — the durable carrier

Full `Write` of the whole file from the confirmed list (single writer, always rebuilt — same discipline as the manifest). Location: `<Kinoa target base>/Data/KinoaResources.cs`, namespace matching the sibling data classes (e.g. `CustomPlayerState.cs`). This file is **source, not an artifact** — it is NOT gitignored; commit it like any generated-then-owned code.

Close the run with the **Dashboard Sync gate** (SKILL.md append-protocol step 5 — the standardized `AskUserQuestion`: *"Start Phase 7 — Dashboard Sync now?"* → **Start now (Recommended)** / **Later**, with the verbatim creates-instances explanation): the catalogue reaches the Dashboard only via Phase 7, and *Start now* enters it immediately in this session; *Later* → the developer runs `/kinoa dashboard-sync` themselves when ready. Also note: removing an entry from the catalogue only removes it from the manifest — the sync **never deletes** Dashboard entities (a removed key becomes `dashboard_only`, operator-owned).

## KinoaResources.cs format

Pure data class — key constants + field metadata in structured doc comments (the same in-code-declaration carrier pattern as FS `/// FS schema:` / `/// FS kind: bundle_key`). **No API calls, no tokens, no `Authorization` headers, no `gate.kinoa.io`/`dashboard.kinoa.io` references — ever** (admin registration is exclusively the Phase-7 consumer's job; the runtime bundles surface is a separate, game-token API this class does not touch).

```csharp
// Kinoa resources catalogue — generated by /kinoa resources --merge (confirmation-gated).
// One const per resource: the string literal IS the resourceKey (byte-for-byte).
// Phase 7 (/kinoa dashboard-sync) reads THIS class to build the manifest's
// resources[] section. Edit by hand freely — /kinoa resources --merge re-gates edits;
// Phase 7 validates keys/types at its preflight. Never add API calls here.
public static class KinoaResources
{
    /// resource-name: Legendary Sword
    /// resource-description: Awarded for beating the final boss.
    /// field: attack:number:default=100
    /// field: rarity:enumeration:values=common,rare,epic
    /// field: tradable:boolean
    public const string LegendarySword = "legendary_sword";

    /// Field keys and enumeration values of LegendarySword — generated together with
    /// the doc-block above; use these instead of hand-written literals.
    public static class LegendarySwordFields
    {
        public const string Attack = "attack";
        public const string Rarity = "rarity";
        public const string Tradable = "tradable";

        public static class RarityValues
        {
            public const string Common = "common";
            public const string Rare = "rare";
            public const string Epic = "epic";
        }
    }

    /// resource-name: Gold Chest
    /// field: capacity:number
    public const string GoldChest = "gold_chest";

    public static class GoldChestFields
    {
        public const string Capacity = "capacity";
    }
}
```

Parsing rules (producer-side; Phase 7 uses the same ones):

- One resource per `public const string` member. **The `key` is the string literal, byte-for-byte** — the const identifier is only a code-side handle (PascalCase of the key by convention, but the literal wins; module 13's "emitted literal ALWAYS wins" rule applies verbatim).
- `/// resource-name:` → `name` (absent → the const identifier). `/// resource-description:` → `description` (optional).
- `/// field:` → one field per line, named-token grammar (hardened 2026-08-05): `NAME:TYPE[:values=V1,V2,...][:default=VALUE][:desc=TEXT]`. Tokens split on `:`, whitespace-trimmed; `TYPE` ∈ the closed 5; `values=` carries the enumeration's allowed-values list (comma-separated); `default=VALUE` the optional default (never for `date` fields — their default is dashboard-side: the page offers no input and the hand-back carries none); `desc=TEXT` the optional field description. **A field is minimally `NAME:TYPE`** — every other token is optional and omitted tokens are simply not emitted (never invent a default/description). LEGACY read form: older doc-blocks carry enum values as a positional comma-bearing token without `=` (`rarity:enumeration:common,rare,epic`) — parsing still accepts it (recognized only when the line has no `values=` token); the catalogue rebuild WRITES `values=`. With the rejoin rule below, `:` and `=` inside values/default/desc ARE representable; the residual unrepresentables are a comma inside a single enum value (it is the list separator) and the token-lookalike corner in the rejoin rule — leave those page/Dashboard-side.
- Doc lines not matching these prefixes (`<summary>`, free text) are ignored — developers may document freely around the structured lines.
- **Companion constants (usage sugar, user decision 2026-08-03):** the catalogue rebuild ALSO emits, per resource, a sibling `public static class <ConstName>Fields` with one `public const string` per field key (PascalCase identifier; the snake literal byte-for-byte) and, per enumeration field, a nested `<FieldName>Values` class with one const per allowed value — game code works with resources without hand-written literals (`KinoaResources.LegendarySwordFields.RarityValues.Epic`). These classes are DERIVED from the same confirmed list in the same single-writer rebuild; **the manifest measures ONLY the doc-blocks + key consts and ignores `*Fields` classes entirely** — drift is impossible because the rebuild replaces them and nothing else may edit them.
- **Doc-block association rule (pinned — two conformant readings existed):** structured `///` lines bind to the **next** `public const string` member below them; blank lines and plain `//` comments between the doc lines and the const do NOT break the binding; another `const`, any statement, or a closing brace does. Scanning is accumulate-downward: everything structured since the previous const belongs to the next one.
- **Token details (pinned; hardened 2026-08-05):** the field `TYPE` token is case-insensitive (normalize to lowercase — the planner does the same); optional tokens (`values=` / legacy positional `ENUM_VALUES` / `req` — accepted and redundant: every field is `required: true` / `default=` / `desc=`) are recognized by SHAPE, not position — the documented order is a writing convention, not a parsing requirement; inside `values=` / `default=` / `desc=` the FIRST `=` splits key from value, later `=` characters belong to the value (`desc=a=b` → `a=b`). **Rejoin rule (closes `:` inside values):** after splitting on `:`, a fragment that matches NO recognized token shape (not a TYPE, not `values=`/`default=`/`desc=`/`req`, and not the legacy positional comma-token — which is out of play whenever `values=` is present) is REJOINED with `:` onto the preceding named token: `default=1:2` → fragments `default=1` + `2` → default `1:2`; `values=a:b,c` → values `a:b`, `c`. Corner: a value fragment that itself LOOKS like a recognized token (`default=a:desc=b` meaning a literal default `a:desc=b`) cannot be represented — it parses as two tokens; leave such values page/Dashboard-side.

## Manifest integration (Phase 7 — see modules/13-dashboard-sync.md)

At every Phase-7 manifest regeneration, `resources[]` is rebuilt by parsing `KinoaResources.cs` — never from a prior manifest, never from the confirmation page's JSON (both are prior outputs; the class is the code truth). Per entry: `{name, key, description, fields, source}` with `source` = the const's `file:line`. The key/field validation of §4 re-runs at the Phase-7 preflight — violations are preflight flags (the consumer's planner also refuses invalid keys as a backstop). No `KinoaResources.cs` in the project → `resources: []` + one preflight note: *"no resources catalogue — run `/kinoa resources --merge` to register the game's items"*.

## Dashboard

| Instance | Created by | Where |
|---|---|---|
| Resource Template (create DRAFT + activate) | sync skill via `kinoa-dashboard-resource-template` | Dashboard → Bundles → resource templates (admin API: `gate.kinoa.io/bundle/resource-templates`) |

Consumer-side semantics (authoritative in the plugin's `kinoa-sdk-dashboard-sync` skill): absent key → create DRAFT + activate; existing DRAFT → activate (fields updated first when they differ); existing ACTIVE with differing fields → **field-conflict developer gate** (a live template may back live bundles/prizes — never edited unattended); **DEPRECATED → warning, never auto-reactivated** (no un-deprecate endpoint — rename the key AND display name in the catalogue (the retired record still holds both; each is unique across all statuses), clone on the dashboard, or drop the entry); **no deletes, ever** (resource-template delete is HARD and DRAFT-only, operator-facing).

## Common Mistakes

- **Silently dropping currency candidates** — coins/lives/energy ARE awardable resources; carry them flagged and let the gate decide (the old "never propose currency" rule is dead).
- **Skipping the gate** (writing `KinoaResources.cs` straight from the scan, or syncing scan output) — the confirmation gate is the module's load-bearing step; unreviewed candidates register junk templates on the Dashboard.
- **Pre-gating the page with a terminal question** — the page IS the review step; a terminal "register these N items?" before it is a redundant gate that must never replace or precede it.
- **Accepting a stale hand-back** — always check `page_generated_at` against this run's `generated_at`; last week's download in `~/Downloads` is not this run's confirmation.
- **Auto-fixing an invalid key** (slugging `9mm Pistol` → `mm_pistol` without the developer seeing it) — validation surfaces, the developer decides; silent munging registers a key the game's own data doesn't carry.
- **Treating the const identifier as the key** — the string literal is the key; the identifier is a C# handle.
- **Rebuilding the manifest's `resources[]` from a prior manifest or a confirmation JSON** — the class is the only code carrier; both others are stale outputs.
- **Expecting removal to delete** — dropping an entry from the catalogue only stops mirroring it; the Dashboard record stays (operator-owned, `dashboard_only`).
- **Adding runtime API code to `KinoaResources.cs`** — it is pure data; the admin surface is skill-only, and the runtime bundles surface has its own module.
