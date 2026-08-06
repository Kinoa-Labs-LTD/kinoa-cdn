# Currency Rates

## Sample File(s)
- `Services/KinoaCurrencyRatesService.cs`

## Integration Notes
- **Import all methods from the sample as-is** (both `GetStringCurrencyRates` and `GetCurrencyRates`). Do not ask — always include everything.

## Merge Surfaces

In Phase 6 `--merge` mode, this module exposes the following editable surfaces in `KinoaCurrencyRatesService.cs`. All other code stays frozen per the cross-cutting rules in SKILL.md §"Phase 6 — `--merge` mode". Re-run shortcut: `/kinoa currency-rates --merge`.

### Editable surfaces

**Currency identifier literals** — sample placeholder values (currency codes used at request sites) replaced with the developer's actual currency identifiers used in their economy.

Currency identifiers follow the standard Dashboard-context 3-way choice (Real value / Placeholder / Skip) per SKILL.md §"Dashboard-context gap at Modify gates" when the values come from Dashboard configuration.

### Frozen (no in-place edits, except where body-extension applies)
- Method **signatures** of `GetStringCurrencyRates` / `GetCurrencyRates` — strict frozen
- `Kinoa.CurrencyRates.*` SDK call signatures — strict frozen
- The order in which the SDK is invoked relative to callback registration — strict frozen
- Cache logic and response handling bodies in `KinoaCurrencyRatesService.cs` — **body extension allowed** per SKILL.md §"Frozen-scope philosophy" (preserve key moments: SDK call invocation, callback dispatch, response-status check, sample-shipped trace points; do not rewrite wholesale). Typical extensions: custom caching strategies (longer TTL for stable currencies, in-memory layer above disk cache, pre-warm on init, manual invalidation hooks), custom transformations, formatting, error categorization, retry-on-failure, observability hooks.

See SKILL.md §"Phase 6 — `--merge` mode" for the frozen-scope philosophy (commenting out and adding alongside is permitted; in-place edits are not).

## Wiki Reference
- [11 - Currency Rates](https://kinoa.atlassian.net/wiki/spaces/KW/pages/119276040/11+-+Currency+Rates+latest+version) — full API reference

## Dashboard

### Dashboard dependencies — instance types

This module has no Dashboard-configured instance dependencies — currency rates are SDK-served from the Kinoa backend (USD-relative exchange rates, updated server-side) and require no per-game configuration.

### Notes
- Currency identifier values used at request sites come from the game's economy / `Kinoa.Data.Enum.Currency` — neither side is Dashboard-managed.
- Rates are read-only on the SDK; there is no Dashboard surface to override them per game. Cache locally per `KinoaCurrencyRatesService` body extension if frequent lookups are needed.

## Key APIs
- `Kinoa.CurrencyRates.GetStringCurrencyRatesAsync()` — returns `Response<Dictionary<string, double>>` (string currency codes as keys)
- `Kinoa.CurrencyRates.GetCurrencyRatesAsync()` — returns `Response<Dictionary<Currency, double>>` (`Currency` enum as keys)

## Overview
Returns a dictionary of exchange rates used on Kinoa backend for converting non-USD currencies to USD. Two variants: string-keyed and enum-keyed (type-safe, `Kinoa.Data.Enum.Currency`).

## Best Practices
- Use `GetCurrencyRatesAsync()` (enum-based) for type safety with known currencies
- Check `response.IsSuccessful()` and `response.Data != null` before accessing rates
- Cache rates locally for frequent lookups — avoid calling the API repeatedly

## Configuration Notes (what's NOT in the sample)
- **Rates are relative to USD** — e.g., `EUR → 0.85` means 1 USD = 0.85 EUR
- **`Currency` enum** is in `Kinoa.Data.Enum` namespace

## Common Mistakes
- Not checking `response.IsSuccessful()` before accessing rate data
- Not checking `response.Data != null` — data can be null even on non-error responses
- Calling the API too frequently instead of caching rates locally
