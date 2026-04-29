using System;
using System.Threading.Tasks;
using Kinoa.Data;
using Kinoa.Data.Enum;
using Kinoa.Data.State;
using Kinoa.Data.SyncGameEvents;
using Kinoa.Data.WebModels;
using UnityEngine;

/// <summary>
///     The sample Player State service used to manage the Player data.
/// </summary>
public class KinoaPlayerStateService : KinoaSingleton<KinoaPlayerStateService>
{
    /// <summary>
    ///     The local Player State set event.
    /// </summary>
    public event Action<bool> OnLocalPlayerStateSet;

    /// <summary>
    ///     The actual Player State.
    /// </summary>
    public CustomPlayerState PlayerState { get; set; }

    /// <summary>
    ///     Initializes a new instance. Registers the operator state change handler.
    /// </summary>
    public KinoaPlayerStateService()
    {
        Kinoa.Player.SetStateChangedByOperatorHandler(OnPlayerStateChangedByOperator);
    }

    /// <summary>
    ///     Notifies that the local Player State has been set.
    /// </summary>
    public void LocalPlayerStateSet(bool isSet)
    {
        OnLocalPlayerStateSet?.Invoke(isSet);
    }

    /// <summary>
    ///     Gets the actual Player State of the active Player asynchronously.
    ///     <remarks>
    ///     Returns the local (source of truth) Player State.
    ///     If null, the server state will be applied on the first session open request.
    ///     </remarks>
    /// </summary>
    public async Task<CustomPlayerState> GetPlayerStateAsync()
    {
        Debug.Log("The local Player State will be used to open session. " +
                  "If null, the server's Player State will be applied automatically.");

        PlayerState = await GetLocalPlayerStateAsync();
        LocalPlayerStateSet(true);

        return PlayerState;
    }

    /// <summary>
    ///     Gets the local Player State asynchronously.
    /// </summary>
    private async Task<CustomPlayerState> GetLocalPlayerStateAsync()
    {
        //TODO: Return the Player State from your local storage (source of truth).
        //      Passing null is fine — Kinoa will use the server state on session open.
        return await Task.FromResult(PlayerState);
    }

    /// <summary>
    ///     Gets the Kinoa server Player State by the Player ID asynchronously.
    ///     <remarks>
    ///     In most cases you don't need this method — OpenSessionAsync already returns
    ///     the server Player State in the response.
    ///     </remarks>
    /// </summary>
    /// <param name="playerId">The Player ID.</param>
    public async Task<Response<CustomPlayerState>> GetServerPlayerStateAsync(string playerId)
    {
        var response = await Kinoa.Player.GetStateAsync<CustomPlayerState>(playerId);
        LogPlayerState(response);
        return response;
    }

    /// <summary>
    ///     Resets the Player State (async, fire-and-forget).
    /// </summary>
    public void ResetPlayerState(CustomPlayerState playerState)
    {
        Kinoa.GameEvents.SendResetPlayerStateEvent(playerState);
    }

    /// <summary>
    ///     Resets the Player State (sync, returns response).
    /// </summary>
    public Task<Response<SyncGameEventResponse>> ResetPlayerStateAsync(CustomPlayerState playerState)
    {
        return Kinoa.SyncGameEvents.SendResetPlayerStateEventAsync(playerState);
    }

    /// <summary>
    ///     Approves the Player State operator changes. Unblocks the future Game Events.
    ///     See <see cref="OnPlayerStateChangedByOperator"/>.
    /// </summary>
    public async Task<Response> ApprovePlayerStateChangesAsync()
    {
        var response = await Kinoa.Player.ApproveStateChangesAsync();
        Debug.Log($"The Player State operator changes are approved: {response.IsSuccessful()}.");
        return response;
    }

    /// <summary>
    ///     Triggered when the Player State is changed by an operator via Dashboard.
    ///     Must approve changes before sending new Game Events.
    /// </summary>
    private async void OnPlayerStateChangedByOperator(WebPlayerStateJsonNode actualServerPlayerState)
    {
        Debug.Log("The Player State changed by an Operator.");

        //TODO: Implement merging strategy of local and remote player states.
        // In current sample we take remote player state as actual, ignoring local changes.

        PlayerState = await actualServerPlayerState.GetPlayerState<CustomPlayerState>();

        // Approve Player State changes after merging.
        await ApprovePlayerStateChangesAsync();

        // Reset remote Player State with actual Player State.
        ResetPlayerState(PlayerState);
    }

    /// <summary>
    ///     Sets preferred language for localization.
    /// </summary>
    public void SetLocalizationLanguage(Language language)
    {
        PlayerState?.SetLanguageCode(language);
    }

    /// <summary>
    ///     Sets install time.
    /// </summary>
    public void SetInstallTime(long timeMs)
    {
        PlayerState?.SetInstallTime(timeMs);
    }

    /// <summary>
    ///     Logs the Player State object in console.
    /// </summary>
    private void LogPlayerState(Response<CustomPlayerState> playerStateResponse)
    {
        var playerState = playerStateResponse.Data;
        Debug.Log($"Player State received: {playerStateResponse.IsSuccessful()}.\n" +
                  $"Player ID: \"{playerState.PlayerIdentifiers.PlayerId}\".");

        if (!playerState.IsRegistered)
        {
            Debug.Log("Player is not registered in Kinoa. " +
                      "Will be created after first session open request.");
        }
    }

    // --- PlayerStateDictionary alternative ---
    // Instead of CustomPlayerState you can use PlayerStateDictionary (no predefined schema).
    //
    // Replace CustomPlayerState → PlayerStateDictionary in: 
    // KinoaPlayerStateService, KinoaGameSessionService, KinoaGameEventBuildingService, KinoaGameController.
    //
    // Example LogPlayerState for dictionary-based state:
    //private void LogPlayerState(Response<PlayerStateDictionary> playerStateResponse)
    //{
    //    var playerState = playerStateResponse.Data;
    //    if (playerState.TryGetValue("player_identifiers", out var value)
    //        && value is Dictionary<string, object> identifiers)
    //    {
    //        Debug.Log($"Player State received: {playerStateResponse.IsSuccessful()}.\n" +
    //                  $"Player ID: \"{identifiers["player_id"]}\".");
    //    }
    //
    //    if (playerState.Count == 0)
    //    {
    //        Debug.Log("Player is not registered in Kinoa. " +
    //                  "Will be created after first session open request.");
    //    }
    //}
}
