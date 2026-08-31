# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres
to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.22.1] - 2026-08-31

### Changed
- Updated saving storages when application is suspended.

## [2.22.0] - 2026-08-06

### Added
- Kinoa Integration Skill — **integration plan page**: an interactive HTML gate to review and edit the proposed events, player fields, feature settings and resources before any code is written. See [AI Integration Skill Guide](https://kinoa.atlassian.net/wiki/spaces/KW/pages/828899329/AI+Integration+Skill+Guide).
- Kinoa Integration Skill — the plan is **validated against your live Dashboard** while you review it: no duplicate entities, existing ones reused, server constraints caught before the sync.
- Kinoa Integration Skill — **game resources** join the mirrored surfaces (`/kinoa resources --merge`): events, player fields, feature settings and resources now mirror end-to-end.

## [2.21.1] - 2026-08-06

### Changed
- Updated time service logic.

## [2.21.0] - 2026-07-20

### Added
- Kinoa Integration Skill — Phase 7 Dashboard Sync now mirrors **Feature Settings** onto the Kinoa Dashboard (`/kinoa dashboard-sync`): infers a feature schema from a CSV data source, registers the schema and its settings entry, and creates a seeded, published default configuration — alongside the existing game-events and player-fields sync. A scoped `reseed <setting-key>` command re-imports a default configuration's data from the source CSV. See [AI Integration Skill Guide](https://kinoa.atlassian.net/wiki/spaces/KW/pages/828899329/AI+Integration+Skill+Guide).
- Kinoa Integration Skill — the `install` predefined event is now registered and published on the first Dashboard sync automatically (the SDK fires it on first device launch, so no game-side call site is required).

### Changed
- Hardened `ContentConstants` game-id resolution (null-safe, lazy cache path) and `SecureJsonConverter` deserialization (guards non-object security-data payloads).

## [2.20.0] - 2026-06-18

### Added
- Kinoa Integration Skill — Phase 7 Dashboard Sync (`/kinoa dashboard-sync`): mirrors the integration's code-defined game events and player fields onto the Kinoa Dashboard. See [AI Integration Skill Guide](https://kinoa.atlassian.net/wiki/spaces/KW/pages/828899329/AI+Integration+Skill+Guide).
- Kinoa Integration Skill — live integration tracking (skill mode): `/kinoa` reports each phase and gate decision to the Kinoa support timeline in real time as you integrate, so the support team can follow your progress from the first step. See [AI Integration Skill Guide](https://kinoa.atlassian.net/wiki/spaces/KW/pages/828899329/AI+Integration+Skill+Guide).

## [2.19.0] - 2026-06-10

### Added
- In-app message view acknowledgement.
- In-app Multiple Feature Settings — new Use existing Feature Settings Feature Configuration Mode on the Dashboard, the InAppMessage.FeatureSettings collection, and three new accessors (GetFeatureConfigurations<T>(key), GetFeatureSetting(key), GetFeatureSettings<T>()).
- New Custom CTA type — InAppCustomClickConfiguration with a CtaName property. The game routes the click to its own handler based on the operator-configured CTA name.

## [2.18.0] - 2026-06-04

### Added
- InApp missions template. 
- Kinoa.Time namespace.

## [2.17.0] - 2026-05-15

### Added
- Kinoa Integration Skill — AI-driven SDK integration via the `/kinoa` Claude Code command (wizard, autonomous, and adaptive merge modes). See [AI Integration Skill Guide](https://kinoa.atlassian.net/wiki/spaces/KW/pages/828899329/AI+Integration+Skill+Guide).

## [2.16.0] - 2026-04-29

### Added
- DisplayOnProgressChange toggle on InAppMilestonesProgressChangedCommand and InAppScoreChangedCommand — controls whether the In-app message should be re-displayed on a progress/score change.

### Changed
- Updated all integration samples distributed with the package.

## [2.15.0] - 2026-03-24

### Added
- In-App Message: Feature Configurations & Bundle Resources support.
- In-App Message: Configuration Filters.
- In-app Message: Image Type in Custom Fields.

## [2.14.0] - 2026-02-09

### Added
- Kinoa.Translations — a new namespace and APIs for working with Dashboard Localization → Translations.
- The Feature Settings download request now supports the IncludeFilters flag to retrieve filters applied to the configuration data.
- In-app sequence ID security validation can now be disabled by configuration.
- Additional debug event parameters for: In-app, Features Settings, Translations.

### Changed
- Deleted entities are now processed as null in Player State diffs when sending Game Events.
- Updated System.Text.Json from version 9.0.1 to 9.0.10.

### Removed
- Removed the Kinoa.PaymentVerification namespace and APIs.
- Removed support for legacy Feature Settings v1.
- Removed dependency on Newtonsoft.Json.

## [2.13.0] - 2025-10-23

### Added
- Old cache clean-up triggered when invoking Kinoa.FeaturesSettings.SmartDownloadAsync if the cached Feature Schema version is outdated.
- New InAppInstanceUpdateCommand — now received when the in-app instance is updated by the operator (e.g., flow placeholders or in-app configuration updates).
- New Kinoa.SDK.SetLogOption signatures providing the ability to exclude sensitive information from logs based on severity levels.

### Changed
- Introduced a common domain name for all API requests.
- Switched from string-based to byte array–based response processing.
- Updated to use asynchronous System.Text.Json overloads for response processing.

## [2.12.6] - 2025-09-10

### Added

- Predefined Player State fields (activity stats from backend, Unity version from game).
- Sync API Game Event retries for wrong sequence IDs now follow global retry settings.
- save_time_ms tracked for all local storage and file write operations.
- Azerbaijani language support.

### Changed
- Resolved race condition in sequence ID requests during active player changes.
- Simplified integration samples in the Unity package.

## [2.12.5] - 2025-08-11

### Added
- Unity version to Player State.

### Changed
- Fixed FileNotFoundException thrown on Features Setting metadata save.

## [2.12.4] - 2025-07-07

### Added
- In-app countdown timer extra properties: IsExpired, EndTimestampWithExtraLifeTime.

### Changed
- Fixed the Features Settings Checksum related unhandled ArgumentNullException.

## [2.12.3] - 2025-06-27

### Changed
- Improved local file saving write operation.

## [2.12.2] - 2025-06-19

### Added
- Milestones custom fields.
- Floating point milestones progression support.
- Cancellation on deserialization layer.

## [2.12.1] - 2025-06-06

### Added
- The Previous Progress field for Milestones In-app.
- The Previous Progress field for Progression In-app.

## [2.12.0] - 2025-06-02

### Added
- Support for predefined In-app One CTA template.
- Ability to add a custom System.Text.Json converter.

### Changed
- Updated A/B test distribution property in In-apps.
- Updated A/B test distribution property in Feature Settings.
- Feature Settings Smart Flow improved: now uses a single request to compare local checksums with the server and download FS only if needed. 
- Redesigned local file handling to ensure atomic write operations. Investigated and improved resilience against corrupted data during file unpacking.

## [2.11.2]

### Changed
- Improve offline mode initialization flow. 
- Reworked retrieving sdk settings. 

## [2.11.1]

### Added
- Virtual NetworkConfiguration.IsInternetReachable method. 

## [2.11.0]

### Added
- In-app Milestones Feature
- AB test distribution in In-apps
- AB test distribution in Features Settings
- Player State as an optional parameter in the Game Session Start request
- New web request client based on .Net HttpClient as alternative to UnityWebRequest

### Changed
- JsonSerializerOptions access.
- Signature of the Game Session Open request: The Player State parameter is optional
- Upgraded System.Text.Json to version 9.0.1
- Checking for internet reachability on requests processing
- WebSocket protocol security updates: 
	- In-app Sequence ID checksum calculation
	- Security validation for Push Notification In-apps

## [2.10.0]

### Added
- Synchronous Game Events security by sequence ID.
- In-app eligibility update method.
- Method to get the Metadata of the Cached/Built-in Features Settings.
- Method to download only Features Settings with an outdated checksum.

## [2.9.2]

### Changed
- Handling of InvalidOperationException on PlayerState serialization

## [2.9.1]

### Added
- Player State as Dictionary. 

- Extending exponential and linear API request retry strategies:
	- RetryTimeout, MaxRetryTimeout, MaxRetryAttempts.

- The requested Features Settings contain the Bundle Resources collection.
- Multiple bundles get method.

- Cancellation tokens for all SDK methods.
- Async overload of the Kinoa.Player.CreateAsync method.

### Changed	
- The sample of handling the Player State is changed by an operator event. 
  The handler already contains the updated Player State.

## [2.8.0]

### Added
- Feature Settings Debug Event.
- Offline Debug Event.

- The compressed Features Settings.
- The multiple Features Settings checksum long polling.

- Methods async overloads:
	- Kinoa.Player.GetRelatedAccountsAsync
	- Kinoa.Player.ApproveStateChangesAsync

- Support for new languages: Serbian, Croatian, Malay.
- Thread-safety improvements.

### Changed	
- Player Language Configuration.

## [2.7.3]

### Added

- Languages: Serbian, Croatian, Malay.

## [2.7.0]

### Added

- Samples Integration Guide.

- Open Session Request.
- Session Start Game Event.

- In-app cooldown by game session count.
- In-app custom elements.

- Collected Resource Game Event.

## [2.6.0]

### Added
- In-app object extension with additional fields: 
	- Collection of the In-app Configuration data filters.
	- Collection of the In-app Audiences.
	- Collection of the In-app User Lists.
	
### Changed	
- Improved Install event.
- Retry for settings during Get Player State call.

## [2.5.0]

### Added

- Kinoa.FeaturesSettings.NotifyWhenChecksumChanged method.
- The multiple Game Events send method.

## [2.4.3]

### Added
- Skip sending debug events to non-debug players.

## [2.4.2]

### Changed
- Fixed non-sending Game Events / Remove the validation for game pausing before the Game Events sending.

## [2.4.1]

### Added
- Extra time metrics of the Events Storage.

## [2.4.0]

### Added
- The Features Settings updates:
	- The built-in Features Settings get method: get by Schema version.
	- The Features Settings checksum get method.
	- The Feature Settings save to cache after download.
	- The cached Features Settings get method.
	- The Feature Settings Smart Download method.

- The Sync Game Events response update: The list of UUIDs that were removed from the Inbox.

## [2.3.0]

### Added
- Custom fields of the In-app custom template images, buttons, and text objects.

### Changed
- In-app custom template images, buttons, and text objects processing.
- FeatureSettingsRequestParams.GetDefault property behaviour:
	- If the relevant Feature Configuration is not found, and the value is false - the default Configuration is returned.

## [2.2.0]

### Added
- In-app Soft Billing CTA.
- In-app Countdown Timer update method.

- Player delete method.
- Kinoa.GameEvents.SendStartSessionEvent method signature update - returns the server-side Player State.
- Internal Game Events of the WebSocket lifecycle: web_socket_opened, web_socket_closed, web_socket_error.

- Feature Settings response object extension with additional fields: 
	- Feature Settings Configuration name.
	- Collection of the Feature Settings Data Filters.
	- Collection of the Feature Configuration Audiences segmentation.
	- Collection of the Feature Configuration User Lists segmentation.
	- Unix timestamp in milliseconds of the Feature Configuration scheduled start time.
	- Unix timestamp in milliseconds of the Feature Configuration scheduled end time.

### Changed
- Session Start Game Event sends only the Player State diff instead of the full body.
- KinoaAsyncDispatcher improvements.

## [2.1.0]

### Added
- Synchronous Game Events.
- In-app Placement property.
- Method to get Built-in Features Settings.
- Method to allow PII (Personally Identifiable Information) in runtime.

### Removed
- In-app Lobby Icon Placement property.

## [2.0.0]

### Added
- Reduced SDK size: "System.Text.Json" assembly stipping is enabled.

## [1.20.3]

### Added
- API request information in the SDK response object.
- Long timestamp to DateTime converter.
- The error event is extended with the thrown Exception stack trace.

### Changed
- Fix DateTime, DateTimeOffset, TimeSpan deserialization.

## [1.20.2]

### Changed
- Fix Player State diff calculation.

## [1.20.1]

### Changed
- Fix Old Features Settings serialization.

## [1.20.0]

### Added
- Create Player request with custom Player ID

## [1.19.1]

### Added
- PlayerBlockedByOperator error handling.
- Enum default value support during the deserialization.
- PlayerCreateIDs now supports the custom Player ID on "Kinoa.Player.Create" an SDK request.

## [1.19.0]
This release brings a major improvement to serialization performance by migrating from Newtonsoft.Json to System.Text.Json.

### Added
- The In-app progression scoring

### Changed
- All SDK data models are migrated from Newtonsoft to System.Text.Json:
	- The Player State integration sample  
	- The Features Settings integration sample   
	- The WebSocket message processing:
		- The WebSocket CommandMessage processing
		- The WebScoket InAppMessage processing :
			- The InAppCommand processing
			- The InAppSimpleTemplateData and InAppCustomTemplateData processing 
			- The InAppClickConfiguration processing

### Removed
- UpdateEconomyCommand WebSocket command is no longer supported

## [1.18.2]

### Added
- TutorialEventData constructor with a string action type

## [1.18.1]

### Changed
- Fixed reset player state event.


## [1.18.0]

### Added
- New Features Settings
- Currency rates provider
- New watch_ad Game Event 
- Security mechanism for Game Events
- Internal In-app events: in_app_received, in_app_created, in_app_inbox_deleted, in_apps_inbox_received
- SDK of .NET standard 2.0. Compatibility with old Unity versions

### Changed
- Concurrent serialization and updating of the Player State object
- Closing of the iOS WebSocket connection on network hotspot change
- Settings Service improvements - remove duplication of get settings request
- Reworked in_app_impression event. It becomes public and does not send automatically (replaced with in_app_received). Now the game client should decide when to send an In-app impression event
- In-app security updates: In-app messaging security configuration

## [1.17.0]

### Added
- In-app security layer
- In-app integration samples: SecurityData and OriginalJson In-app properties access

## [1.16.0]

### Added
- InAppMessage.FlowId - the In-app Flow identifier (see integration samples and InAppMessage class)
- InAppMessage.LobbyIcon.Score - The In-app lobby icon's score (weight or z-index) helps manage icon ordering (see integration samples and InAppLobbyIcon class)
- InAppMessage.Scheduling - The In-app message scheduling information (start/end time) (see integration samples and InAppMessageScheduling class)
- InAppMessage.InboxStats.Reminders - the In-app reminders counter (see integration samples and InAppMessageStatistics class)
- InAppMessage.ResetRemindersMetrics() - the In-app reminders counter reset method (see integration samples and In-app message methods)
- Internal in_app_received event with In-app debug information

### Changed
- PaymentEventData constructor update (see integration samples)
- Optimized internal slow_request event

### Removed
- GameEventData.SetSuccessfulCompletion() method. The Game Event Success property now always equals “true”, else the event should not be raised

## [1.15.0]

### Added
- Get Player State by any Player ID
- A CalculatedFields property inside the PlayerState object - calculated fields from your Google Bucket file
- Using of SocialConnectEventData, SocialDisconnectEventData, and SocialPostEventData instead of SocialEventData (see samples)
- Logging the response data inside the error event on throwing the ParseException
- Logging of requests that take more than 1 second

## [1.14.0]

### Added
- A Tick event description
- A Tick event configuration (see also Initialize SDK method)
- A possibility to enable/disable the Personally Identifiable Information (PII) usage by the SDK (see also Initialize SDK method)
- Player State property - Is a blocked Player / SetIsBlocked Player State method (see OnCheatingEvent Integration samples)
- Player State property - Is a cheater Player / SetIsCheater Player State method (see OnCheatingEvent Integration samples)
- The In-app message lobby icon text

### Changed
- GameSecrets constructor is extended with the AllowPii flag
- Kinoa.SDK.Initialize method is extended with the TickEventsConfiguration
- UnityEngine.SystemInfo.deviceUniqueIdentifier  is used by the SDK automatically only if the AllowPii is enabled
- Secured WebSocket connection with a Game-Auth header

## [1.13.0]

### Added
- Mobile Traffic decreased by the Player State Diff
- JsonDiffPatch.Net NuGet package (see Dependencies)
- Additional Player identifiers (GoogleId, AppleId, ExtraIds) on:
	- New Player Creation
	- New Test Player Creation
	- Getting Player accounts
	- Getting Player State
	- Player State updating (see OnSocialConnect and OnSocialDisconnect integration samples)

- Getting/Setting the localization language of the active Player

### Changed
- Updated minimum Unity version from 2020.2 to 2021.2 to support .netstandard2.1

## [1.12.0]

### Added
- In-app message from Push

## [1.11.0]

### Added

- Settings Service
- Fixed performance issues

## [1.10.0] 

### Added

- WebGL build support
- Game-Auth security layer for the Web API requests
- In-app messages ver. 2
- The In-app integration samples
- The global network configuration of SDK requests
- The network configuration of the session start request
- A WebSocket command to indicate the removal of In-app messages from the inbox
- A sample of initializing the Player State properties that can only be called from the main thread

### Changed

- In-app message structure to the ver. 2
- Single client handler for both optional and mandatory In-app messages
- Asynchronous Kinoa.GameEvents.SendStartSessionEvent method
- Asynchronous Kinoa.Player.ResetState method

### Removed

- NewInboxMessageCommand and NewInboxMessagesCommand WebSocket Commands.


## [1.9.0] 

### Added

- Kinoa.FeaturesSettings.NotifyWhenChecksumChanged method
- In-app message fields:
	- The “Trigger In-app only by Lobby Icon” field (ShowOnTrigger)
	- The UUID of the replaced In-app message (ReplacedUuid)
	- The placement number/ID of the lobby icon (LobbyIconPlacement)
	- Button fields in the custom template:
		- The background image type (Kind)
		- The background image link (BackgroundImage)

### Changed

- Fixed incorrect deserialization of a string property that contains a DateTime value

## [1.8.2] 

### Added

- Payment verification error codes
- In-app message fields:
	- The In-app message name (Name)
	- The timestamp when the In-app message was sent (SentTime)
	- The In-app message eligibility:
		- The original eligibility limit (Capping.EligibilityLimit)
		- The actual eligibility limit (Capping.ActualEligibility)
		- Is the ActualEligibility limit equal to 0 (Capping.IsEligibilityUsed)
	- The In-app message inbox statistics metrics:
		- The inbox message views counter (InboxStats.Views)
		- The inbox message usage counter (InboxStats.Usage)

- In-app message update methods:
	- Sets the actual eligibility limit (SetEligibility)
	- Sets the number of times the inbox message has been viewed (SetViewsMetrics)
	- Sets the number of times an inbox message is used (SetUsageMetrics)
	- Sets In-app message client custom parameters (SetCustomParameters)
	- Adds In-app message custom parameters (AddCustomParameters)

-Multiple In-app messages delete method

### Removed

- Kinoa.GameEvents.SendInAppEligibilityEvent method

## [1.8.1]

### Changed
- Asynchronous WebSocket messaging initialization
- PaymentEventData singature

## [1.8.0] 

### Added

- Get Resources from Bundles by bundle key
- In-App message countdown timer hide option
- Enum of In-app message image types

### Changed

- The C# version increased to 8.0. Minimal Unity version: 2020.2
- Asynchronous read/write operations of local files: Features Settings, Game Events storage, P2P Events storage, Players storage
- Asynchronous read/write operations of built-in Streaming Assets
- Asynchronous REST API calls pre-processing and post-processing
- Asynchronous JSON serialization and deserialization
- Asynchronous GZip compression and decompression
- Unblocked the main UI thread during SDK services use
- Other performance improvements

## [1.7.4] - 2023-04-01

### Changed

- Async read/write of local storages
- Async SDK initialization


## [1.7.3] - 2022-08-12

### Added

- Kinoa.FeaturesSettings.GetLastCachedOrBuiltIn method
- Kinoa.FeaturesSettings.GetBuiltIn method
- Kinoa.FeaturesSettings.GetCached method
- In-App message Eligibility event

### Changed

- Extended Kinoa.GameEvents.OnGameSessionStarted callback
- Extended Kinoa.GameEvents.OnGameSessionStartFailed callback
- How to process the WebSocket Command message

- Updated In-app events integration samples:
	- In-app message click
	- In-app message close
	- In-app message impression

- Bug fixes and other improvements:
	- Fixed outdated cache issue
	- More informative logs for the events

## [1.7.2] - 2022-22-11

### Added

- Configurable network requests timeout
- Features Settings download cancellation token
- In-app messages creation by External Link
- PromiseRewards action on In-app message click
- UpdateAppVersion action on In-app message click
- PlayerStateChangedByOperator WebSocket command
- A field indicating the In-app message is triggered on an offline event

### Changed

- SDK initialize method
- Extended callbacks of Features Settings methods 
	- Download Features Settings

- Extended callbacks of P2P events methods 
	- Get P2P Event
	- Delete P2P Event

- Extended callbacks of Player methods 
	- Create new Player
	- Get Player accounts
	- Get Player State
	- Approve state changes

- Bug fixes
	- Game callbacks invocation when the game session is closed
	- Custom Player State deserialization issues
	- Enums deserialization issues

## [1.7.1.0] - 2022-09-11

### Added

- Beta: Purchase validation for iOS and Android.
- In-app generation by token.
- Bad response error codes.

### Changed

- Resource management methods signatures.
- In-app messaging methods signatures.

## [1.7.0.0] - 2022-01-11

### Added

- Resource Management.
- Game session implementation.
- In-app messages support (Simple / Custom templates).
- Get inbox messages.
- New inbox message web socket command.
- Delete inbox message by ID.
- Delete all inbox messages.
- In-app click event.
- In-app impression event.
- Create new Tester Player.

### Changed

- Bug fixes, refactoring, and other improvements.

## [1.6.0.0] - 2022-09-14

### Added

- GameID support.
- Game-Auth security layer.
- Async UnityWebRequest client.
- Unstable Internet connection handling. Retries implementation.
- Get Feature Settings directly from cache.

### Changed

- Global SDK project structure updates.
- Bug fixes, refactoring, and other improvements.

## [1.5.5.0] - 2022-06-22

### Added

- Logging severity levels.
- Predefined state fields:
  - The "PlayerState.SessionData" field includes important session data.
  - The "PlayerState.Devices" field includes player devices data.
  - Other important state fields.
  
- Get/Set player country method (Countries enum).
- Get/Set player language method (Languages enum).

### Changed

- Built-in features settings mobile platforms fix.
- The "PlayerState.TimeZone" updates.
- The "GameEventData" fields updates.
- Reset Player State business logic updates.
- Bug fixes, refactoring, and other improvements.

## [1.5.0] - 2022-05-24

### Added

- In-app messages.
- In-app close event.
- Get Features Settings by key.

### Changed

- Bug fixes, refactoring, and other improvements.

## [1.4.5] - 2022-04-19

### Added

- Get player state request.
- Update player state request.
- Create the new player request.
- Compatibility with the player state derided types.
- An ActivePlayerID setter moved on the game side.
- Get all player-related accounts request.
- Approve player state changes request.
- Handling player state-changing by an operator.
- Country and language code support by the SetPlayerInfo player state extension method.
- P2P events:
  - Get P2P events request.
  - Send P2P events request.
  - Delete P2P events request.
- Messaging & WebSocket protocol support:
  - Command messages.

### Changed

- Bug fixes, refactoring, and other improvements.

## [1.4.0] - 2022-02-18

### Added

- An "Error" event:
    - SDK internal critical errors reporting.
    - Client-side errors reporting.

### Changed

- Transition to the UUID's identifiers.
- Transition to the Single-economy (features settings).
- Transition from "Economy" to "Features Settings" component:
    - Updated namespaces and signatures: "Kinoa.Economy" to "Kinoa.FeaturesSettings"
    - Updated built-in features settings file path

- Third-party dependencies:
    - Used Unity compatible newtonsoft.json package: "jillejr.newtonsoft.json-for-unity" to "
      com.unity.nuget.newtonsoft-json"

- Bug fixes and other improvements.

## [1.3.5] - 2022-01-14

### Added

- Compressed events data-transfer.
- The updated economies compression algorithm.

- The new events signatures.
- Billing "platform" field setter.
- "Install" event automatic raising on the first game launch.

- "KinoaBaseProvider" - SDK basic methods and properties provider sample.
- "KinoaEventsProvider" - SDK events provider sample.

### Changed

- SDK configuration is moved from "KinoaAnalyticsProvider" to the new "KinoBaseProvider" component.
- Events are moved from "KinoaAnalyticsProvider" to the new "KinoaEventsProvider" component.
- Updated SDK namespaces:
    - "Kinoa.Analytics" was renamed to "Kinoa.Events".
    - "Kinoa.SDK" - contains SDK basic methods and properties.

- Bug fixes and other minor improvements.

### Removed

- "Install" event manual invocation possibility.
- "KinoaAnalyticsProvider" was removed.

## [1.3.0] - 2021-11-08

### Added

- One-click integration – just import the Kinoa samples from Unity PM to your game and that's it.

- Kinoa Offline gaming:
    - Offline and built-in economies are available now. See the documentation for more
      details: "https://docs.google.com/document/d/10no1lT7n-3IG_IRFoeF8C-6-XYQ0sOtJ/edit#heading=h.9cy9xioexqo4"
    - Offline local events storage were implemented.

- Сommon SDK files storage mechanism developed.
- Multiple players events storage supporting. Sending, failed, and actual events storage for each player.
- Events processing behaviour was changed.

- "Kinoa.Analytics.SetPlayerChangedHandler" method added – returns an active player identifier when it was changed. Use
  cases:
    - Download Economy for the new Player
    - In-app business logic on Active Player changed

- "start_session" / "install" events were updated:
    - SetSocialNetworks possibility added
    - Device information was extended with the new extra fields:
      "time_zone", "device_model", "screen_resolution", "screen_dpi", "locale"

- "social_connect" / "social_disconnect" events were updated:
    - Player state – progress and player balance is required now

### Changed

- Kinoa CDN is moved to the Kinoa repositories. The new Kinoa SDK package URL:
  "https://bitbucket.org/kinoa-team/kinoa-cdn/src/master/kinoa-sdk-v.1.3.0"

- "Kinoa.Economy.Checksum" / "Kinoa.Economy.Download" signature updates

- Bug fixes and other minor improvements.

### Removed

- "start_session" event – application handled callback was removed

## [1.2.1] - 2021-09-30

### Added

- Social disconnect event. Social connect and disconnect states added.

### Changed

- Event data custom parameters for all events.
- Bug fixes and other minor improvements.

### Removed

- ThirdPartyServices field from PlayerState.

## [1.2.0] - 2021-08-30

### Added

- Multiple economies caching system.
- Economies filters: by name, by category.
- SDK testing solution.
- Bug fixes.

## [1.1.0] - 2021-08-09

### Added

- Economies checksum request.
- Economies downloading request.
- Economies caching system.
- Compressed economies files implementation (20kB instead of 7mB).
- Bug fixes.

## [1.0.0] - 2021-07-30

### Added

- The first version of SDK developed.
- All game events are sent and processed via the API and Kinoa SDK for Unity.
- Event data aggregated by ClickHouse.
- Kinoa API analyzes data and calculates the audiences dynamically.

### Changed

- SDK is moved to a separate repository to provide the possibility to instal the SDK through the Unity Package Manager.

### Removed

- SDK is removed from C&F repository.

### Fixed

- All the major and minor bugs were fixed.