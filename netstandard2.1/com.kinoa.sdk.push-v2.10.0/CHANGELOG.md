# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres
to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.9.0] - 2025-10-23

### Added
- Introduce a common domain name for all api requests.

## [2.8.0] - 2025-10-08

### Added
- Android icon tint color.

## [2.7.1] - 2025-06-19

### Added
- Compatible with com.kinoa.sdk.core ver. 2.12.2.

## [2.7.0] - 2025-06-02

### Added
- Android: Skip and silent foreground push notifications.

## [2.6.1]

### Added
- Compatible with com.kinoa.sdk.core ver. 2.11.2.
- Added Proguard rules for KinoaNotificationManager.aar.

## [2.6.0]

### Added
- Compatible with com.kinoa.sdk.core ver. 2.11.0.
- Ability to initialize Kinoa Push SDK without resolving Firebase dependencies.

## [2.5.0]

### Added
- Compatible with com.kinoa.sdk.core ver. 2.9.0.

## [2.4.0]

### Added
- Compatible with com.kinoa.sdk.core ver. 2.6.0.

## [2.3.0]

### Added
- Compatible with com.kinoa.sdk.core ver. 2.6.0.

## [2.2.0]

### Added
- Ability to place custom sounds for iOS push notifications in the Assets/StreamingAssets/Kinoa/Sounds folder.
- Support for resolving Android native dependencies with EDM4U.
- Automatic FCM push token registration, even when a custom in-game FCM implementation already exists. 

## [2.1.0]

### Added
- Push click handler with Internal Links support.

### Changed
- Compatible with com.kinoa.sdk.core ver. 2.4.0.

## [2.0.0]

### Added
- Android sounds and icons.
- iOS sounds.

## [1.10.3]

### Changed
- Compatible with com.kinoa.sdk.core ver. 2.1.0.

## [1.10.2]

### Changed
- Compatible with com.kinoa.sdk.core ver. 1.20.3.

## [1.10.0]

### Added
- iOS badge counter.

## [1.9.0]

### Added
- Push personalization.


## [1.8.0]

### Changed
- Autodeleting token on swiching Kinoa players.

## [1.7.1]

### Changed
- Kinoa Rich Push for Android hotfixes

## [1.7.0]

### Added
- Kinoa Rich Push for Android


## [1.6.0]

### Added
- Support changing localization language from SDK

### Changed
- Updated minimum Unity version from 2020.2 to 2021.2 to support .netstandard2.1

## [1.5.0]

### Added

- In-app message from Push
- Rescheduling of local pushes
- Image for iOS local pushes

## [1.4.0.0]

### Added

- Local storage for Android local push notifications
- Dismiss option for iOS pushes
- Integration with Settings service

## [1.3.0.0]

### Added

- Local push notification:
    - Schedule/cancel local notifications
    - Calendar and interval triggers
- Block/unblock pushes:
    - Block/Unblock Kinoa Push Notifications with REST call
    - Get current status of Kinoa Push Notifications
 

## [1.2.0.0] - 2023-01-19

### Changed

- Kinoa core major update

## [1.1.0.0] - 2022-12-06

### Added

- Apple Push Notification Extension service.

## [1.0.0.0] - 2022-07-11

### Added

- Firebase Cloud Messaging client.
- Apple Push Notification service client.