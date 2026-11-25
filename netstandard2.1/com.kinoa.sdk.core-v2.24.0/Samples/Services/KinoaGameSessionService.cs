using System.Threading;
using System.Threading.Tasks;
using Kinoa.Core.Network.Retry;
using Kinoa.Data;
using Kinoa.Data.Network;
using Kinoa.Data.WebModels;
using UnityEngine;

/// <summary>
///     The sample Game Session service used to manage the Game Session.
/// </summary>
public class KinoaGameSessionService : KinoaSingleton<KinoaGameSessionService>
{
    /// <summary>
    ///     First try: fast fail with limited retries.
    /// </summary>
    private readonly NetworkConfiguration firstTryNetworkConfig = new NetworkConfiguration(
        networkTimeout: 30,
        new RetryConfiguration(
            retryReason: RetryReason.AlwaysRetry,
            retryStrategy: RetryStrategy.Exponential,
            maxRetryAttempts: 1,
            maxRetryDelay: 1));

    /// <summary>
    ///     Background retry: unlimited attempts until success.
    /// </summary>
    private readonly NetworkConfiguration secondTryNetworkConfig = new NetworkConfiguration(
        networkTimeout: 30,
        new RetryConfiguration(
            retryReason: RetryReason.AlwaysRetry,
            retryStrategy: RetryStrategy.Exponential,
            maxRetryAttempts: int.MaxValue,
            maxRetryDelay: 15));

    /// <summary>
    ///     Opens a new Game Session asynchronously.
    ///     <remarks>
    ///     Does not send the Session Start Game Event — call it separately after session open.
    ///     Player State is optional: if null, the server state will be applied.
    ///     The response contains the actualized (merged) Player State.
    ///     </remarks>
    /// </summary>
    /// <param name="gameSessionData">The Game Session Data.</param>
    /// <param name="playerState">The Player State (source of truth). Pass null if unavailable.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="useRetryNetworkConfiguration">Use background retry config after first failed try.</param>
    public async Task<Response<WebPlayerState<CustomPlayerState>>> OpenSessionAsync(
        GameSessionData gameSessionData, CustomPlayerState playerState = null,
        CancellationToken cancellationToken = default,
        bool useRetryNetworkConfiguration = false)
    {
        var gameSession = gameSessionData ?? new GameSessionData();
        var networkConfig =
            useRetryNetworkConfiguration ? secondTryNetworkConfig : firstTryNetworkConfig;

        var response = await Kinoa.GameSession.OpenSessionAsync(
            gameSession, playerState, networkConfig, cancellationToken);

        if (response.IsSuccessful())
        {
            Debug.Log("Game Session successfully opened.");
            if (response.Data != null)
            {
                Debug.Log("Server Player State received.");
                KinoaPlayerStateService.Instance.PlayerState = response.Data.PlayerState;
            }
        }
        else
        {
            Debug.Log($"Game Session opening failed: {response.Error?.Code}. " +
                      $"Status: {response.Status}");
        }

        return response;
    }
}
