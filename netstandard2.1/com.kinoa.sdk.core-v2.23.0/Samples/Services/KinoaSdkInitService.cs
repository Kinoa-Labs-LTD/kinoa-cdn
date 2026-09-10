using System;
using System.Threading.Tasks;
using Kinoa.Core.Network.Retry;
using Kinoa.Data.Enum;
using Kinoa.Data.Messaging.InApp;
using Kinoa.Data.Network;
using UnityEngine;

/// <summary>
///     The sample of the Kinoa SDK initialization service.
/// </summary>
public class KinoaSdkInitService : KinoaSingleton<KinoaSdkInitService>
{
    /// <summary>
    ///     Gets or sets a value indicating whether the SDK is initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Kinoa Game unique identifier.
    /// </summary>
    private const string GameID = "YOUR_GAME_ID";

    /// <summary>
    ///     Kinoa Game secret token.
    /// </summary>
    private const string GameToken = "YOUR_GAME_TOKEN";

    /// <summary>
    ///     Gets the SDK version number.
    /// </summary>
    public static string SDKVersion => Kinoa.SDK.Version;

    /// <summary>
    ///     Gets the SDK logging severity level.
    /// </summary>
    public static LogLevel LogLevel => LogLevel.Trace;

    /// <summary>
    ///     Initialize and configure the SDK.
    /// </summary>
    /// <returns>True if the SDK is initialized, otherwise false.</returns>
    public async Task<bool> InitializeAsync()
    {
        if (IsInitialized)
            return true;

        if (string.IsNullOrEmpty(GameToken) || string.IsNullOrEmpty(GameID))
        {
            Debug.LogError("GameID or GameToken is not set.");
            return false;
        }

        IsInitialized = true;

        // Game secrets.
        var gameSecrets = new GameSecrets(gameID: GameID, gameToken: GameToken, allowPii: true);

        // Retry configuration for all network requests (exponential backoff).
        var exponentialRetryConfig = new RetryConfiguration(
            retryReason: RetryReason.AlwaysRetry,
            retryStrategy: RetryStrategy.Exponential,
            maxRetryAttempts: 3,
            maxRetryDelay: 4);

        /*var linearRetryConfig = new RetryConfiguration(
            retryReason: RetryReason.AlwaysRetry,
            retryStrategy: RetryStrategy.Linear,
            maxRetryAttempts: 3,
            retryDelay: 4);*/

        // Network configuration for all SDK requests.
        var networkConfig = new NetworkConfiguration(networkTimeout: 30, exponentialRetryConfig);

        // Tick Game Event configuration (SDK heartbeat event).
        var tickEventsConfig = TickEventsConfiguration.GetCustom(60 * 1000);

        // Game Events security configuration.
        var gameEventsSecurityConfig =
            new GameEventsSecurityConfiguration(isSecurityValidationEnabled: true);

        // Server time usage configuration.
        var timeConfig = new TimeConfiguration(useServiceTime: true);

        // Language resolving configuration.
        var languageConfig = new LanguageConfiguration(autoResolvingEnabled: false);

        // Set SDK logging severity level.
        Kinoa.SDK.SetLogLevel(LogLevel);
        //Kinoa.SDK.SetLogOption(KinoaLogOption.NoStacktrace);

        // Add custom JSON converters before initializing SDK.
        //JsonUtils.AddCustomConverter(new KinoaCustomJsonConverterSample());

        // Register In-app feature configuration types before initializing SDK.
        // Version-specific registration.
        //TODO: Replace with the actual In-app feature schema name and version.
        InAppFeatureConfiguration.Register<InAppDailyBonusFeatureConfiguration>(
            schemaName: "DailyBonus", schemaVersion: 1);
        // Version-agnostic registration (fallback).
        //TODO: Replace with the actual In-app feature schema name.
        //InAppFeatureConfiguration.Register<InAppDailyBonusFeatureConfiguration>(schemaName: "DailyBonus");

        // Initialize the SDK.
        await Kinoa.SDK.Initialize(
            gameSecrets, networkConfig, tickEventsConfig,
            gameEventsSecurityConfig, timeConfig, languageConfig);

        return true;
    }
}
