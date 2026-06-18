using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kinoa.Core.Network;
using Kinoa.Data;
using Kinoa.Data.State;
using UnityEngine;

/// <summary>
///     The sample Player Account service used to manage the Player accounts.
/// </summary>
public class KinoaPlayerAccountService : KinoaSingleton<KinoaPlayerAccountService>
{
    /// <summary>
    ///     Gets and sets an Active Player identifier <see cref="Kinoa.Player.ID"/>.
    /// </summary>
    public string ActivePlayerID
    {
        get => Kinoa.Player.ID;
        set => Kinoa.Player.ID = value;
    }

    /// <summary>
    ///     Log in to the Player account.
    /// </summary>
    /// <param name="useKinoaRecovery">Use Kinoa related accounts lookup as fallback. Default: false.</param>
    public async Task LogInPlayer(bool useKinoaRecovery = false)
    {
        //TODO: Identify the active Player ID using the internal login mechanism.
        var loggedInPlayerId = GetLoggedInPlayerId();
        if (!string.IsNullOrEmpty(loggedInPlayerId))
        {
            if (loggedInPlayerId == ActivePlayerID)
            {
                Debug.Log($"The Player from the last Game Session is already logged in: {loggedInPlayerId}");
                return;
            }

            Debug.Log($"The Active Player ID is updated: Old: {ActivePlayerID} -> New: {loggedInPlayerId}");
            ActivePlayerID = loggedInPlayerId;
            return;
        }

        //TODO: Set useKinoaRecovery to true only if your game has no own Player ID recovery mechanism.
        if (useKinoaRecovery)
        {
            await LogInPlayerByRelatedAccountsAsync();
            return;
        }

        //TODO: Implement the new Player ID generation logic.
        // Recommended to use your Player ID as Kinoa Player ID.
        Debug.Log("The Active Player ID is not identified: The new Player is logged in.");
        ActivePlayerID = Guid.NewGuid().ToString();
    }

    /// <summary>
    ///     Gets the logged in Player ID based on the game login data.
    /// </summary>
    /// <returns>The logged in Player ID.</returns>
    private string GetLoggedInPlayerId()
    {
        //TODO: Identify the active Player ID based on your internal game data
        //      e.g., using the PlayerPrefs or any other login mechanisms.
        var loggedInPlayerId = PlayerPrefs.GetString("ActivePlayerID", null);

        //TODO: You can use the Kinoa.Player.ID as a source of the last Active Player ID
        //      across the game launches.
        return !string.IsNullOrEmpty(loggedInPlayerId) ? loggedInPlayerId : Kinoa.Player.ID;
    }

    /// <summary>
    ///     Deletes the Kinoa Player.
    /// </summary>
    /// <param name="playerID">The Player ID to delete.</param>
    /// <returns>The response object.</returns>
    public Task<Response> DeletePlayerAsync(string playerID)
    {
        return Kinoa.Player.DeletePlayerAsync(playerID);
    }

    /// <summary>
    ///     Log in to the Player account by the Kinoa related accounts asynchronously.
    ///     <remarks>
    ///     Use the related accounts to identify the logged in Player only if the Active Player ID
    ///     cannot be identified with the game login mechanism and the game has no own Player ID recovery.
    ///     </remarks>
    /// </summary>
    public async Task LogInPlayerByRelatedAccountsAsync()
    {
        var response = await GetRelatedAccountsAsync();
        if (response.IsSuccessful())
        {
            var playerRelatedAccounts = response.Data;
            if (playerRelatedAccounts != null && playerRelatedAccounts.Count > 0)
            {
                //TODO: Implement the logic to choose the logged in Player among the related accounts.
                var loggedInPlayer = playerRelatedAccounts[0].PlayerId;
                Debug.Log($"The Active Player is successfully logged in: {loggedInPlayer}");
                ActivePlayerID = loggedInPlayer;
            }
            else
            {
                //TODO: Implement the new Player ID generation logic.
                // Recommended to use your Player ID as Kinoa Player ID.
                Debug.Log("The Active Player ID is not identified: No related accounts found.");
                ActivePlayerID = Guid.NewGuid().ToString();
            }
        }
        else if (response.IsConnectionError())
        {
            //TODO: Process the failed request e.g., show a connection error dialog to the client.
            //TODO: ActivePlayerID must be set before proceeding — all SDK requests will be rejected without it.
            Debug.LogError("The Active Player ID is not identified: Connection error.");
        }
        else
        {
            //TODO: Process the failed request e.g., show an error message to the client.
            //TODO: ActivePlayerID must be set before proceeding — all SDK requests will be rejected without it.
            Debug.LogError("The Active Player ID is not identified: Get related accounts request failed.");
        }
    }

    /// <summary>
    ///     Gets all player-related accounts asynchronously.
    /// </summary>
    /// <param name="playerSearchIDs">The known Player IDs.</param>
    /// <returns>The Player related accounts.</returns>
    public async Task<Response<List<PlayerRelatedAccount>>> GetRelatedAccountsAsync(
        PlayerSearchIDs playerSearchIDs = null)
    {
        var response = await Kinoa.Player.GetRelatedAccountsAsync(
            playerSearchIDs ?? new PlayerSearchIDs(ActivePlayerID));

        Debug.Log($"All player-related accounts received successfully: {response.IsSuccessful()}.\n" +
                  $"Related accounts: {response.Data?.Count}");

        return response;
    }
}
