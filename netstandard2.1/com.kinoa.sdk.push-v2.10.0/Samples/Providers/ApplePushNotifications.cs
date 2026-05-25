#if UNITY_IOS
using System;
using System.Text;
using Kinoa.PushNotifications.iOS.Clients;
using Kinoa.PushNotifications.iOS.Logic.Extensions;
using Unity.Notifications.iOS;
using UnityEngine;

/// <summary>
///     By installing a Kinoa Push Notifications, the <see cref="Unity.Notifications"/> features
///     are automatically available and could be used directly to extend the client push notification features
///     e.g <see cref="iOSNotificationCenter.GetLastRespondedNotification"/> <seealso cref="GetLastOpenedNotification"/>.
/// </summary>
public class ApplePushNotifications : ApplePushNotificationClient
{
    /// <summary>
    ///     The authorization options your app is requesting.
    ///     You may specify multiple options to request authorization for.
    ///     Request only the authorization options that you plan to use.
    /// </summary>
    protected override AuthorizationOption AuthorizationOptions =>
        AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;

    /// <summary>
    ///     On push notification service initialization successfully completed.
    /// </summary>
    protected override void OnInitializationCompleted() =>
        Debug.Log("[GAME] APNs initialization is completed. Status: success.");

    /// <summary>
    ///     On push notification service initialization failed.
    /// </summary>
    protected override void OnInitializationFailed() =>
        Debug.Log("[GAME] APNs initialization is completed. Status: failed.");

    /// <summary>
    ///     On whenever a remote notification is received, regardless if it's shown in the foreground.
    /// </summary>
    /// <param name="notification">iOS notification message.</param>
    protected override void OnPushNotificationReceived(iOSNotification notification)
    {
        Debug.Log("[Game] The new remote push notification message successfully received.");
    }

    /// <summary>
    ///     On device registration token updated.
    /// </summary>
    /// <param name="token">Updated token for a current device.</param>
    protected override void OnRegistrationTokenUpdated(string token) =>
        Debug.Log($"[GAME] Received APNs device registration Token: {token}");

    /// <summary>
    ///     Sample shows how to get the last opened notification
    ///     using the <see cref="Unity.Notifications.iOS"/> features.
    /// </summary>
    public iOSNotification GetLastOpenedNotification()
    {
        var openedNotification = iOSNotificationCenter.GetLastRespondedNotification();
        if (openedNotification != null)
        {
            LogNotifications(new [] { openedNotification }, nameof(GetLastOpenedNotification));
        }
        else Debug.Log("[GAME] There is no last opened notification available.");

        return openedNotification;
    }

    /// <summary>
    ///     Sample shows how to get delivered notifications that are currently shown in the Notification Center.
    ///     using the <see cref="Unity.Notifications.iOS"/> features.
    /// </summary>
    public iOSNotification[] GetDeliveredNotifications()
    {
        var deliveredNotifications = iOSNotificationCenter.GetDeliveredNotifications();
        LogNotifications(deliveredNotifications, nameof(GetDeliveredNotifications));
        return deliveredNotifications;
    }

    /// <summary>
    ///     Logs the notifications.
    /// </summary>
    /// <param name="notifications">Notification array.</param>
    /// <param name="source">The source of notification array.</param>
    private void LogNotifications(iOSNotification[] notifications, string source)
    {
        var logBuilder = new StringBuilder();
        logBuilder.AppendFormat("[GAME] {0}:{1}", source, notifications.Length);

        foreach (var notification in notifications)
            LogNotification(logBuilder, notification);

        Debug.Log(logBuilder.ToString());
    }

    /// <summary>
    ///     Logs the notification.
    /// </summary>
    /// <param name="logBuilder">Log builder.</param>
    /// <param name="notification">Notification.</param>
    private async void LogNotification(StringBuilder logBuilder, iOSNotification notification)
    {
        if (notification == null) return;
        
        logBuilder.AppendFormat("\nID: {0}", notification.Identifier);
        logBuilder.AppendFormat("\nTitle: {0}", notification.Title);
        logBuilder.AppendFormat("\nSubtitle: {0}", notification.Subtitle);
        logBuilder.AppendFormat("\nBody: {0}", notification.Body);
        logBuilder.AppendFormat("\nExtraData:");
        logBuilder.Append("\n{");
        foreach (var iter in await notification.GetExtraData())
            logBuilder.Append("\n    " + iter.Key + ": " + iter.Value);
        logBuilder.Append("\n}");
        
        if (notification.UserInfo != null && notification.UserInfo.Count > 0)
        {
            logBuilder.AppendFormat("\nUserInfo:");
            logBuilder.Append("\n{");
            foreach (var iter in notification.UserInfo)
                logBuilder.Append("\n    " + iter.Key + ": " + iter.Value);
            logBuilder.Append("\n}");
        }
    }
}
#endif