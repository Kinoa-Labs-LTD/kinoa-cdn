using System;
using System.Collections.Generic;
using Kinoa.Data.Enum;
using Kinoa.Data.Events;
using Kinoa.Data.Messaging.InApp;
using Kinoa.Data.State;

/// <summary>
///     Kinoa Asynchronous (fire-and-forget) Game Events service.
///     All methods are void — In-app messages triggered by async events arrive via WebSocket.
/// </summary>
public class KinoaGameEventsService : KinoaGameEventBuildingService<KinoaGameEventsService>
{
    #region Predefined Events (mandatory)

    /// <summary>
    ///     Sends Session Start Game Event. Mandatory.
    /// </summary>
    public void SendSessionStartEvent()
    {
        var e = OnGameSessionStart();
        Kinoa.GameEvents.SendSessionStartEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Payment Game Event. Mandatory.
    /// </summary>
    /// <param name="productId">Shop product identifier in market. Example: "play-market-product-id".</param>
    /// <param name="spent">Spent real money amount. Example: 0.99m.</param>
    /// <param name="isoCurrencyCode">Real money currency in ISO 4217 format. Example: "USD".</param>
    /// <param name="received">Received in-game resources.</param>
    /// <param name="inAppMessage">In-app message that triggered the purchase.</param>
    public void SendPaymentEvent(
        string productId,
        decimal spent,
        string isoCurrencyCode,
        Dictionary<string, decimal> received,
        InAppMessage inAppMessage)
    {
        var e = OnPayment(productId, spent, isoCurrencyCode, received, inAppMessage);
        Kinoa.GameEvents.SendPaymentEvent(e, PlayerState);
    }

    #endregion

    #region Predefined Events (optional)

    /// <summary>
    ///     Sends the Progression Game Event.
    /// </summary>
    public void SendProgressionEvent()
    {
        var e = OnProgression();
        Kinoa.GameEvents.SendProgressionEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Level Up Game Event.
    /// </summary>
    /// <param name="customParams">Custom event custom parameters.</param>
    public void SendLevelUpEvent(Dictionary<string, object> customParams = null)
    {
        var e = OnLevelUp(customParams);
        Kinoa.GameEvents.SendLevelUpEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Watch Ad Game Event.
    /// </summary>
    /// <param name="incomeFromAd">Real money income from ad. Example: 40m.</param>
    /// <param name="isoCurrencyCode">Real money currency in ISO 4217 format. Example: "UAH".</param>
    /// <param name="adType">Ad type. Example: "simple_ad".</param>
    /// <param name="received">Received in-game resources.</param>
    /// <param name="inAppMessage">In-app message that triggered the ad.</param>
    public void SendWatchAdEvent(
        decimal incomeFromAd,
        string isoCurrencyCode,
        string adType,
        Dictionary<string, decimal> received,
        InAppMessage inAppMessage)
    {
        var e = OnWatchAd(incomeFromAd, isoCurrencyCode, adType, received, inAppMessage);
        Kinoa.GameEvents.SendWatchAdEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the In-game Purchase Game Event.
    /// </summary>
    /// <param name="spent">Spent in-game resources.</param>
    /// <param name="received">Received in-game resources.</param>
    public void SendInGamePurchaseEvent(
        Dictionary<string, decimal> spent = null,
        Dictionary<string, decimal> received = null)
    {
        var e = OnInGamePurchase(spent, received);
        Kinoa.GameEvents.SendInGamePurchaseEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Tutorial Game Event.
    /// </summary>
    /// <param name="action">Tutorial action.</param>
    public void SendTutorialEvent(TutorialAction action = TutorialAction.Finish)
    {
        var e = OnTutorial(action);
        Kinoa.GameEvents.SendTutorialEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Collected Resource Game Event.
    /// </summary>
    public void SendCollectedResourceEvent()
    {
        var e = OnCollectedResource();
        Kinoa.GameEvents.SendCollectedResourceEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Social Connect Game Event.
    /// </summary>
    public void SendSocialConnectEvent()
    {
        var e = OnSocialConnect();
        Kinoa.GameEvents.SendSocialConnectEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Social Disconnect Game Event.
    /// </summary>
    public void SendSocialDisconnectEvent()
    {
        var e = OnSocialDisconnect();
        Kinoa.GameEvents.SendSocialDisconnectEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Social Post Game Event.
    /// </summary>
    public void SendSocialPostEvent()
    {
        var e = OnSocialPost();
        Kinoa.GameEvents.SendSocialPostEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Error Game Event. No player state needed.
    /// </summary>
    /// <param name="exception">Thrown exception.</param>
    public void SendErrorEvent(Exception exception)
    {
        var e = OnError(exception);
        Kinoa.GameEvents.SendErrorEvent(e);
    }

    #endregion

    #region Custom Events

    /// <summary>
    ///     Sends the Custom Game Event.
    /// </summary>
    /// <param name="name">Custom event name.</param>
    /// <param name="customParams">Custom event custom parameters.</param>
    public void SendCustomEvent(string name, Dictionary<string, object> customParams = null)
    {
        var e = OnCustomEvent(name, customParams);
        Kinoa.GameEvents.SendCustomEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Cheating Game Event (custom event that marks player as cheater).
    /// </summary>
    public void SendCheatingEvent()
    {
        var e = OnCheatingEvent();
        Kinoa.GameEvents.SendCustomEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the Tester Game Event (custom event that marks player as tester).
    /// </summary>
    public void SendTesterEvent()
    {
        var e = OnTesterEvent();
        Kinoa.GameEvents.SendCustomEvent(e, PlayerState);
    }

    #endregion

    #region In-App Events

    /// <summary>
    ///     Sends the In-App Close Game Event.
    /// </summary>
    /// <param name="inAppMessage">The closed In-app message.</param>
    /// <param name="inAppMessageID">The closed In-app message identifier.</param>
    public void SendInAppCloseEvent(InAppMessage inAppMessage, string inAppMessageID = null)
    {
        var e = OnCloseInApp(inAppMessage, inAppMessageID);
        Kinoa.GameEvents.SendInAppCloseEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the In-App Click Game Event.
    /// </summary>
    /// <param name="inAppMessage">The clicked In-app message.</param>
    /// <param name="inAppMessageID">The clicked In-app message identifier.</param>
    /// <param name="received">Received in-game resources.</param>
    public void SendInAppClickEvent(InAppMessage inAppMessage, string inAppMessageID = null,
        Dictionary<string, decimal> received = null)
    {
        var e = OnClickInApp(inAppMessage, inAppMessageID, received);
        Kinoa.GameEvents.SendInAppClickEvent(e, PlayerState);
    }

    /// <summary>
    ///     Sends the In-App Impression Game Event. No player state needed.
    /// </summary>
    /// <param name="inAppMessage">The impressed In-app message.</param>
    /// <param name="inAppMessageID">The impressed In-app message identifier.</param>
    public void SendInAppImpressionEvent(InAppMessage inAppMessage, string inAppMessageID = null)
    {
        var e = OnInAppImpression(inAppMessage, inAppMessageID);
        Kinoa.GameEvents.SendInAppImpressionEvent(e);
    }

    #endregion

    #region Batch Events

    /// <summary>
    ///     Sends multiple Game Events in a single request.
    /// </summary>
    public void SendEvents()
    {
        var startedTutorialEventData = OnTutorial(TutorialAction.Start);
        startedTutorialEventData.AddCustomParameters(new Dictionary<string, object> { { "tutorial_id", "tutorial_1" } });
        var finishedTutorialEventData = OnTutorial(TutorialAction.Finish);
        finishedTutorialEventData.AddCustomParameters(new Dictionary<string, object> { { "tutorial_id", "tutorial_2" } });
        var tutorialEventsCollection = new List<GameEventData> { startedTutorialEventData, finishedTutorialEventData };

        Kinoa.GameEvents.SendEvents(tutorialEventsCollection, PlayerState);
    }

    #endregion
}
