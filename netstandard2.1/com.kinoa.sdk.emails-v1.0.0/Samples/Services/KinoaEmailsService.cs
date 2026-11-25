using System.Collections.Generic;
using System.Threading.Tasks;
using Kinoa.Data;
using Kinoa.Emails.Data;
using UnityEngine;

/// <summary>
///     Shows how to use the Kinoa Emails module.
///     Call <see cref="Initialize"/> AFTER Kinoa.SDK.Initialize() has completed.
///     Register/Delete requests return a "not initialized" bad response until the module is initialized.
/// </summary>
public class KinoaEmailsService : KinoaSingleton<KinoaEmailsService>
{
    /// <summary>
    ///     Initializes the Emails module.
    /// </summary>
    /// <returns>True when the module is ready; false when the Core SDK is not initialized yet.</returns>
    public bool Initialize()
    {
        var initialized = Kinoa.Emails.SDK.Initialize();
        if (initialized)
            Debug.Log("[KINOA] Emails module initialized.");
        else
            Debug.LogWarning("[KINOA] Emails module NOT initialized: the Core SDK is not initialized. " +
                             "Call after Kinoa.SDK.Initialize() has completed.");

        return initialized;
    }

    /// <summary>
    ///     Registers the player email on Kinoa.
    /// </summary>
    /// <param name="email">Player email address.</param>
    /// <param name="fields">Additional player fields (e.g. "first_name"). Keys and values are sent as is. Optional.</param>
    /// <param name="playerId">Kinoa Player ID. The active player is used if null.</param>
    /// <returns>Processed API response with the registration result.</returns>
    public async Task<Response<KinoaEmailRegistrationResult>> RegisterEmailAsync(string email,
        Dictionary<string, string> fields = null, string playerId = null)
    {
        var response = await Kinoa.Emails.SDK.RegisterEmail(playerId, email, fields);
        if (response.IsSuccessful())
            Debug.Log($"[KINOA] Email registered: status {response.Data?.Status}.");
        else
            Debug.LogWarning($"[KINOA] Email registration failed: {response.Status}. {response.Error?.Message}");

        return response;
    }

    /// <summary>
    ///     Deletes the player email on Kinoa.
    /// </summary>
    /// <param name="playerId">Kinoa Player ID. The active player is used if null.</param>
    /// <returns>Processed API response.</returns>
    public async Task<Response> DeleteEmailAsync(string playerId = null)
    {
        var response = await Kinoa.Emails.SDK.DeleteEmail(playerId);
        if (response.IsSuccessful())
            Debug.Log("[KINOA] Email deleted.");
        else
            Debug.LogWarning($"[KINOA] Email deletion failed: {response.Status}. {response.Error?.Message}");

        return response;
    }
}
