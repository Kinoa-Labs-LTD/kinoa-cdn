using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kinoa.Core.Callbacks;
using Kinoa.Data;
using Kinoa.Data.Enum;
using Kinoa.Data.FeaturesSettings;
using Kinoa.Data.FeaturesSettings.Enum;
using UnityEngine;

/// <summary>
///     Kinoa Features Settings service.
/// </summary>
public class KinoaFeaturesSettingsService : KinoaSingleton<KinoaFeaturesSettingsService>
{
    /// <summary>
    ///     The sample client-side Feature Settings collection.
    /// </summary>
    public List<FeatureSettingsResponse<FeatureSettingsData>> LocalFeatureSettings { get; private set; } =
        new List<FeatureSettingsResponse<FeatureSettingsData>>();

    #region Download Methods

    /// <summary>
    ///     Smart Download (recommended). Compares local checksums with the server in a single request.
    ///     Downloads only outdated settings from the server; up-to-date settings are taken from cache or built-in.
    ///     The response always contains ALL requested settings regardless of source.
    /// </summary>
    /// <param name="settingsRequestParams">The Features Settings request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onProgress">Callback invoked on download progress change.</param>
    /// <returns>Response containing all requested settings from server, cache, or built-in.</returns>
    public async Task<Response<FeaturesSettingsResponse<FeatureSettingsData>>> SmartDownloadAsync<TData>(
        List<FeatureSettingsSmartDownloadRequestParams> settingsRequestParams,
        CancellationToken cancellationToken = default, ProgressChangedCallback onProgress = null)
    {
        settingsRequestParams ??= DefaultSmartDownloadParams();
        cancellationToken = EnsureCancellationToken(cancellationToken);

        var response = await Kinoa.FeaturesSettings.SmartDownloadAsync<FeatureSettingsData>(
            settingsRequestParams, cancellationToken, onProgress ?? OnDownloadProgressChanged);

        LogSettingsData(response.Data?.Settings);
        ReplaceFeatureSettings(response.Data?.Settings);

        if (response.IsSuccessful())
        {
            //TODO: Successful response handling.
            Debug.Log("[KINOA] Features Settings smart download request was successful.");

            // Optional: to periodically detect and apply server-side FS updates during gameplay, use one of:
            // DownloadIfChecksumChangedAsync (recommended — manual, controlled, e.g. on loading screen)
            // ConfigureChecksumLongPolling (automatic background polling on timer interval)
        }
        else if (response.IsConnectionError())
        {
            //TODO: Connection error handling.
            Debug.Log("[KINOA] Features Settings smart download: connection error. " +
                      "response.Data still contains settings from cache/built-in (per FeatureSettingsFailedDownloadStrategy).");
        }
        else if (response.IsResponseFailed())
        {
            //TODO: Bad response handling.
            Debug.Log("[KINOA] Features Settings smart download: request failed. " +
                      "response.Data still contains settings from cache/built-in (per FeatureSettingsFailedDownloadStrategy).");
        }
        else if (response.IsResponseCanceled())
        {
            //TODO: Cancellation handling.
            Debug.Log("[KINOA] Features Settings smart download request was canceled." +
                      "\nThe settings that were downloaded before cancellation are returned.");
        }

        return response;
    }

    /// <summary>
    ///     Direct download from the server. No checksum comparison — always downloads all requested settings.
    ///     Downloaded settings are automatically saved to cache.
    ///     Consider using <see cref="SmartDownloadAsync{TData}"/> instead (best practice).
    /// </summary>
    /// <param name="settingsRequestParams">The Features Settings download request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onProgress">Callback invoked on download progress change.</param>
    /// <returns>Response containing all requested settings from the server.</returns>
    public async Task<Response<FeaturesSettingsResponse<FeatureSettingsData>>> DownloadAsync<TData>(
        List<FeatureSettingsDownloadRequestParams> settingsRequestParams,
        CancellationToken cancellationToken = default, ProgressChangedCallback onProgress = null)
    {
        settingsRequestParams ??= DefaultDownloadParams();
        cancellationToken = EnsureCancellationToken(cancellationToken);

        var response = await Kinoa.FeaturesSettings.DownloadAsync<FeatureSettingsData>(
            settingsRequestParams, cancellationToken, onProgress ?? OnDownloadProgressChanged);

        if (response.IsSuccessful())
        {
            LogSettingsData(response.Data.Settings);
            ReplaceFeatureSettings(response.Data.Settings);
        }
        else if (response.IsConnectionError())
        {
            Debug.Log("[KINOA] Features Settings download request failed with connection error.");
        }
        else if (response.IsResponseFailed())
        {
            Debug.Log("[KINOA] Features Settings download request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            Debug.Log("[KINOA] Features Settings download request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Downloads only Feature Settings whose checksum differs from <see cref="LocalFeatureSettings"/>.
    ///     Best practice: call at defined game moments (e.g., loading screen) — more controlled than <see cref="ConfigureChecksumLongPollingAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onProgress">Callback invoked on download progress change.</param>
    /// <returns>Response with updated settings (Status == Ok) and unchanged settings (Status != Ok).</returns>
    public async Task<Response<FeaturesSettingsResponse<FeatureSettingsData>>> DownloadIfChecksumChangedAsync<TData>(
        CancellationToken cancellationToken = default, ProgressChangedCallback onProgress = null)
    {
        var settingsRequestParams = LocalFeatureSettings
            .Select(x => new FeatureSettingsDownloadRequestParams(
                key: x.Request.Key,
                version: Convert.ToUInt16(x.Request.Version),
                checksum: x.Checksum))
            .ToList();

        cancellationToken = EnsureCancellationToken(cancellationToken);

        var response = await Kinoa.FeaturesSettings.DownloadIfChecksumChangedAsync<FeatureSettingsData>(
            settingsRequestParams, cancellationToken, onProgress ?? OnDownloadProgressChanged);

        if (response.IsSuccessful())
        {
            var okSettings = response.Data.Settings
                .Where(x => x.Status == FeatureSettingsResponseStatus.Ok).ToList();
            LogSettingsData(okSettings);
            ReplaceFeatureSettings(okSettings);

            var notOkSettings = response.Data.Settings
                .Where(x => x.Status != FeatureSettingsResponseStatus.Ok).ToList();
            LogSettingsData(notOkSettings);
        }
        else if (response.IsConnectionError())
        {
            Debug.Log("[KINOA] Features Settings download request failed with connection error.");
        }
        else if (response.IsResponseFailed())
        {
            Debug.Log("[KINOA] Features Settings download request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            Debug.Log("[KINOA] Features Settings download request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     On Features Settings download progress changed.
    /// </summary>
    private static void OnDownloadProgressChanged(decimal progress)
    {
        Debug.Log($"Features Settings download progress changed: {progress}");
    }

    #endregion

    #region Built-in & Cached

    /// <summary>
    ///     Gets Built-in Features Settings from StreamingAssets (offline fallback).
    ///     Requires exported "Default Feature Settings.zip" unpacked to Assets/StreamingAssets/Kinoa/.
    /// </summary>
    /// <param name="settingsRequestParams">The Features Settings request parameters.</param>
    /// <returns>Response containing settings loaded from StreamingAssets.</returns>
    public async Task<Response<FeaturesSettingsResponse<FeatureSettingsData>>> GetBuiltInAsync<TData>(
        List<FeatureSettingsRequestParams> settingsRequestParams)
    {
        settingsRequestParams ??= DefaultLocalRequestParams();

        var response = await Kinoa.FeaturesSettings.GetBuiltInAsync<FeatureSettingsData>(settingsRequestParams);
        if (response.IsSuccessful())
        {
            LogSettingsData(response.Data.Settings);
            ReplaceFeatureSettings(response.Data.Settings);
        }
        else if (response.IsResponseFailed())
        {
            Debug.Log("[KINOA] Features Settings get request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            Debug.Log("[KINOA] Features Settings get request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Gets Built-in Features Settings metadata (key, version, checksum — no data).
    /// </summary>
    /// <param name="settingsRequestParams">The Features Settings request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing metadata without Feature Settings data.</returns>
    public async Task<Response<List<FeatureSettingsMetadataResponse>>> GetBuiltInMetadataAsync(
        List<FeatureSettingsRequestParams> settingsRequestParams, CancellationToken cancellationToken = default)
    {
        settingsRequestParams ??= DefaultLocalRequestParams();

        var response = await Kinoa.FeaturesSettings.GetBuiltInMetadataAsync(settingsRequestParams, cancellationToken);
        if (response.IsSuccessful())
        {
            LogSettingsMetadata(response.Data, DataSourceType.BuiltIn);
        }
        else if (response.IsResponseFailed())
        {
            Debug.Log("[KINOA] Features Settings metadata get request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            Debug.Log("[KINOA] Features Settings metadata get request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Gets Cached Features Settings from local cache.
    /// </summary>
    /// <param name="settingsRequestParams">The Features Settings request parameters.</param>
    /// <returns>Response containing settings loaded from local cache.</returns>
    public async Task<Response<FeaturesSettingsResponse<FeatureSettingsData>>> GetCachedAsync<TData>(
        List<FeatureSettingsRequestParams> settingsRequestParams)
    {
        settingsRequestParams ??= DefaultLocalRequestParams();

        var response = await Kinoa.FeaturesSettings.GetCachedAsync<FeatureSettingsData>(settingsRequestParams);
        if (response.IsSuccessful())
        {
            LogSettingsData(response.Data.Settings);
            ReplaceFeatureSettings(response.Data.Settings);
        }
        else if (response.IsResponseFailed())
        {
            Debug.Log("[KINOA] Features Settings get request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            Debug.Log("[KINOA] Features Settings get request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Gets Cached Features Settings metadata (key, version, checksum — no data).
    /// </summary>
    /// <param name="settingsRequestParams">The Features Settings request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing metadata without Feature Settings data.</returns>
    public async Task<Response<List<FeatureSettingsMetadataResponse>>> GetCachedMetadataAsync(
        List<FeatureSettingsRequestParams> settingsRequestParams, CancellationToken cancellationToken = default)
    {
        settingsRequestParams ??= DefaultLocalRequestParams();

        var response = await Kinoa.FeaturesSettings.GetCachedMetadataAsync(settingsRequestParams, cancellationToken);
        if (response.IsSuccessful())
        {
            LogSettingsMetadata(response.Data, DataSourceType.Cache);
        }
        else if (response.IsResponseFailed())
        {
            Debug.Log("[KINOA] Features Settings metadata get request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            Debug.Log("[KINOA] Features Settings metadata get request was canceled.");
        }

        return response;
    }

    #endregion

    #region Checksums & Long Polling

    /// <summary>
    ///     Gets the server checksums without downloading data.
    ///     Returns all information except the Feature Settings Data.
    /// </summary>
    /// <param name="settingsRequestParams">The Features Settings download request parameters.</param>
    /// <returns>Response with checksums, status, segmentation, scheduling — no data.</returns>
    public async Task<Response<FeaturesSettingsResponse>> GetChecksumsAsync(
        List<FeatureSettingsDownloadRequestParams> settingsRequestParams)
    {
        settingsRequestParams ??= DefaultDownloadParams();

        var response = await Kinoa.FeaturesSettings.GetChecksumsAsync(settingsRequestParams);
        if (response.IsSuccessful())
        {
            LogSettings(response.Data.Settings);
        }
        else if (response.IsConnectionError())
        {
            Debug.Log("[KINOA] Features Settings checksum request failed with connection error.");
        }
        else if (response.IsResponseFailed())
        {
            Debug.Log("[KINOA] Features Settings checksum request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            Debug.Log("[KINOA] Features Settings checksum request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Configures checksum long polling for multiple Features Settings in a single request.
    ///     Avoid tracking the same settings in multiple polling requests.
    ///     Consider using <see cref="DownloadIfChecksumChangedAsync{TData}"/> instead (best practice).
    /// </summary>
    private async void ConfigureChecksumLongPolling(IEnumerable<FeatureSettingsResponse> featuresSettingsToTrack)
    {
        var trackList = featuresSettingsToTrack?
            .Where(x => x.Status == FeatureSettingsResponseStatus.Ok)
            .ToList();

        if (trackList == null || !trackList.Any()) return;

        var cts = new CancellationTokenSource();
        await ConfigureChecksumLongPollingAsync(trackList,
            (checksumChangedFs, requestedFs) =>
                OnFeaturesSettingsChecksumChanged(checksumChangedFs, requestedFs, cts), cts);
    }

    /// <summary>
    ///     Configures checksum long polling with specified settings, callback, and cancellation.
    ///     Cancel old CancellationTokenSource before starting a new one after settings are updated.
    /// </summary>
    /// <param name="featuresSettingsToTrack">Features Settings to track for checksum changes.</param>
    /// <param name="appCallback">Callback invoked when any tracked checksum changes.</param>
    /// <param name="cts">Cancellation token source — cancel to stop polling.</param>
    /// <returns>Response with status 'Success' if the tracker started successfully.</returns>
    private async Task<Response> ConfigureChecksumLongPollingAsync(
        List<FeatureSettingsResponse> featuresSettingsToTrack,
        FeaturesSettingsChecksumChangedCallback appCallback,
        CancellationTokenSource cts)
    {
        const int tickDelayMs = 30 * 1000;
        cts.CancelAfter(90 * 1000);

        var response = await Kinoa.FeaturesSettings.ConfigureChecksumLongPollingAsync(
            featuresSettingsToTrack, appCallback, cts.Token, tickDelayMs);

        var keys = string.Join(", ", featuresSettingsToTrack.Select(x => x.Request.Key));
        Debug.Log(response.IsSuccessful()
            ? $"The '{keys}' checksum long polling is configured successfully."
            : $"Failed to configure the '{keys}' checksum long polling.");

        return response;
    }

    /// <summary>
    ///     Callback when server checksum changes: downloads updated settings, restarts polling.
    /// </summary>
    private async void OnFeaturesSettingsChecksumChanged(
        List<FeatureSettingsResponse> checksumChangedFeaturesSettings,
        List<FeatureSettingsResponse> requestedFeaturesSettings,
        CancellationTokenSource cts)
    {
        foreach (var fs in checksumChangedFeaturesSettings)
            Debug.Log($"The server checksum of '{fs.Request.Key}' FS changed. New checksum: {fs.Checksum}.");

        var settingsRequestParams = checksumChangedFeaturesSettings
            .Select(x => new FeatureSettingsDownloadRequestParams(
                key: x.Request.Key,
                version: Convert.ToUInt16(x.Request.Version),
                getDefault: false,
                compressData: x.Request.CompressData))
            .ToList();

        var response = await DownloadAsync<FeatureSettingsData>(
            settingsRequestParams);

        if (response.IsSuccessful())
        {
            cts.Cancel();
            Debug.Log("Old checksum long polling stopped. Updated settings downloaded successfully.");
            ConfigureChecksumLongPolling(requestedFeaturesSettings);
        }
        else
        {
            Debug.Log("Old checksum long polling continues. Failed to download updated settings.");
        }
    }

    #endregion

    #region Schema Info

    /// <summary>
    ///     Gets information about available Features Settings by schema key.
    ///     Use for dynamic integration — discover new settings without code changes.
    /// </summary>
    /// <param name="schemaKey">The Feature Schema key/name.</param>
    /// <returns>Response with schema info and available Feature Settings (IDs, keys, names, versions).</returns>
    public async Task<Response<SchemaFeaturesSettingsInfo>> GetSchemaFeaturesSettingsInfoAsync(string schemaKey)
    {
        var response = await Kinoa.FeaturesSettings.GetInfoAsync(schemaKey);
        if (response.IsSuccessful() && response.Data != null)
        {
            var sb = new StringBuilder($"Feature Schema ID:{response.Data.Id}, Name:{response.Data.Name}.");
            if (response.Data.Settings.Any())
            {
                sb.Append("\nAvailable Features Settings:");
                foreach (var fs in response.Data.Settings)
                {
                    var versions = fs.Versions.Any() ? string.Join(", ", fs.Versions) : "none";
                    sb.Append($"\n\tId: {fs.Id}, Key: {fs.Key}, Name: {fs.Name}, Versions: {versions}");
                }
            }
            else
            {
                sb.Append("\nNo Features Settings available.");
            }
            Log(sb);
        }
        else
        {
            Debug.Log($"{nameof(Kinoa.FeaturesSettings.GetInfoAsync)} failed: " +
                      $"Status: {response.Status}, Message: {response.Error?.Message}, Code: {response.Error?.Code}");
        }

        return response;
    }

    #endregion

    #region Local Settings Management

    /// <summary>
    ///     Replaces outdated settings in <see cref="LocalFeatureSettings"/> with newly downloaded ones.
    ///     Only settings with Status == Ok are replaced.
    ///     <remarks>
    ///     This method and <see cref="LocalFeatureSettings"/> are for demonstration purposes only.
    ///     In a real game, implement your own settings replacement logic to ensure
    ///     a smooth and seamless gameplay experience when Feature Settings change.
    ///     </remarks>
    /// </summary>
    private void ReplaceFeatureSettings(List<FeatureSettingsResponse<FeatureSettingsData>> settings)
    {
        if (settings == null || !settings.Any()) return;

        var okSettings = settings.Where(x => x.Status == FeatureSettingsResponseStatus.Ok).ToList();
        if (!okSettings.Any()) return;

        LocalFeatureSettings.RemoveAll(x => okSettings.Any(y => y.Request.IsKeyVersionEqualTo(x.Request)));
        LocalFeatureSettings.AddRange(okSettings);

        var info = string.Join("\n", LocalFeatureSettings.Select(x =>
            $"Key: {x.Request.Key}, Version: {x.Request.Version}, Checksum: {x.Checksum}"));
        Debug.Log($"Local Features Settings collection updated:\n{info}");
    }

    #endregion

    #region Default Parameters

    private static List<FeatureSettingsSmartDownloadRequestParams> DefaultSmartDownloadParams() => new()
    {
        new FeatureSettingsSmartDownloadRequestParams(key: "DailyBonus", version: 1, getDefault: false,
            FeatureSettingsFailedDownloadStrategy.GetCachedOrBuiltIn, compressData: false),
        new FeatureSettingsSmartDownloadRequestParams(key: "WheelOfFortune", version: 1, getDefault: false,
            FeatureSettingsFailedDownloadStrategy.GetCachedOrBuiltIn, compressData: false)
    };

    private static List<FeatureSettingsDownloadRequestParams> DefaultDownloadParams() => new()
    {
        new FeatureSettingsDownloadRequestParams(key: "DailyBonus", version: 1, getDefault: false, compressData: false),
        new FeatureSettingsDownloadRequestParams(key: "WheelOfFortune", version: 1, getDefault: false, compressData: false)
    };

    private static List<FeatureSettingsRequestParams> DefaultLocalRequestParams() => new()
    {
        new FeatureSettingsRequestParams(key: "DailyBonus", version: 1),
        new FeatureSettingsRequestParams(key: "WheelOfFortune", version: 1)
    };

    private static CancellationToken EnsureCancellationToken(CancellationToken token)
    {
        if (token != default) return token;
        var cts = new CancellationTokenSource();
        cts.CancelAfter(10 * 1000);
        return cts.Token;
    }

    #endregion

    #region Logging

    /// <summary>
    ///     Logs Feature Settings response info (status, source, key, segmentation, scheduling).
    ///     Demonstrates how to access all FeatureSettingsResponse fields.
    /// </summary>
    private void LogSettings(IEnumerable<FeatureSettingsResponse> settings)
    {
        foreach (var setting in settings)
        {
            var sb = new StringBuilder("Feature Settings received:");
            sb.Append($"\n\tStatus: {setting.Status}, Source: {setting.Source}");
            sb.Append($"\n\tKey: {setting.Request.Key}, Version: {setting.Request.Version}");
            sb.Append($"\n\tConfiguration: {setting.ConfigurationName}, Checksum: {setting.Checksum}");

            if (setting.StartTime > 0)
                sb.Append($"\n\tStartTime: {DateTimeOffset.FromUnixTimeMilliseconds(setting.StartTime.Value):yyyy-MM-dd HH:mm:ss}");
            if (setting.EndTime > 0)
                sb.Append($"\n\tEndTime: {DateTimeOffset.FromUnixTimeMilliseconds(setting.EndTime.Value):yyyy-MM-dd HH:mm:ss}");

            var audiences = setting.Audiences?.Where(x => x.Value).Select(x => x.Key).ToList();
            if (audiences?.Any() == true) sb.Append($"\n\tAudiences: {string.Join(", ", audiences)}");

            var userLists = setting.UserLists?.Where(x => x.Value).Select(x => x.Key).ToList();
            if (userLists?.Any() == true) sb.Append($"\n\tUserLists: {string.Join(", ", userLists)}");

            if (setting.AbTestDistribution != null)
            {
                var ab = setting.AbTestDistribution;
                sb.Append($"\n\tABTest: Name={ab.AbTestName}, Group={ab.AbTestGroupName}, " +
                          $"Id={ab.AbTestId}, GroupId={ab.AbTestGroupId}");
            }

            Log(sb);
        }
    }

    /// <summary>
    ///     Logs Feature Settings data including concrete DTO fields.
    ///     Demonstrates how to access polymorphic data and bundle resources.
    /// </summary>
    private void LogSettingsData(List<FeatureSettingsResponse<FeatureSettingsData>> settings)
    {
        if (settings == null || !settings.Any()) return;

        LogSettings(settings);

        // Access specific Feature Settings by key.
        var dailyBonusSettings = settings.FirstOrDefault(x => x.Request.Key == "DailyBonus");
        if (dailyBonusSettings is { Status: FeatureSettingsResponseStatus.Ok } && dailyBonusSettings.Data.Any())
        {
            var sb = new StringBuilder("\tDaily Bonus Settings:");
            LogFilters(sb, dailyBonusSettings);
            foreach (var data in dailyBonusSettings.Data)
                sb.Append($"\n\tCoins: {((DailyBonusSettings)data).Coins}");
            Log(sb);
        }

        var wheelOfFortuneSettings = settings.FirstOrDefault(x => x.Request.Key == "WheelOfFortune");
        if (wheelOfFortuneSettings is { Status: FeatureSettingsResponseStatus.Ok } && wheelOfFortuneSettings.Data.Any())
        {
            var sb = new StringBuilder("\tWheel of Fortune Settings:");
            LogFilters(sb, wheelOfFortuneSettings);
            foreach (var data in wheelOfFortuneSettings.Data)
            {
                var wof = (WheelOfFortuneSettings)data;
                sb.Append($"\n\tPrize: {wof.Prize}");
                sb.Append($"\n\tCoins: {wof.Coins}");
                LogBundleResources(sb, wof.FooBundleKey, wheelOfFortuneSettings);
            }
            Log(sb);
        }
    }

    /// <summary>
    ///     Logs Filters (Player State values used for configuration matching).
    /// </summary>
    private static void LogFilters(StringBuilder sb, FeatureSettingsResponse<FeatureSettingsData> setting)
    {
        if (setting.Filters == null) return;
        foreach (var filter in setting.Filters)
            sb.Append($"\n\tFilter: {filter.Key} = {filter.Value}");
    }

    /// <summary>
    ///     Logs Feature Settings metadata (key, version, checksum).
    /// </summary>
    private static void LogSettingsMetadata(List<FeatureSettingsMetadataResponse> metadata, DataSourceType source)
    {
        foreach (var data in metadata)
        {
            var checksum = data.Status == FeatureSettingsResponseStatus.Ok && data.Metadata != null
                ? $", Checksum: {data.Metadata.Checksum}"
                : string.Empty;
            Debug.Log($"Feature Settings Metadata: Status: {data.Status}, Source: {source}, " +
                      $"Key: {data.Request.Key}, Version: {data.Request.Version}{checksum}");
        }
    }

    /// <summary>
    ///     Logs bundle resources associated with a Feature Settings entry.
    /// </summary>
    private static void LogBundleResources(StringBuilder sb, string bundleKey,
        FeatureSettingsResponse<FeatureSettingsData> setting)
    {
        if (string.IsNullOrEmpty(bundleKey)) return;
        sb.Append($"\n\tBundle Key: {bundleKey}");

        var bundleResources = setting.BundleResources?.FirstOrDefault(x => x.Key == bundleKey).Value;
        if (bundleResources == null) return;

        foreach (var resource in bundleResources)
            sb.Append($"\n\t\tResource: {resource.ResourceKey} = {resource.Amount}");
    }

    private void Log(StringBuilder builder)
    {
        Debug.Log(builder.ToString());
        builder.Clear();
    }

    #endregion
}
