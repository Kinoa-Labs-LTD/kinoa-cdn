# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres
to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-12

### Added
- `Kinoa.Emails.SDK.Initialize()` — initializes the Emails module; returns `false` and logs if Kinoa Core SDK is not initialized.
- `Kinoa.Emails.SDK.RegisterEmail(email, fields)` / `RegisterEmail(playerId, email, fields)` — registers the player's email with optional string fields; returns `KinoaEmailRegistrationResult` (`Status`).
- `Kinoa.Emails.SDK.DeleteEmail()` / `DeleteEmail(playerId)` — deletes the player's email.
- `Kinoa.Emails.SDK.GetLinkParameters(url)` — returns the URL-decoded query parameters of any link (https or a custom deep link scheme).
- `Kinoa.Emails.SDK.GetInAppCreationParams(url)` / `GetInAppCreationParams(data)` — read the Kinoa In-app data (`inAppData`) from an email link or from deep link data passed by AppsFlyer, Branch or Adjust callbacks, and return `InAppByEmailCreationParams` for `Kinoa.Messaging.CreateInAppMessageAsync`; `null` when the link carries no valid Kinoa In-app data. No initialization required. Requires `com.kinoa.sdk.core` 2.24.0+.
- `Samples/Services/KinoaEmailsService.cs` — sample service: initialize the module, register and delete the player email.
