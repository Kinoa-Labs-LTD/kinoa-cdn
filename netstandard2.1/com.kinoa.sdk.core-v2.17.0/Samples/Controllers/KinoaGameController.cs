using System.Collections.Generic;
using System.Threading.Tasks;
using Kinoa.Data;
using Kinoa.Data.Enum;
using Kinoa.Data.FeaturesSettings;
using Kinoa.Data.FeaturesSettings.Enum;
using Kinoa.Data.Translations;
using UnityEngine;

/// <summary>
///     Kinoa Game Controller — SDK initialization, session lifecycle, and startup flow.
///     <para>Two integration paths — pick the one that matches your game's bootstrap pattern:</para>
///     <list type="bullet">
///         <item><b>MonoBehaviour scene-attached</b> (default) — keep <c>: MonoBehaviour</c>; attach
///             this component to a GameObject in your bootstrap scene. Unity invokes <see cref="Start"/>
///             automatically, which calls <see cref="InitializeAndOpenSessionAsync"/>.</item>
///         <item><b>Non-MonoBehaviour singleton</b> — drop <c>: MonoBehaviour</c>, drop the
///             <see cref="Start"/> method, drop the <see cref="overlay"/> field, and call
///             <see cref="InitializeAndOpenSessionAsync"/> directly from your existing bootstrap code
///             (e.g., <c>LoadingController</c>, <c>AppController</c>, <c>BootstrapInstaller</c>).
///             Recommended when your game's bootstrap is plain C# / DI / service-locator rather than
///             scene-attached MonoBehaviours.</item>
///     </list>
/// </summary>
public class KinoaGameController : MonoBehaviour
//public class KinoaGameController : KinoaSingleton<KinoaGameController> // Non-MonoBehaviour singleton path — see class summary.
{
    /// <summary>
    ///     Sample loading overlay — displayed during SDK initialization. Replace with your own UI.
    ///     Drop this field if migrating to non-MonoBehaviour singleton.
    /// </summary>
    public GameObject overlay;

    /// <summary>
    ///     Unity lifecycle entry point. Drop this method if migrating to non-MonoBehaviour singleton
    ///     (call <see cref="InitializeAndOpenSessionAsync"/> directly from your bootstrap instead).
    /// </summary>
    private async void Start()
    {
        await InitializeAndOpenSessionAsync();
    }

    /// <summary>
    ///     Full Kinoa startup flow — SDK + Messaging init, login, state, session open + session_start,
    ///     Feature Settings and Translations downloads (in parallel), background retry. Idempotent:
    ///     returns early if SDK is already initialized.
    /// </summary>
    public async Task InitializeAndOpenSessionAsync()
    {
        if (KinoaSdkInitService.Instance.IsInitialized)
            return;

        using (new KinoaOverlay(overlay))
        {
            await InitializeServicesAsync();
            await Task.WhenAll(LogInAndOpenSessionAsync(), DownloadTranslationsAsync());
        }
    }

    /// <summary>
    ///     Initializes the Kinoa SDK and In-app Messaging services.
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        await KinoaSdkInitService.Instance.InitializeAsync();
        await KinoaMessagingService.Instance.InitializeAsync();
    }

    /// <summary>
    ///     Logs in, opens the game session, downloads feature settings, and ensures session stability.
    /// </summary>
    public async Task LogInAndOpenSessionAsync()
    {
        // Step 1 — Set Active Player ID (Log in).
        await KinoaPlayerAccountService.Instance.LogInPlayer();

        // Step 2 — Get local Player State.
        var playerState = await KinoaPlayerStateService.Instance.GetPlayerStateAsync();

        // Step 3 — Open game session.
        await OpenGameSessionAsync(playerState, new GameSessionData());

        // Step 4 (optional) — Download Feature Settings.
        await DownloadFeatureSettingsAsync();

        // Step 5 — Background retry if session open failed.
        EnsureGameSessionOpen();
    }

    /// <summary>
    ///     Opens the game session and sends session_start 
    ///     (Sync API — inbox state included in response).
    /// </summary>
    /// <param name="playerState">Local Player State (source of truth).</param>
    /// <param name="gameSessionData">New for first try; <see cref="Kinoa.GameSession.ActiveSession"/> for retry.</param>
    /// <param name="useRetryNetworkConfiguration">Use background retry config (unlimited attempts) instead of fast-fail.</param>
    private async Task OpenGameSessionAsync(
        CustomPlayerState playerState, GameSessionData gameSessionData,
        bool useRetryNetworkConfiguration = false)
    {
        var response = await KinoaGameSessionService.Instance.OpenSessionAsync(
            gameSessionData, playerState, useRetryNetworkConfiguration: useRetryNetworkConfiguration);

        if (response.IsSuccessful())
        {
            await KinoaSyncGameEventsService.Instance.SendSessionStartEventAsync();
        }
    }

    /// <summary>
    ///     Background retry — reopens the session with the same Session ID if the first attempt failed.
    ///     Uses <see cref="Kinoa.GameSession.ActiveSession"/> to preserve the Session ID across retries.
    /// </summary>
    private async void EnsureGameSessionOpen()
    {
        var gameSession = Kinoa.GameSession.ActiveSession ?? new GameSessionData();
        if (gameSession.IsOpened)
            return;

        Debug.Log("[KINOA] Game session not open, retrying...");
        await OpenGameSessionAsync(
            KinoaPlayerStateService.Instance.PlayerState, gameSession,
            useRetryNetworkConfiguration: true);
    }

    /// <summary>
    ///     Downloads Feature Settings after the session is opened — the player's audience may
    ///     shift on session open, so the response reflects the current audience.
    ///     TODO: Replace keys and versions with your actual Feature Settings from the Kinoa Dashboard.
    /// </summary>
    private async Task DownloadFeatureSettingsAsync()
    {
        var requestParams = new List<FeatureSettingsSmartDownloadRequestParams>
        {
            new FeatureSettingsSmartDownloadRequestParams(
                key: "DailyBonus", version: 1, getDefault: false,
                FeatureSettingsFailedDownloadStrategy.GetCachedOrBuiltIn, compressData: false),
            new FeatureSettingsSmartDownloadRequestParams(
                key: "WheelOfFortune", version: 1, getDefault: false,
                FeatureSettingsFailedDownloadStrategy.GetCachedOrBuiltIn, compressData: false)
        };

        await KinoaFeaturesSettingsService.Instance.SmartDownloadAsync<FeatureSettingsData>(requestParams);
    }

    /// <summary>
    ///     Downloads Translations. Requires SDK initialization but no player session.
    ///     TODO: Replace language and group keys with your actual Translations from the Kinoa Dashboard.
    /// </summary>
    private async Task DownloadTranslationsAsync()
    {
        var requestParams = new List<TranslationDownloadRequest>
        {
            new TranslationDownloadRequest(Language.English, new Dictionary<string, TranslationGroupRequest>
            {
                // string.Empty is the default group key for the Dashboard rows with no group specified.
                { string.Empty, new TranslationGroupRequest() },
                { "ui", new TranslationGroupRequest() },
                { "store", new TranslationGroupRequest() }
            })
        };

        await KinoaTranslationsService.Instance.SmartDownloadAsync(requestParams);
    }
}
