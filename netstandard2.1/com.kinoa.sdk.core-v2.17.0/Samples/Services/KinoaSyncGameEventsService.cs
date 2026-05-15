using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Services;
using Kinoa.Data;
using Kinoa.Data.Enum;
using Kinoa.Data.Messaging.InApp;
using Kinoa.Data.State;
using Kinoa.Data.SyncGameEvents;

/// <summary>
///     Kinoa Synchronous Game Events service.
///     All methods return <see cref="Response{T}"/> with <see cref="SyncGameEventResponse"/>
///     containing In-App inbox state for processing in-app messages at the place of execution.
/// </summary>
public class KinoaSyncGameEventsService : KinoaGameEventBuildingService<KinoaSyncGameEventsService>
{
    #region Predefined Events (mandatory)

    /// <summary>
    ///     Sends Session Start Game Event. Mandatory.
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendSessionStartEventAsync()
    {
        var e = OnGameSessionStart();
        var response = await Kinoa.SyncGameEvents.SendSessionStartEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Payment Game Event. Mandatory.
    /// </summary>
    /// <param name="productId">Shop product identifier in market. Example: "play-market-product-id".</param>
    /// <param name="spent">Spent real money amount. Example: 0.99m.</param>
    /// <param name="isoCurrencyCode">Real money currency in ISO 4217 format. Example: "USD".</param>
    /// <param name="received">Received in-game resources.</param>
    /// <param name="inAppMessage">In-app message that triggered the purchase.</param>
    public async Task<Response<SyncGameEventResponse>> SendPaymentEventAsync(
        string productId,
        decimal spent,
        string isoCurrencyCode,
        Dictionary<string, decimal> received,
        InAppMessage inAppMessage)
    {
        var e = OnPayment(productId, spent, isoCurrencyCode, received, inAppMessage);
        var response = await Kinoa.SyncGameEvents.SendPaymentEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    #endregion

    #region Predefined Events (optional)

    /// <summary>
    ///     Sends the Progression Game Event.
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendProgressionEventAsync()
    {
        var e = OnProgression();
        var response = await Kinoa.SyncGameEvents.SendProgressionEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Level Up Game Event.
    /// </summary>
    /// <param name="customParams">Custom event custom parameters.</param>
    public async Task<Response<SyncGameEventResponse>> SendLevelUpEventAsync(
        Dictionary<string, object> customParams = null)
    {
        var e = OnLevelUp(customParams);
        var response = await Kinoa.SyncGameEvents.SendLevelUpEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Watch Ad Game Event.
    /// </summary>
    /// <param name="incomeFromAd">Real money income from ad. Example: 40m.</param>
    /// <param name="isoCurrencyCode">Real money currency in ISO 4217 format. Example: "UAH".</param>
    /// <param name="adType">Ad type. Example: "simple_ad".</param>
    /// <param name="received">Received in-game resources.</param>
    /// <param name="inAppMessage">In-app message that triggered the ad.</param>
    public async Task<Response<SyncGameEventResponse>> SendWatchAdEventAsync(
        decimal incomeFromAd,
        string isoCurrencyCode,
        string adType,
        Dictionary<string, decimal> received,
        InAppMessage inAppMessage)
    {
        var e = OnWatchAd(incomeFromAd, isoCurrencyCode, adType, received, inAppMessage);
        var response = await Kinoa.SyncGameEvents.SendWatchAdEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the In-game Purchase Game Event.
    /// </summary>
    /// <param name="spent">Spent in-game resources.</param>
    /// <param name="received">Received in-game resources.</param>
    public async Task<Response<SyncGameEventResponse>> SendInGamePurchaseEventAsync(
        Dictionary<string, decimal> spent = null,
        Dictionary<string, decimal> received = null)
    {
        var e = OnInGamePurchase(spent, received);
        var response = await Kinoa.SyncGameEvents.SendInGamePurchaseEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Tutorial Game Event.
    /// </summary>
    /// <param name="action">Tutorial action.</param>
    public async Task<Response<SyncGameEventResponse>> SendTutorialEventAsync(
        TutorialAction action = TutorialAction.Finish)
    {
        var e = OnTutorial(action);
        var response = await Kinoa.SyncGameEvents.SendTutorialEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Collected Resource Game Event.
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendCollectedResourceEventAsync()
    {
        var e = OnCollectedResource();
        var response = await Kinoa.SyncGameEvents.SendCollectedResourceEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Social Connect Game Event.
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendSocialConnectEventAsync()
    {
        var e = OnSocialConnect();
        var response = await Kinoa.SyncGameEvents.SendSocialConnectEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Social Disconnect Game Event.
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendSocialDisconnectEventAsync()
    {
        var e = OnSocialDisconnect();
        var response = await Kinoa.SyncGameEvents.SendSocialDisconnectEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Social Post Game Event.
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendSocialPostEventAsync()
    {
        var e = OnSocialPost();
        var response = await Kinoa.SyncGameEvents.SendSocialPostEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Error Game Event.
    /// </summary>
    /// <param name="exception">Thrown exception.</param>
    public async Task<Response<SyncGameEventResponse>> SendErrorEventAsync(Exception exception)
    {
        var e = OnError(exception);
        var response = await Kinoa.SyncGameEvents.SendErrorEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    #endregion

    #region Custom Events

    /// <summary>
    ///     Sends the Custom Game Event.
    /// </summary>
    /// <param name="name">Custom event name.</param>
    /// <param name="customParams">Custom event custom parameters.</param>
    public async Task<Response<SyncGameEventResponse>> SendCustomEventAsync(string name,
        Dictionary<string, object> customParams = null)
    {
        var e = OnCustomEvent(name, customParams);
        var response = await Kinoa.SyncGameEvents.SendCustomEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Cheating Game Event (custom event that marks player as cheater).
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendCheatingEventAsync()
    {
        var e = OnCheatingEvent();
        var response = await Kinoa.SyncGameEvents.SendCustomEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the Tester Game Event (custom event that marks player as tester).
    /// </summary>
    public async Task<Response<SyncGameEventResponse>> SendTesterEventAsync()
    {
        var e = OnTesterEvent();
        var response = await Kinoa.SyncGameEvents.SendCustomEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    #endregion

    #region In-App Events

    /// <summary>
    ///     Sends the In-App Close Game Event.
    /// </summary>
    /// <param name="inAppMessage">The closed In-app message.</param>
    /// <param name="inAppMessageID">The closed In-app message identifier.</param>
    public async Task<Response<SyncGameEventResponse>> SendInAppCloseEventAsync(InAppMessage inAppMessage,
        string inAppMessageID = null)
    {
        var e = OnCloseInApp(inAppMessage, inAppMessageID);
        var response = await Kinoa.SyncGameEvents.SendInAppCloseEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the In-App Click Game Event.
    /// </summary>
    /// <param name="inAppMessage">The clicked In-app message.</param>
    /// <param name="inAppMessageID">The clicked In-app message identifier.</param>
    /// <param name="received">Received in-game resources.</param>
    public async Task<Response<SyncGameEventResponse>> SendInAppClickEventAsync(InAppMessage inAppMessage,
        string inAppMessageID = null,
        Dictionary<string, decimal> received = null)
    {
        var e = OnClickInApp(inAppMessage, inAppMessageID, received);
        var response = await Kinoa.SyncGameEvents.SendInAppClickEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    /// <summary>
    ///     Sends the In-App Impression Game Event.
    /// </summary>
    /// <param name="inAppMessage">The impressed In-app message.</param>
    /// <param name="inAppMessageID">The impressed In-app message identifier.</param>
    public async Task<Response<SyncGameEventResponse>> SendInAppImpressionEventAsync(InAppMessage inAppMessage,
        string inAppMessageID = null)
    {
        var e = OnInAppImpression(inAppMessage, inAppMessageID);
        var response = await Kinoa.SyncGameEvents.SendInAppImpressionEventAsync(e, PlayerState);
        ProcessResponse(response);

        return response;
    }

    #endregion

    #region Response Processing

    /// <summary>
    ///     Processes the Sync Game Event response.
    ///     Recommended order: non-inbox → removed → replaced → new → reminders → progression → milestones → instance updates.
    ///     The order can be changed depending on game needs.
    ///     A UUID can appear in multiple lists simultaneously (e.g., OldInApps + ReminderInApps).
    /// </summary>
    /// <param name="response">The response.</param>
    public void ProcessResponse(Response<SyncGameEventResponse> response)
    {
        if (!response.IsSuccessful() || response.Data?.InboxDetails == null)
        {
            return;
        }

        // TODO: KinoaUiService is a demo reference only.
        // Implement your own UI service responsible for in-app display, queue management, content loading, etc.

        ProcessNonInboxInApps(response.Data);
        ProcessRemovedInApps(response.Data);
        ProcessReplacedInApps(response.Data);
        ProcessNewInApps(response.Data);

        ProcessReminderInApps(response.Data);
        ProcessProgressionScoreInApps(response.Data);
        ProcessMilestonesProgressInApps(response.Data);
        ProcessInstanceUpdateInApps(response.Data);

        //TODO: Replace with your game's UI logic.
        KinoaUiService.Instance.TryDisplayGameInApps();
    }

    /// <summary>
    ///     Non-Inbox In-apps — not stored in inbox, displayed once and disappear.
    /// </summary>
    private void ProcessNonInboxInApps(SyncGameEventResponse response)
    {
        var nonInboxInApps = response.OptionalMessages.ToArray();
        if (nonInboxInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.CreateGameInApps(nonInboxInApps,
                nameof(SyncGameEventResponse), nameof(nonInboxInApps), addToDisplayQueue: true);
        }
    }

    /// <summary>
    ///     Removed In-apps — remove from UI.
    /// </summary>
    private void ProcessRemovedInApps(SyncGameEventResponse response)
    {
        var removedInApps = response.InboxDetails.RemovedInApps;
        if (removedInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.RemoveGameInApps(removedInApps,
                nameof(SyncGameEventResponse), nameof(removedInApps));
        }
    }

    /// <summary>
    ///     Replaced In-apps — remove old versions from UI. New versions arrive in NewInApps.
    /// </summary>
    private void ProcessReplacedInApps(SyncGameEventResponse response)
    {
        var replacedInApps = response.InboxDetails.ReplacedInApps;
        if (replacedInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.RemoveGameInApps(replacedInApps,
                nameof(SyncGameEventResponse), nameof(replacedInApps));
        }
    }

    /// <summary>
    ///     Old Inbox In-apps — already shown, no action needed by default.
    ///     Not called from ProcessResponse. Use if your game needs to handle old in-apps (e.g., on game start).
    /// </summary>
    private void ProcessOldInApps(SyncGameEventResponse response)
    {
        var oldInApps = response.InboxMessages
            .Where(msg => response.InboxDetails.OldInApps.Contains(msg.Uuid))
            .ToArray();

        if (oldInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.CreateGameInApps(oldInApps,
                nameof(SyncGameEventResponse), nameof(oldInApps), addToDisplayQueue: false);
        }
    }

    /// <summary>
    ///     New In-apps — add to display queue.
    /// </summary>
    private void ProcessNewInApps(SyncGameEventResponse response)
    {
        var newInApps = response.InboxMessages
            .Where(msg => response.InboxDetails.NewInApps.Contains(msg.Uuid))
            .ToArray();

        if (newInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.CreateGameInApps(newInApps,
                nameof(SyncGameEventResponse), nameof(newInApps), addToDisplayQueue: true);
        }
    }

    /// <summary>
    ///     Reminder In-apps — a reminder that this in-app is in the inbox and should be shown to the player.
    /// </summary>
    private void ProcessReminderInApps(SyncGameEventResponse response)
    {
        var reminderInApps = response.InboxMessages
            .Where(msg => response.InboxDetails.ReminderInApps.Contains(msg.Uuid))
            .ToArray();

        if (reminderInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(reminderInApps,
                nameof(SyncGameEventResponse), nameof(reminderInApps), addToDisplayQueue: true);
        }
    }

    /// <summary>
    ///     Progression Score In-apps — progression score incremented, refresh on the in-app UI object.
    /// </summary>
    private void ProcessProgressionScoreInApps(SyncGameEventResponse response)
    {
        var progressionInApps = response.InboxMessages
            .Where(msg => response.InboxDetails.ProgressionScoreInApps.Contains(msg.Uuid))
            .ToArray();

        if (progressionInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(progressionInApps,
                nameof(SyncGameEventResponse), nameof(progressionInApps), addToDisplayQueue: false);
        }
    }

    /// <summary>
    ///     Milestones Progress In-apps — milestones progress updated, refresh on the in-app UI object.
    /// </summary>
    private void ProcessMilestonesProgressInApps(SyncGameEventResponse response)
    {
        var milestonesProgressInApps = response.InboxMessages
            .Where(msg => response.InboxDetails.MilestonesProgressInApps.Contains(msg.Uuid))
            .ToArray();

        if (milestonesProgressInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(milestonesProgressInApps,
                nameof(SyncGameEventResponse), nameof(milestonesProgressInApps), addToDisplayQueue: false);
        }
    }

    /// <summary>
    ///     Instance Update In-apps — config/placeholders updated by operator on Kinoa Dashboard.
    /// </summary>
    private void ProcessInstanceUpdateInApps(SyncGameEventResponse response)
    {
        var instanceUpdateInApps = response.InboxMessages
            .Where(msg => response.InboxDetails.UpdatedInApps.Contains(msg.Uuid))
            .ToArray();

        if (instanceUpdateInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(instanceUpdateInApps,
                nameof(SyncGameEventResponse), nameof(instanceUpdateInApps), addToDisplayQueue: false);
        }
    }

    #endregion
}
