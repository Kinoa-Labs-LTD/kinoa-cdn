using System.Collections.Generic;
using System.Linq;
using Kinoa.Data;
using Kinoa.Data.P2PEvents;
using UnityEngine;

/// <summary>
///     Kinoa P2P (Player-to-Player) events service.
/// </summary>
public class KinoaP2PEventsService : KinoaSingleton<KinoaP2PEventsService>
{
    /// <summary>
    ///     The incoming P2P events collection.
    /// </summary>
    private List<IncomingP2PEvent> incomingP2PEvents = new List<IncomingP2PEvent>();

    /// <summary>
    ///     Sample P2P event data — "attack" that decrements 11 health units.
    ///     TODO: Replace with your actual event data class.
    /// </summary>
    private class MockedEventData
    {
        public int Amount { get; set; } = -11;
        public string Resource { get; set; } = "health";
        public string Icon { get; set; } = "https://example.com/icon.png";
    }

    #region API

    /// <summary>
    ///     Sends a P2P event to another player.
    /// </summary>
    /// <param name="targetPlayerID">Target Kinoa Player ID.</param>
    public void Send(string targetPlayerID)
    {
        var eventData = new MockedEventData();
        Kinoa.P2PEvents.Send(new OutgoingP2PEvent(targetPlayerID, "mocked_event", eventData));
    }

    /// <summary>
    ///     Gets the current list of incoming P2P events for the active player.
    /// </summary>
    public void Get()
    {
        Kinoa.P2PEvents.Get(OnP2PEventsReceived);
    }

    /// <summary>
    ///     Deletes P2P events by ID. Call after processing to prevent duplicates.
    /// </summary>
    /// <param name="eventIDs">IDs of processed events to delete. If null, deletes all received events.</param>
    public void Delete(List<string> eventIDs = null)
    {
        eventIDs ??= incomingP2PEvents.Select(e => e.ID).ToList();
        Kinoa.P2PEvents.Delete(eventIDs, OnP2PEventsDeleted);
    }

    #endregion

    #region Callbacks

    /// <summary>
    ///     Callback for received P2P events.
    /// </summary>
    /// <param name="response">API response with the incoming P2P events collection.</param>
    private void OnP2PEventsReceived(Response<List<IncomingP2PEvent>> response)
    {
        if (!response.IsSuccessful())
        {
            Debug.LogError("[KINOA] Failed to get P2P events.");
            return;
        }

        incomingP2PEvents = response.Data ?? new List<IncomingP2PEvent>();
        Debug.Log($"[KINOA] P2P events received: {incomingP2PEvents.Count}");

        //TODO: Process events and update Player State.

        // Delete processed events to prevent duplicates on next Get().
        Delete(incomingP2PEvents.Select(e => e.ID).ToList());

        //TODO: Send a game event to sync the updated state with the server.
    }

    /// <summary>
    ///     Callback for P2P events deletion.
    /// </summary>
    /// <param name="response">API response.</param>
    private void OnP2PEventsDeleted(Response response)
    {
        if (!response.IsSuccessful())
        {
            Debug.LogError("[KINOA] Failed to delete P2P events.");
            return;
        }

        incomingP2PEvents.Clear();
        Debug.Log("[KINOA] P2P events deleted.");
    }

    #endregion
}
