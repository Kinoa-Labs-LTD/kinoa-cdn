using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kinoa.Core.Callbacks;
using Kinoa.Data;
using Kinoa.Data.Enum;
using Kinoa.Data.Translations;
using UnityEngine;

/// <summary>
///     Kinoa Translations service.
/// </summary>
public class KinoaTranslationsService : KinoaSingleton<KinoaTranslationsService>
{
    /// <summary>
    ///     The sample client-side Translations collection.
    /// </summary>
    public List<TranslationLanguageResponse> LocalTranslations { get; private set; } =
        new List<TranslationLanguageResponse>();

    #region Download Methods

    /// <summary>
    ///     Downloads Translations incrementally — only changed or missing groups are fetched from the server;
    ///     unchanged groups are served from the local cache. Groups not in the request are preserved in cache.
    /// </summary>
    /// <param name="requestParams">The translations request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onProgress">Callback function that is invoked on download progress change.</param>
    /// <returns>The requested Translations type of <see cref="TranslationsResponse"/>.</returns>
    public async Task<Response<TranslationsResponse>> SmartDownloadAsync(
        List<TranslationDownloadRequest> requestParams,
        CancellationToken cancellationToken = default, ProgressChangedCallback onProgress = null)
    {
        requestParams ??= DefaultRequestParams();
        cancellationToken = EnsureCancellationToken(cancellationToken);

        var response = await Kinoa.Translations.SmartDownloadAsync(
            requestParams, cancellationToken, onProgress ?? OnDownloadProgressChanged);

        // Apply successfully loaded Translations to the local collection.
        ReplaceTranslations(response.Data?.Translations);

        if (response.IsSuccessful())
        {
            Debug.Log("[KINOA] Translations smart download request was successful.");
            LogTranslations(response.Data?.Translations);
        }
        else if (response.IsConnectionError())
        {
            //TODO: Connection error handling.
            Debug.Log("[KINOA] Translations smart download request failed with connection error.");
        }
        else if (response.IsResponseFailed())
        {
            //TODO: Bad response handling.
            Debug.LogError("[KINOA] Translations smart download request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            //TODO: Cancellation handling.
            Debug.Log("[KINOA] Translations smart download request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Downloads Translations by the provided request parameters asynchronously.
    ///     Fully overwrites the local cache — groups not in the request are removed.
    ///     Consider using <see cref="SmartDownloadAsync"/> instead (best practice).
    /// </summary>
    /// <param name="requestParams">The translations request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onProgress">Callback function that is invoked on download progress change.</param>
    /// <returns>The requested Translations type of <see cref="TranslationsResponse"/>.</returns>
    public async Task<Response<TranslationsResponse>> DownloadAsync(
        List<TranslationDownloadRequest> requestParams,
        CancellationToken cancellationToken = default, ProgressChangedCallback onProgress = null)
    {
        requestParams ??= DefaultRequestParams();
        cancellationToken = EnsureCancellationToken(cancellationToken);

        var response = await Kinoa.Translations.DownloadAsync(
            requestParams, cancellationToken, onProgress ?? OnDownloadProgressChanged);

        // Apply successfully loaded Translations to the local collection.
        ReplaceTranslations(response.Data.Translations);

        if (response.IsSuccessful())
        {
            Debug.Log("[KINOA] Translations download request was successful.");
            LogTranslations(response.Data.Translations);
        }
        else if (response.IsConnectionError())
        {
            //TODO: Connection error handling.
            Debug.Log("[KINOA] Translations download request failed with connection error.");
        }
        else if (response.IsResponseFailed())
        {
            //TODO: Bad response handling.
            Debug.LogError("[KINOA] Translations download request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            //TODO: Cancellation handling.
            Debug.Log("[KINOA] Translations download request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Gets the cached translations by the provided request parameters asynchronously.
    ///     No network calls — returns only cached data.
    /// </summary>
    /// <param name="requestParams">The translations request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested Translations type of <see cref="TranslationsResponse"/>.</returns>
    public async Task<Response<TranslationsResponse>> GetCachedAsync(
        List<TranslationDownloadRequest> requestParams,
        CancellationToken cancellationToken = default)
    {
        requestParams ??= DefaultRequestParams();
        cancellationToken = EnsureCancellationToken(cancellationToken);

        var response = await Kinoa.Translations.GetCachedAsync(requestParams, cancellationToken);

        // Apply successfully loaded Translations to the local collection.
        ReplaceTranslations(response.Data.Translations);

        if (response.IsSuccessful())
        {
            Debug.Log("[KINOA] Cached Translations were loaded successfully.");
            LogTranslations(response.Data.Translations);
        }
        else if (response.IsResponseFailed())
        {
            //TODO: Bad response handling.
            Debug.LogError("[KINOA] Cached Translations request failed with error.");
        }
        else if (response.IsResponseCanceled())
        {
            //TODO: Cancellation handling.
            Debug.Log("[KINOA] Cached Translations request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     On Translations download progress changed.
    /// </summary>
    private static void OnDownloadProgressChanged(decimal progress)
    {
        Debug.Log($"[KINOA] Translations download progress: {progress}");
    }

    #endregion

    #region Defaults

    /// <summary>
    ///     Gets default translation request parameters.
    ///     TODO: Replace with your actual languages and group keys defined on the Kinoa Dashboard.
    /// </summary>
    private static List<TranslationDownloadRequest> DefaultRequestParams()
    {
        return new List<TranslationDownloadRequest>
        {
            new(Language.English, new Dictionary<string, TranslationGroupRequest>
            {
                // string.Empty is the default group key for the Dashboard rows with no group specified.
                { string.Empty, new TranslationGroupRequest() },
                { "ui", new TranslationGroupRequest() },
                { "store", new TranslationGroupRequest() }
            })
        };
    }

    /// <summary>
    ///     Ensures a valid CancellationToken (default: 10s timeout).
    /// </summary>
    private static CancellationToken EnsureCancellationToken(CancellationToken token)
    {
        if (token != default) return token;
        var cts = new CancellationTokenSource();
        cts.CancelAfter(10 * 1000);
        return cts.Token;
    }

    #endregion

    #region Local Collection

    /// <summary>
    ///     Replaces Ok translations in the sample local collection <see cref="LocalTranslations"/>,
    ///     preserving entries for languages not present in the response.
    /// </summary>
    private void ReplaceTranslations(List<TranslationLanguageResponse> translations)
    {
        var okTranslations = translations?
            .Where(x => x.Status == TranslationResponseStatus.Ok)
            .ToList();
        if (okTranslations == null || !okTranslations.Any()) return;

        LocalTranslations.RemoveAll(x => okTranslations.Any(y => y.Language == x.Language));
        LocalTranslations.AddRange(okTranslations);

        var info = string.Join("\n\t", LocalTranslations.Select(x =>
            $"{x.Language}: {x.Groups?.Count ?? 0} group(s)"));
        Debug.Log($"[KINOA] Local Translations updated:\n\t{info}");
    }

    #endregion

    #region Logging

    /// <summary>
    ///     Logs the requested Translations information including the Data.
    /// </summary>
    /// <param name="translations">The Translations collection.</param>
    private static void LogTranslations(List<TranslationLanguageResponse> translations)
    {
        if (translations == null || !translations.Any())
        {
            Debug.Log("No Translations found.");
            return;
        }

        foreach (var translation in translations)
        {
            var log = new StringBuilder();
            log.AppendFormat($"Translation received for language: {translation.Language}");
            log.AppendFormat($"\nStatus: {translation.Status}");
            log.AppendFormat($"\nGroups count: {translation.Groups.Count}");

            if (translation.Status == TranslationResponseStatus.Ok && translation.Groups.Any())
            {
                foreach (var group in translation.Groups)
                {
                    log.AppendFormat($"\n\tGroup: {group.Key}");
                    log.AppendFormat($"\n\t\tStatus: {group.Value.Status}");
                    log.AppendFormat($"\n\t\tSource: {group.Value.Source}");

                    if (group.Value.Status == TranslationResponseStatus.Ok)
                    {
                        var translationCount = group.Value.Data?.Count ?? 0;
                        log.AppendFormat($"\n\t\tTranslation keys count: {translationCount}");

                        if (translationCount > 0)
                        {
                            // Log first few translation examples
                            var examples = group.Value.Data.Take(3);
                            foreach (var example in examples)
                            {
                                log.AppendFormat($"\n\t\t\t{example.Key}: {example.Value}");
                            }
                            if (translationCount > 3)
                            {
                                log.AppendFormat($"\n\t\t\t... and {translationCount - 3} more");
                            }
                        }
                    }
                }
            }

            Log(log);
        }
    }

    private static void Log(StringBuilder builder)
    {
        Debug.Log(builder.ToString());
        builder.Clear();
    }

    #endregion
}
