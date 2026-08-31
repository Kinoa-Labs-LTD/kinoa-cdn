using System;
using System.Collections.Generic;
using Kinoa.Core.Utils.Helpers;
using Kinoa.Data.Enum;
using Kinoa.Data.Events;
using Kinoa.Data.Messaging.InApp;
using Kinoa.Data.State;
using UnityEngine;

/// <summary>
///     Kinoa Game Event building service.
///     Shared base for building event data objects — used by both async and sync event services.
/// </summary>
/// <typeparam name="TService">Service type for singleton pattern.</typeparam>
public abstract class KinoaGameEventBuildingService<TService> : KinoaSingleton<TService>
    where TService : new()
{
    private const string LobbyPlace = "Lobby";
    private const string GameArenaPlace = "GameArena";
    private const string ShopPlace = "Shop";

    /// <summary>
    ///     The current player state.
    ///     To set current player state on game start <see cref="Kinoa.Player.GetState{T}"/>
    /// </summary>
    protected CustomPlayerState PlayerState => KinoaPlayerStateService.Instance.PlayerState;

    /// <summary>
    ///     Updates the player balance with spent and received resources.
    ///     TODO: Implement your balance update logic. Player State can be updated anywhere in the game.
    /// </summary>
    private void UpdateBalance(Dictionary<string, decimal> spent, Dictionary<string, decimal> received)
    {
    }

    /// <summary>
    ///     Updates the player progress.
    ///     TODO: Implement your progress update logic. Example: PlayerState.SetLevel(99);
    /// </summary>
    private void UpdateProgress()
    {
    }

    #region Predefined Events (mandatory)

    /// <summary>
    ///     Builds Session Start event data.
    ///     Updates personal info and attaches custom parameters.
    /// </summary>
    protected StartSessionEventData OnGameSessionStart()
    {
        SetPlayerPersonalInfo();

        var e = new StartSessionEventData();
        e.AddCustomParameters(new Dictionary<string, object>
        {
            { "TestCustomParameterKey", "TestCustomParameterValue" }
        });

        return e;
    }

    /// <summary>
    ///     Updates the Player State with the personal information: country and city.
    /// </summary>
    private void SetPlayerPersonalInfo()
    {
        PlayerState?.PersonalInfo
            .SetCountry(Country.Ukraine)
            //.SetCountry("UA")
            .SetCity("Kyiv");
    }

    /// <summary>
    ///     Builds Payment event data. Validates currency code via <see cref="CurrencyHelper"/>.
    /// </summary>
    /// <param name="productId">Shop product identifier in market.</param>
    /// <param name="spent">Spent real money amount.</param>
    /// <param name="isoCurrencyCode">Real money currency in ISO 4217 format.</param>
    /// <param name="received">Received in-game resources.</param>
    /// <param name="inAppMessage">Optional In-app message that triggered the purchase.</param>
    protected PaymentEventData OnPayment(
        string productId,
        decimal spent,
        string isoCurrencyCode,
        Dictionary<string, decimal> received,
        InAppMessage inAppMessage)
    {
        if (!CurrencyHelper.TryConvert(isoCurrencyCode, out var currency))
        {
            Debug.Log($"Currency \"{isoCurrencyCode}\" is not supported.");
            return null;
        }

        UpdateBalance(spent: null, received: received);

        var e = new PaymentEventData(productId, spent, currency, received, inAppMessage);
        e.SetPlace(ShopPlace);
        if (PlayerState?.Level != null)
            e.SetLevel((int)PlayerState.Level);

        return e;
    }

    #endregion

    #region Predefined Events (optional)

    /// <summary>
    ///     Builds Progression event data.
    /// </summary>
    protected ProgressionEventData OnProgression()
    {
        UpdateProgress();

        var e = new ProgressionEventData();
        e.SetPlace(GameArenaPlace);
        e.SetDuration(60L);

        return e;
    }

    /// <summary>
    ///     Builds Level Up event data. Updates player state level before creating the event.
    /// </summary>
    protected LevelUpEventData OnLevelUp(Dictionary<string, object> customParams = null)
    {
        var newLevel = PlayerState?.Level == null ? 5 : (int)PlayerState.Level + 1;
        PlayerState?.SetLevel(newLevel);
        UpdateProgress();

        var e = new LevelUpEventData();
        e.SetLevel(newLevel);
        e.SetPlace(GameArenaPlace);
        e.SetDuration(60L);
        e.AddCustomParameters(customParams);

        return e;
    }

    /// <summary>
    ///     Builds Watch Ad event data. Validates currency code via <see cref="CurrencyHelper"/>.
    /// </summary>
    /// <param name="incomeFromAd">Real money income from ad.</param>
    /// <param name="isoCurrencyCode">Real money currency in ISO 4217 format.</param>
    /// <param name="adType">Ad type (e.g., "rewarded_video", "interstitial").</param>
    /// <param name="received">Received in-game resources.</param>
    /// <param name="inAppMessage">Optional In-app message that triggered the ad.</param>
    protected WatchAdEventData OnWatchAd(
        decimal incomeFromAd,
        string isoCurrencyCode,
        string adType,
        Dictionary<string, decimal> received,
        InAppMessage inAppMessage)
    {
        if (!CurrencyHelper.TryConvert(isoCurrencyCode, out var currency))
        {
            Debug.Log($"Currency \"{isoCurrencyCode}\" is not supported.");
            return null;
        }

        UpdateBalance(spent: null, received: received);

        var e = new WatchAdEventData(incomeFromAd, currency, adType, received, inAppMessage);
        e.SetPlace(ShopPlace);
        if (PlayerState?.Level != null)
            e.SetLevel((int)PlayerState.Level);

        return e;
    }

    /// <summary>
    ///     Builds In-Game Purchase event data.
    /// </summary>
    /// <param name="spent">Spent in-game resources.</param>
    /// <param name="received">Received in-game resources.</param>
    /// <param name="inAppMessage">Optional In-app message that triggered the purchase.</param>
    protected InGamePurchaseEventData OnInGamePurchase(
        Dictionary<string, decimal> spent = null,
        Dictionary<string, decimal> received = null,
        InAppMessage inAppMessage = null)
    {
        UpdateBalance(spent, received);

        var e = new InGamePurchaseEventData(spent, received, inAppMessage);
        e.SetPlace(ShopPlace);
        if (PlayerState?.Level != null)
            e.SetLevel((int)PlayerState.Level);

        return e;
    }

    /// <summary>
    ///     Builds Tutorial event data.
    /// </summary>
    /// <param name="action">Tutorial action (Start, Skip, Finish).</param>
    protected TutorialEventData OnTutorial(TutorialAction action = TutorialAction.Finish)
    {
        var e = new TutorialEventData(action);
        e.SetStep(1);
        e.SetPlace(GameArenaPlace);
        if (PlayerState?.Level != null)
            e.SetLevel((int)PlayerState.Level);

        return e;
    }

    /// <summary>
    ///     Builds Collected Resource event data.
    /// </summary>
    /// <param name="inAppMessage">The In-app message the player collected resources from.</param>
    protected CollectedResourceEventData OnCollectedResource(InAppMessage inAppMessage = null)
    {
        var e = new CollectedResourceEventData(inAppMessage);
        e.SetPlace(GameArenaPlace);
        if (PlayerState?.Level != null)
            e.SetLevel((int)PlayerState.Level);

        return e;
    }

    /// <summary>
    ///     Builds Social Connect event data. Sets player social network identifiers.
    /// </summary>
    protected SocialConnectEventData OnSocialConnect()
    {
        PlayerState?.PlayerIdentifiers
            .SetFacebookId(Guid.NewGuid().ToString())
            .SetGoogleId(Guid.NewGuid().ToString())
            .SetAppleId(Guid.NewGuid().ToString());

        return new SocialConnectEventData();
    }

    /// <summary>
    ///     Builds Social Disconnect event data. Clears player social network identifiers.
    /// </summary>
    protected SocialDisconnectEventData OnSocialDisconnect()
    {
        PlayerState?.PlayerIdentifiers
            .SetFacebookId(null)
            .SetGoogleId(null)
            .SetAppleId(null);

        return new SocialDisconnectEventData();
    }

    /// <summary>
    ///     Builds Social Post event data.
    /// </summary>
    protected SocialPostEventData OnSocialPost()
    {
        var e = new SocialPostEventData();
        e.SetPlace(LobbyPlace);
        if (PlayerState?.Level != null)
            e.SetLevel((int)PlayerState.Level);

        return e;
    }

    #endregion

    #region Custom Events

    /// <summary>
    ///     Builds Custom event data.
    /// </summary>
    /// <param name="name">Custom event name (must match Dashboard event name for trigger rules).</param>
    /// <param name="customParams">Custom parameters (keys must match Dashboard field names).</param>
    protected CustomEventData OnCustomEvent(
        string name = "custom_event",
        Dictionary<string, object> customParams = null)
    {
        var e = new CustomEventData(name);
        e.AddCustomParameter("number_list", new List<int> { 1, 2, 3 });
        e.AddCustomParameter("string_list", new[] { "1", "two", "III" });
        e.AddCustomParameters(new Dictionary<string, object>
        {
            ["ID"] = 1.0,
            ["custom_key_2"] = "custom_value_2",
            ["camelCase"] = true
        });
        e.AddCustomParameters(customParams);

        return e;
    }

    /// <summary>
    ///     Builds Cheating event. Marks player as cheater and blocks the player.
    /// </summary>
    protected CustomEventData OnCheatingEvent()
    {
        PlayerState?
            .SetIsCheater(true)
            .SetIsBlocked(true);

        return new CustomEventData("cheating_event");
    }

    /// <summary>
    ///     Builds Tester event. Marks the player as a tester.
    /// </summary>
    protected CustomEventData OnTesterEvent()
    {
        PlayerState?.SetIsTester(true);

        return new CustomEventData("tester_event");
    }

    #endregion

    #region Error

    /// <summary>
    ///     Builds Error event data from an exception.
    /// </summary>
    protected ErrorEventData OnError(Exception exception)
    {
        var e = new ErrorEventData(exception);
        e.AddCustomParameters(new Dictionary<string, object>
        {
            ["additional_message"] = "Something went wrong during e.g. SDK response deserialization."
        });

        return e;
    }

    #endregion

    #region In-App Events

    /// <summary>
    ///     Builds In-App Close event data.
    /// </summary>
    /// <param name="inAppMessage">The closed In-app message.</param>
    /// <param name="inAppMessageID">The closed In-app message identifier (fallback if message is null).</param>
    protected InAppCloseEventData OnCloseInApp(InAppMessage inAppMessage, string inAppMessageID = null)
    {
        var e = new InAppCloseEventData(inAppMessage?.MessageId ??
                                        (!string.IsNullOrEmpty(inAppMessageID) ? inAppMessageID : "SomeInAppID"));
        e.SetPlace(LobbyPlace);

        return e;
    }

    /// <summary>
    ///     Builds In-App Click event data.
    /// </summary>
    /// <param name="inAppMessage">The clicked In-app message.</param>
    /// <param name="inAppMessageID">The clicked In-app message identifier (fallback if message is null).</param>
    /// <param name="received">Received in-game resources from the In-app click.</param>
    protected InAppClickEventData OnClickInApp(
        InAppMessage inAppMessage,
        string inAppMessageID = null,
        Dictionary<string, decimal> received = null)
    {
        UpdateBalance(spent: null, received: received);

        var e = new InAppClickEventData(inAppMessage?.MessageId ??
                                        (!string.IsNullOrEmpty(inAppMessageID) ? inAppMessageID : "SomeInAppID"));
        e.SetPlace(LobbyPlace);
        e.SetReceived(received);

        return e;
    }

    /// <summary>
    ///     Builds In-App Impression event data.
    /// </summary>
    /// <param name="inAppMessage">The impressed In-app message.</param>
    /// <param name="inAppMessageID">The impressed In-app message identifier (fallback if message is null).</param>
    protected InAppImpressionEventData OnInAppImpression(InAppMessage inAppMessage, string inAppMessageID = null)
    {
        var e = new InAppImpressionEventData(inAppMessage?.MessageId ??
                                             (!string.IsNullOrEmpty(inAppMessageID) ? inAppMessageID : "SomeInAppID"));
        e.SetPlace(LobbyPlace);

        return e;
    }

    #endregion
}
