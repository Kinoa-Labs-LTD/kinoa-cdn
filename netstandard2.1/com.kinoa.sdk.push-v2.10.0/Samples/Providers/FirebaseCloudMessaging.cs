#if UNITY_ANDROID
using System.Text;
using Firebase.Messaging;
using Kinoa.PushNotifications.Android.Clients;
using Kinoa.PushNotifications.Android.Logic.Extensions;
using UnityEngine;

/// <summary>
///     Firebase Cloud Messaging client sample.
/// </summary>
public class FirebaseCloudMessaging : FirebasePushNotificationClient
{
    /// <summary>
    ///     Initializes the new instance of FirebaseCloudMessaging client.
    /// </summary>
    /// <param name="withFirebaseAppCheckAndFixDependency"> Indicates if Kinoa needs to call FirebaseApp.CheckAndFixDependenciesAsync.</param>
    /// <param name="withRequestPermission">Indicates if Kinoa needs to call FirebaseMessaging.RequestPermissionAsync.</param>
    /// <param name="withTokenRequest">Indicates if Kinoa needs to call FirebaseMessaging.GetTokenAsync on application startup. Default values is false, token saved to PlayersPref will be used.</param>
    public FirebaseCloudMessaging(
        bool withFirebaseAppCheckAndFixDependency = true,
        bool withRequestPermission = true,
        bool withTokenRequest = false) : base(withFirebaseAppCheckAndFixDependency, withRequestPermission, withTokenRequest)
    {
    }
	
    /// <summary>
    ///     On push notification service initialization successfully completed.
    /// </summary>
    protected override void OnInitializationCompleted() =>
        Debug.Log("[GAME] FCM initialization is completed. Status: success.");

    /// <summary>
    ///     On push notification service initialization failed.
    /// </summary>
    protected override void OnInitializationFailed() =>
        Debug.Log("[GAME] FCM initialization is completed. Status: failed.");

    /// <summary>
    ///     On push notification message received.
    /// </summary>
    /// <param name="message">Firebase cloud message.</param>
    protected override void OnPushNotificationReceived(FirebaseMessage message)
    {
        Debug.Log("[Game] The new remote push notification message successfully received.");
    }

    /// <summary>
    ///     On device registration token updated.
    /// </summary>
    /// <param name="token">Updated token for a current device.</param>
    protected override void OnRegistrationTokenUpdated(string token)
    {
        Debug.Log($"[GAME] Received FCM device registration Token: {token}");
        //TODO: Please remember to renew your topics subscription using "SubscribeToTopicAsync" if the new registration token received.
    }
}
#endif