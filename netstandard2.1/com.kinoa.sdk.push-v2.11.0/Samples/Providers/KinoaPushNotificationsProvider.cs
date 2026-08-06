#if UNITY_ANDROID || UNITY_IOS
using System.Text;
using Kinoa.PushNotifications.Data;
using UnityEngine;

/// <summary>
///     Kinoa Push Notifications SDK basic methods and properties provider sample.
/// </summary>
public static class KinoaPushNotificationsProvider
{
    /// <summary>
    ///     Initialize the new instance of <see cref="KinoaPushNotificationsProvider"/>
    /// </summary>
    public static void Subcscribe()
    {
        Kinoa.PushNotifications.SDK.OnPushNotificationClicked += OnPushNotificationClicked;
    }

    /// <summary>
    ///     Event handler of push clicked.
    /// </summary>
    private static void OnPushNotificationClicked(KinoaPushNotificationClickedData pushClickedData)
    {
        var log = new StringBuilder("[GAME] Kinoa push notification clicked:");
        log.AppendFormat($"\nNotification ID: {pushClickedData.NotificationIdentifier}");
        log.AppendFormat($"\nCampaign ID: {pushClickedData.CampaignId}");
        log.AppendFormat($"\nCampaign iteration number: {pushClickedData.IterationNumber}");
        if (pushClickedData.InAppCreationParams != null)
        {
            log.AppendFormat($"\nInApp Data:");
            log.AppendFormat($"\n\tInApp ID: {pushClickedData.InAppCreationParams.ID}");
        }

        if (pushClickedData.InternalLink != null)
        {
            log.AppendFormat("\nInternal Link:");
            log.AppendFormat($"\n\tType: {pushClickedData.InternalLink.Type}");
            log.AppendFormat($"\n\tValue: {pushClickedData.InternalLink.Value}");
        }
        
        if (pushClickedData.ExtraData != null)
        {
            log.AppendFormat($"\nExtra Data:");
            foreach (var ed in pushClickedData.ExtraData)
            {
                log.AppendFormat($"\n\tKey: {ed.Key} Value: {ed.Value}");
            }
        }
        log.AppendFormat("\n");
        Debug.Log(log.ToString());
    }
    
    /// <summary>
    ///     Block player pushes.
    /// </summary>
    public static async void BlockPushes()
    {
        var response = await Kinoa.PushNotifications.SDK.BlockPushes();
        Debug.Log($"[GAME] Kinoa pushes block request completed with status: {response.Status}");
    }
    
    /// <summary>
    ///     Unblock player pushes.
    /// </summary>
    public static async void UnblockPushes()
    {
        var response = await Kinoa.PushNotifications.SDK.UnblockPushes();
        Debug.Log($"[GAME] Kinoa pushes unblock request completed with status: {response.Status}");
    }
    
    /// <summary>
    ///     Delete device push token.
    /// </summary>
    public static async void DeleteToken()
    {
        var token = "FCM or APN device token";
        var response = await Kinoa.PushNotifications.SDK.DeleteToken(token);
        Debug.Log($"[GAME] Kinoa delete token completed with status: {response.Status}");
    }
    
    /// <summary>
    ///     Check pushes status on Kinoa.
    /// </summary>
    public static async void CheckPushStatus()
    {
        var response = await Kinoa.PushNotifications.SDK.GetPushStatus();
        if (response.IsSuccessful())
        {
            Debug.Log($"[GAME] Kinoa pushes are blocked: {response.Data.Blocked}");
        }
        else
        {
            Debug.Log($"[GAME] Get push status request status: {response.Status}.");
        }
    }
    
	/// <summary>
    ///     Set personalization information.
    /// </summary>
    public static async void SetPersonalizationInfo()
    {
        var personalizationInfo = new KinoaPushPersonalizationInfo()
        {
            Name = "KinoaPlayer",
            Country = "KinoaCountry",
            City = "KinoaCity",
            Extra1 = "KinoaExtra1",
            Extra2 = "KinoaExtra2",
            Extra3 = "KinoaExtra3",
        };
        personalizationInfo
            .SetValue("custom_key_one", "Custom Value One")
            .SetValue("custom_key_two", "Custom Value TWO");
        var response = await Kinoa.PushNotifications.SDK.SetPushPersonalizationInfo(personalizationInfo);
        Debug.Log($"[GAME] Kinoa set personalization info completed with status: {response.Status}");
    }
    
    /// <summary>
    ///     Get personalization information.
    /// </summary>
    public static async void GetPersonalizationInfo()
    {
        var response = await Kinoa.PushNotifications.SDK.GetPushPersonalizationInfo();
        if (response.IsSuccessful())
        {
            Debug.Log($"[GAME] Kinoa get personalization info completed successfully:.\n" +
                      $"Name: \"{response.Data?.Name}\"; " +
                      $"City: \"{response.Data?.City}\"; " +
                      $"Country: \"{response.Data?.Country}\"; " +
                      $"Extra 1: \"{response.Data?.Extra1}\"; " +
                      $"Extra 2: \"{response.Data?.Extra2}\"; " +
                      $"Extra 3: \"{response.Data?.Extra3}\"; " +
                      $"Custom 1: \"{response.Data?.GetValue("custom_key_one")}\"; " +
                      $"Custom 2: \"{response.Data?.GetValue("custom_key_two")}\"; ");
        }
        else
        {
            Debug.Log($"[GAME] Kinoa get personalization info completed with status: {response.Status}");
        }
    }
    
    /// <summary>
    ///     Delete personalization information.
    /// </summary>
    public static async void DeletePersonalizationInfo()
    {
        var response = await Kinoa.PushNotifications.SDK.DeletePushPersonalizationInfo();
        Debug.Log($"[GAME] Kinoa delete personalization info completed with status: {response.Status}");
    }
}
#endif