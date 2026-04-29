using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Services;
using Kinoa.Core.Network;
using Kinoa.Data;
using Kinoa.Data.Enum;
using Kinoa.Data.Messaging.Command;
using Kinoa.Data.Messaging.InApp;
using Kinoa.Data.Messaging.InApp.Capping;
using Kinoa.Data.Messaging.InApp.CreationParams;
using Kinoa.Data.Messaging.InApp.Features.Milestones;
using Kinoa.Data.Messaging.InApp.Templates.Custom;
using Kinoa.Data.Messaging.InApp.Templates.Simple;
using Kinoa.Data.Network;
using Kinoa.Data.ResourceManagement;
using Debug = UnityEngine.Debug;

/// <summary>
///     The sample implementation of the Kinoa Messaging service.
/// </summary>
public class KinoaMessagingService : KinoaSingleton<KinoaMessagingService>
{
    // TODO: KinoaUiService is a demo reference only.
    // Implement your own UI service responsible for in-app display, queue management, content loading, etc.

    #region Initialization

    /// <summary>
    ///     Initialize and configure the Kinoa Messaging service.
    /// </summary>
    public async Task InitializeAsync()
    {
        var inAppSecurityConfiguration = new InAppSecurityConfiguration(true);
        await Kinoa.Messaging.Initialize(inAppSecurityConfiguration);

        Kinoa.Messaging.OnInAppReceived += OnInAppReceived;
        Kinoa.Messaging.OnCommandReceived += OnCommandReceived;
    }

    #endregion

    #region WebSocket Handlers

    /// <summary>
    ///     Handles the received WebSocket Command message.
    /// </summary>
    private void OnCommandReceived(CommandMessage message)
    {
        switch (message.Command)
        {
            case ReloadP2PCommand:
                Log($"[Command] {nameof(ReloadP2PCommand)} received. UUID: {message.Uuid}");
                break;
            case RemovedInboxInAppsCommand cmd:
                //TODO: Replace with your game's UI logic.
                KinoaUiService.Instance.RemoveGameInApps(cmd.InApps.Select(x => x.Uuid),
                    "WebSocket", nameof(RemovedInboxInAppsCommand));
                break;
            default:
                Log($"[Command] Unknown type {message.Command?.GetType().Name}. UUID: {message.Uuid}");
                break;
        }
    }

    /// <summary>
    ///     Processes the In-app messages received via WebSocket.
    /// </summary>
    private void OnInAppReceived(InAppMessages messages)
    {
        ProcessNonInboxInApps(messages.Data);
        ProcessInboxInApps(messages.Data);

        //TODO: Replace with your game's UI logic.
        KinoaUiService.Instance.TryDisplayGameInApps();
        LogInAppMessages(messages.Data);
    }

    /// <summary>
    ///     Processes the non-Inbox In-app messages.
    /// </summary>
    private void ProcessNonInboxInApps(IEnumerable<InAppMessage> messages)
    {
        var nonInboxInApps = messages.Where(x => !x.IsInboxMessage).ToArray();
        if (nonInboxInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.CreateGameInApps(nonInboxInApps,
                "WebSocket", nameof(nonInboxInApps), addToDisplayQueue: true);
        }
    }

    /// <summary>
    ///     Processes the Inbox In-app messages by their command type.
    ///     Mirror of KinoaSyncGameEventsService.ProcessResponse — same In-app processing logic,
    ///     but WebSocket In-apps carry instructions as InAppMessage.Command,
    ///     while Sync API delivers them via InboxDetails categories.
    /// </summary>
    private void ProcessInboxInApps(IEnumerable<InAppMessage> messages)
    {
        var inboxMessages = messages.Where(x => x.IsInboxMessage).ToArray();

        ProcessNewInApps(inboxMessages);
        ProcessReplacedInApps(inboxMessages);
        ProcessReminderInApps(inboxMessages);
        ProcessProgressionScoreInApps(inboxMessages);
        ProcessMilestonesProgressInApps(inboxMessages);
        ProcessInstanceUpdateInApps(inboxMessages);
    }

    /// <summary>
    ///     New In-apps — no command attached, add to display queue.
    /// </summary>
    private void ProcessNewInApps(IEnumerable<InAppMessage> inboxMessages)
    {
        var newInApps = inboxMessages.Where(x => x.Command == null).ToList();
        if (newInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.CreateGameInApps(newInApps,
                "WebSocket", nameof(newInApps), addToDisplayQueue: true);
        }
    }

    /// <summary>
    ///     Replaced In-apps — remove old version from UI, add replacement to display queue.
    /// </summary>
    private void ProcessReplacedInApps(IEnumerable<InAppMessage> inboxMessages)
    {
        var replacedInApps = inboxMessages.Where(x => x.Command is InAppReplacedCommand).ToList();
        if (replacedInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(replacedInApps,
                "WebSocket", nameof(InAppReplacedCommand), addToDisplayQueue: true);
        }
    }

    /// <summary>
    ///     Reminder In-apps — a reminder that this in-app is in the inbox and should be shown to the player.
    /// </summary>
    private void ProcessReminderInApps(IEnumerable<InAppMessage> inboxMessages)
    {
        var reminderInApps = inboxMessages.Where(x => x.Command is InAppReminderCommand).ToList();
        if (reminderInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(reminderInApps,
                "WebSocket", nameof(InAppReminderCommand), addToDisplayQueue: true);
        }
    }

    /// <summary>
    ///     Progression Score In-apps — progression score incremented, refresh on the in-app UI object.
    /// </summary>
    private void ProcessProgressionScoreInApps(IEnumerable<InAppMessage> inboxMessages)
    {
        var scoreChangedInApps = inboxMessages.Where(x => x.Command is InAppScoreChangedCommand).ToList();
        var displayOnChange = scoreChangedInApps
            .Where(x => ((InAppScoreChangedCommand)x.Command).DisplayOnProgressChange).ToList();
        var silentUpdate = scoreChangedInApps
            .Where(x => !((InAppScoreChangedCommand)x.Command).DisplayOnProgressChange).ToList();

        if (displayOnChange.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(displayOnChange,
                "WebSocket", nameof(InAppScoreChangedCommand), addToDisplayQueue: true);
        }

        if (silentUpdate.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(silentUpdate,
                "WebSocket", nameof(InAppScoreChangedCommand), addToDisplayQueue: false);
        }
    }

    /// <summary>
    ///     Milestones Progress In-apps — milestones progress updated, refresh on the in-app UI object.
    /// </summary>
    private void ProcessMilestonesProgressInApps(IEnumerable<InAppMessage> inboxMessages)
    {
        var milestonesProgressInApps = inboxMessages.Where(x => x.Command is InAppMilestonesProgressChangedCommand).ToList();
        var displayOnChange = milestonesProgressInApps
            .Where(x => ((InAppMilestonesProgressChangedCommand)x.Command).DisplayOnProgressChange).ToList();
        var silentUpdate = milestonesProgressInApps
            .Where(x => !((InAppMilestonesProgressChangedCommand)x.Command).DisplayOnProgressChange).ToList();

        if (displayOnChange.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(displayOnChange,
                "WebSocket", nameof(InAppMilestonesProgressChangedCommand), addToDisplayQueue: true);
        }

        if (silentUpdate.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(silentUpdate,
                "WebSocket", nameof(InAppMilestonesProgressChangedCommand), addToDisplayQueue: false);
        }
    }

    /// <summary>
    ///     Instance Update In-apps — config/placeholders updated by operator on Kinoa Dashboard.
    /// </summary>
    private void ProcessInstanceUpdateInApps(IEnumerable<InAppMessage> inboxMessages)
    {
        var instanceUpdateInApps = inboxMessages.Where(x => x.Command is InAppInstanceUpdateCommand).ToList();
        if (instanceUpdateInApps.Any())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ReplaceGameInApps(instanceUpdateInApps,
                "WebSocket", nameof(InAppInstanceUpdateCommand), addToDisplayQueue: false);
        }
    }

    #endregion

    #region Inbox Management

    /// <summary>
    ///     Gets the list of all InApp inbox messages.
    /// </summary>
    public async Task<Response<List<InAppMessage>>> GetInboxMessagesAsync()
    {
        var response = await Kinoa.Messaging.GetInboxMessagesAsync();
        if (response.Status == ResponseState.Success && response.Data != null)
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.ClearGameInApps(nameof(response), nameof(GetInboxMessagesAsync));
            KinoaUiService.Instance.CreateGameInApps(response.Data,
                nameof(response), nameof(GetInboxMessagesAsync), addToDisplayQueue: true);
            KinoaUiService.Instance.TryDisplayGameInApps();
        }
        else
        {
            Log($"{nameof(GetInboxMessagesAsync)} status: {response.Status}.");
        }

        return response;
    }

    /// <summary>
    ///     Deletes a single inbox message.
    /// </summary>
    public async Task<Response> DeleteInboxMessageAsync(InAppMessage message)
    {
        var response = await Kinoa.Messaging.DeleteInboxMessageAsync(message);
        Log($"{nameof(DeleteInboxMessageAsync)} status: {response.Status}.");
        if (!response.IsSuccessful()) return response;

        //TODO: Replace with your game's UI logic.
        KinoaUiService.Instance.RemoveGameInApp(message.Uuid,
            nameof(response), nameof(DeleteInboxMessageAsync));

        return response;
    }

    /// <summary>
    ///     Deletes multiple inbox messages.
    /// </summary>
    public async Task<Response> DeleteInboxMessagesAsync(List<InAppMessage> messages)
    {
        var response = await Kinoa.Messaging.DeleteInboxMessagesAsync(messages);
        Log($"{nameof(DeleteInboxMessagesAsync)} status: {response.Status}.");
        if (!response.IsSuccessful()) return response;

        //TODO: Replace with your game's UI logic.
        KinoaUiService.Instance.RemoveGameInApps(messages.Select(x => x.Uuid).ToList(),
            nameof(response), nameof(DeleteInboxMessagesAsync));

        return response;
    }

    /// <summary>
    ///     Deletes all inbox messages.
    /// </summary>
    public async Task<Response<List<string>>> DeleteAllInboxMessagesAsync()
    {
        var response = await Kinoa.Messaging.DeleteAllInboxMessagesAsync();
        Log($"{nameof(DeleteAllInboxMessagesAsync)} status: {response.Status}.");
        if (!response.IsSuccessful()) return response;

        //TODO: Replace with your game's UI logic.
        KinoaUiService.Instance.ClearGameInApps(
            nameof(response), nameof(DeleteAllInboxMessagesAsync));

        return response;
    }

    /// <summary>
    ///     Updates a single inbox message with custom parameters, metrics, and countdown timer.
    /// </summary>
    public async Task<Response> UpdateInboxMessageAsync(InAppMessage message)
    {
        var endTimestampUpdate = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds();
        var customParams = new Dictionary<string, object>
        {
            ["custom_key_1"] = 1.0,
            ["custom_key_2"] = "custom_value_2",
            ["custom_key_3"] = true
        };

        message
            .SetCustomParameters(customParams)
            .SetViewsMetrics(message.InboxStats.Views + 1)
            .SetUsageMetrics(message.InboxStats.Usage + 1)
            .SetCountdownTimerEndTimestamp(endTimestampUpdate)
            .ResetRemindersMetrics();

        var response = await Kinoa.Messaging.UpdateInboxMessageAsync(message);
        Log($"{nameof(UpdateInboxMessageAsync)} status: {response.Status}.");

        return response;
    }

    /// <summary>
    ///     Updates multiple inbox messages.
    /// </summary>
    public async Task<Response> UpdateInboxMessagesAsync(List<InAppMessage> messages)
    {
        var endTimestampUpdate = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds();
        var customParams = new Dictionary<string, object>
        {
            ["ID"] = 1.0,
            ["custom_key_2"] = "custom_value_2",
            ["camelCase"] = true
        };

        foreach (var message in messages)
        {
            message
                .SetCustomParameters(customParams)
                .SetViewsMetrics(message.InboxStats.Views + 1)
                .SetUsageMetrics(message.InboxStats.Usage + 1)
                .SetCountdownTimerEndTimestamp(endTimestampUpdate)
                .ResetRemindersMetrics();
        }

        var response = await Kinoa.Messaging.UpdateInboxMessagesAsync(messages);
        Log($"{nameof(UpdateInboxMessagesAsync)} status: {response.Status}.");

        return response;
    }

    #endregion

    #region Eligibility

    /// <summary>
    ///     Uses the eligibility of the inbox In-app message.
    ///     Decreases remaining eligibility and auto-deletes the message when it reaches 0.
    /// </summary>
    public async Task<Response<InAppEligibilityUpdateResult>> UseInboxMessageEligibilityAsync(
        InAppMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Capping?.EligibilityLimit == null)
        {
            Log($"In-app '{message.Name}' '{message.Uuid}' has no eligibility limit (countdown timer only).");
            return null;
        }

        var response = await Kinoa.Messaging.UseInboxMessageEligibilityAsync(message, cancellationToken);
        if (response.IsSuccessful() && response.Data != null)
        {
            message.SetLocalEligibility(response.Data.ActualEligibility);

            if (response.Data.Processed)
            {
                //TODO: Give the reward to the Player.
                Log($"In-app '{message.Name}' '{message.Uuid}' eligibility used. " +
                    $"Remaining: {response.Data.ActualEligibility}, Deleted: {response.Data.Deleted}");
            }
            else
            {
                Log($"In-app '{message.Uuid}' eligibility limit reached. " +
                    $"Remaining: {response.Data.ActualEligibility}, Deleted: {response.Data.Deleted}");
            }

            if (response.Data.Deleted)
            {
                //TODO: Replace with your game's UI logic.
                KinoaUiService.Instance.RemoveGameInApp(message.Uuid,
                    nameof(response), nameof(UseInboxMessageEligibilityAsync));
            }
        }
        else
        {
            Log($"{nameof(UseInboxMessageEligibilityAsync)} status: {response.Status}.");
            if (response.Error != null)
            {
                Log($"Error: {response.Error.Code} — {response.Error.Message}");
                if (response.Error.Code == ResponseErrorCode.InAppNotFound)
                {
                    Log($"In-app '{message.Uuid}' not found in server inbox (deleted or expired).");
                    //TODO: Replace with your game's UI logic.
                    KinoaUiService.Instance.RemoveGameInApp(message.Uuid,
                        nameof(response), nameof(UseInboxMessageEligibilityAsync));
                }
            }
        }

        return response;
    }

    #endregion

    #region Create In-app

    /// <summary>
    ///     Creates a new In-app message by External Link.
    /// </summary>
    public async Task<Response<InAppMessage>> CreateInAppMessageAsync(string externalLink)
    {
        var response = await Kinoa.Messaging.CreateInAppMessageAsync(externalLink);
        if (response.IsSuccessful())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.CreateGameInApp(response.Data,
                nameof(response), nameof(CreateInAppMessageAsync), addToDisplayQueue: true);
        }
        else
        {
            Log($"{nameof(CreateInAppMessageAsync)} status: {response.Status}. " +
                $"Error: {response.Error?.Code.ToString() ?? "null"}");
        }

        return response;
    }

    /// <summary>
    ///     Creates a new In-app message by Push Notification.
    /// </summary>
    public async Task<Response<InAppMessage>> CreateInAppMessageAsync(InAppByPushCreationParams inAppCreationParams)
    {
        var response = await Kinoa.Messaging.CreateInAppMessageAsync(inAppCreationParams);
        if (response.IsSuccessful())
        {
            //TODO: Replace with your game's UI logic.
            KinoaUiService.Instance.CreateGameInApp(response.Data,
                nameof(response), nameof(CreateInAppMessageAsync), addToDisplayQueue: true);
            Log($"In-app '{inAppCreationParams.ID}' created by Push. Message UUID: {response.Data.Uuid}");
        }
        else
        {
            Log($"{nameof(CreateInAppMessageAsync)} status: {response.Status}. " +
                $"Error: {response.Error?.Code.ToString() ?? "null"}");
        }

        return response;
    }

    #endregion

    #region Milestones

    /// <summary>
    ///     Collects a single milestone from an inbox In-app message.
    /// </summary>
    public async Task<Response<InAppMilestonesCollectResult>> CollectSingleMilestoneAsync(
        InAppMessage message, uint milestoneIndex, CancellationToken cancellationToken = default)
    {
        return await CollectMilestonesCoreAsync(message, new List<uint> { milestoneIndex },
            "Single milestone collection", cancellationToken);
    }

    /// <summary>
    ///     Collects multiple milestones from an inbox In-app message.
    /// </summary>
    public async Task<Response<InAppMilestonesCollectResult>> CollectMultipleMilestonesAsync(
        InAppMessage message, List<uint> milestoneIndexes, CancellationToken cancellationToken = default)
    {
        return await CollectMilestonesCoreAsync(message, milestoneIndexes,
            "Multiple milestones collection", cancellationToken);
    }

    /// <summary>
    ///     Collects all milestones from an inbox In-app message.
    /// </summary>
    public async Task<Response<InAppMilestonesCollectResult>> CollectAllMilestonesAsync(
        InAppMessage message, CancellationToken cancellationToken = default)
    {
        var milestonesData = GetMilestonesFeature(message);
        if (milestonesData == null) return null;

        var allIndexes = Enumerable.Range(0, milestonesData.Steps.Count).Select(i => (uint)i).ToList();
        return await CollectMilestonesCoreAsync(message, allIndexes,
            "All milestones collection", cancellationToken);
    }

    /// <summary>
    ///     Collects all milestones from multiple inbox In-app messages in parallel.
    /// </summary>
    public async Task CollectAllMilestonesFromMultipleInAppsAsync(
        IEnumerable<InAppMessage> messages, CancellationToken cancellationToken = default)
    {
        var inAppMessages = messages?.ToList();
        if (inAppMessages == null || !inAppMessages.Any())
        {
            Debug.LogWarning("Cannot collect milestones: no In-app messages provided.");
            return;
        }

        Debug.Log($"Collecting all milestones from {inAppMessages.Count} In-app messages...");
        await Task.WhenAll(inAppMessages.Select(m => CollectAllMilestonesAsync(m, cancellationToken)));
        Debug.Log($"Completed collecting all milestones from {inAppMessages.Count} In-app messages.");
    }

    /// <summary>
    ///     Validates milestone indexes against the available milestones.
    /// </summary>
    private static List<uint> ValidateMilestoneIndexes(
        List<uint> milestoneIndexes, InAppMilestonesFeature milestonesData, string messageName)
    {
        if (milestoneIndexes == null || !milestoneIndexes.Any())
        {
            Debug.LogWarning($"No milestone indexes provided for '{messageName}'.");
            return new List<uint>();
        }

        var validIndexes = milestoneIndexes.Where(i => i < milestonesData.Steps.Count).ToList();
        var invalidIndexes = milestoneIndexes.Where(i => i >= milestonesData.Steps.Count).ToList();

        if (invalidIndexes.Any())
            Debug.LogWarning($"Invalid milestone indexes for '{messageName}': " +
                             $"{string.Join(", ", invalidIndexes)}. Range: 0-{milestonesData.Steps.Count - 1}");

        return validIndexes;
    }

    /// <summary>
    ///     Core method to collect milestones with validation and reward processing.
    /// </summary>
    private async Task<Response<InAppMilestonesCollectResult>> CollectMilestonesCoreAsync(
        InAppMessage message, List<uint> milestoneIndexes, string operationName,
        CancellationToken cancellationToken = default)
    {
        var milestonesData = GetMilestonesFeature(message);
        if (milestonesData == null) return null;

        var validIndexes = ValidateMilestoneIndexes(milestoneIndexes, milestonesData, message.Name);
        if (!validIndexes.Any()) return null;

        var response = await Kinoa.Messaging.CollectMilestonesAsync(message, validIndexes, cancellationToken);
        if (response.IsSuccessful() && response.Data != null)
        {
            if (response.Data.Collected?.Any() == true)
            {
                message.SetMilestonesStatusAsCollected(response.Data.Collected);
                CollectMilestonesReward(milestonesData, response.Data.Collected);

                Log($"{operationName} for '{message.Name}' '{message.Uuid}': " +
                    $"Collected [{string.Join(", ", response.Data.Collected)}]");
            }

            if (response.Data.NotCollected?.Any() == true)
            {
                Log($"{operationName} for '{message.Name}' '{message.Uuid}': " +
                    $"Not collected [{string.Join(", ", response.Data.NotCollected)}]");
            }
        }
        else
        {
            Log($"{operationName} status: {response.Status}.");
        }

        return response;
    }

    /// <summary>
    ///     Grants the rewards for the collected milestones.
    /// </summary>
    private static void CollectMilestonesReward(InAppMilestonesFeature milestonesData, List<uint> collectedIndexes)
    {
        foreach (var index in collectedIndexes.Where(i => i < milestonesData.Steps.Count))
        {
            var step = milestonesData.Steps[(int)index];

            //TODO: Update the Player State with the reward for the collected milestone.
            if (step.Button?.Resources != null)
            {
                var rewards = step.Button.Resources.Select(r => $"{r.ResourceKey}: {r.Amount}");
                Debug.Log($"Milestone [{index}] reward: {string.Join(", ", rewards)}");
            }

            if (step.Button?.Packages != null)
            {
                var packages = step.Button.Packages;
                if (!string.IsNullOrEmpty(packages.IosPackageID))
                    Debug.Log($"Milestone [{index}] iOS package: {packages.IosPackageID}");
                if (!string.IsNullOrEmpty(packages.AndroidPackageID))
                    Debug.Log($"Milestone [{index}] Android package: {packages.AndroidPackageID}");
                if (!string.IsNullOrEmpty(packages.IosDiscountPackageID))
                    Debug.Log($"Milestone [{index}] iOS discount package: {packages.IosDiscountPackageID}");
                if (!string.IsNullOrEmpty(packages.AndroidDiscountPackageID))
                    Debug.Log($"Milestone [{index}] Android discount package: {packages.AndroidDiscountPackageID}");
            }
        }
    }

    /// <summary>
    ///     Extracts the milestones feature from an In-app message.
    /// </summary>
    private static InAppMilestonesFeature GetMilestonesFeature(InAppMessage message)
    {
        var milestonesData = (message.Data as InAppCustomTemplateData)?.Feature as InAppMilestonesFeature;
        if (milestonesData == null)
            Debug.LogWarning($"In-app '{message.Name}' '{message.Uuid}' has no milestones feature.");

        return milestonesData;
    }

    #endregion

    #region Logging

    /// <summary>
    ///     Logs a message.
    /// </summary>
    private void Log(string message, List<InAppMessage> inApps = null)
    {
        Debug.Log(message);
    }

    /// <summary>
    ///     Logs the received In-app messages overview.
    /// </summary>
    public void LogInAppMessages(List<InAppMessage> messages, string logMessage = null)
    {
        if (messages == null)
            return;

        var uuids = messages.Count > 0
            ? string.Join(", ", messages.Select(m => m.Uuid))
            : "none";

        Log($"{logMessage}{nameof(InAppMessages)} received ({messages.Count}): {uuids}", messages);

        if (KinoaSdkInitService.LogLevel != LogLevel.Trace)
            return;

        foreach (var inApp in messages)
            LogInAppDetails(inApp);
    }

    /// <summary>
    ///     Logs detailed In-app message properties (Trace level).
    ///     Demonstrates how to access all In-app message fields.
    /// </summary>
    private void LogInAppDetails(InAppMessage inApp)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== In-app Message Details ===");

        // Core properties
        sb.AppendLine($"Uuid: {inApp.Uuid}");
        sb.AppendLine($"MessageId: {inApp.MessageId}");
        sb.AppendLine($"FlowId: {inApp.FlowId}");
        sb.AppendLine($"Name: {inApp.Name}");
        sb.AppendLine($"Order: {inApp.Order}");
        sb.AppendLine($"SentTime: {inApp.SentTime}");
        sb.AppendLine($"IsInboxMessage: {inApp.IsInboxMessage}");
        sb.AppendLine($"IsTriggeredOffline: {inApp.IsTriggeredOffline}");

        // Command
        if (inApp.Command != null)
        {
            sb.Append("Command: ");
            sb.AppendLine(inApp.Command switch
            {
                InAppReminderCommand => nameof(InAppReminderCommand),
                InAppReplacedCommand cmd => $"{nameof(InAppReplacedCommand)} " +
                    $"(ReplacedUuid: {cmd.ReplacedUuid})",
                InAppScoreChangedCommand cmd => $"{nameof(InAppScoreChangedCommand)} " +
                    $"(DisplayOnProgressChange: {cmd.DisplayOnProgressChange})",
                InAppMilestonesProgressChangedCommand cmd => $"{nameof(InAppMilestonesProgressChangedCommand)} " +
                    $"(DisplayOnProgressChange: {cmd.DisplayOnProgressChange})",
                InAppInstanceUpdateCommand => nameof(InAppInstanceUpdateCommand),
                _ => $"Unknown ({inApp.Command.GetType().Name})"
            });
        }

        // Template data
        switch (inApp.Data)
        {
            case InAppSimpleTemplateData simple:
                LogSimpleTemplate(sb, simple);
                break;
            case InAppCustomTemplateData custom:
                LogCustomTemplate(sb, custom);
                break;
        }

        // Lobby icon
        if (inApp.LobbyIcon != null)
        {
            LogImage(sb, "LobbyIcon", inApp.LobbyIcon);
            sb.AppendLine($"\tIsInAppTrigger={inApp.LobbyIcon.IsInAppTrigger}, Text={inApp.LobbyIcon.Text}, Score={Convert.ToString(inApp.LobbyIcon.Score)}");
        }

        // Placement
        if (inApp.Placement != null)
            sb.AppendLine($"Placement: {Convert.ToString(inApp.Placement.Id)}");

        // Countdown timer
        if (inApp.CountdownTimer != null)
        {
            var ct = inApp.CountdownTimer;
            sb.AppendLine($"CountdownTimer: EndTimestamp={ct.EndTimestamp}, IsExpired={ct.IsExpired}, " +
                          $"IsVisible={ct.IsVisible}, ExtraLifeTime={ct.ExtraLifeTime}, " +
                          $"EndWithExtra={ct.EndTimestampWithExtraLifeTime}");
        }

        // Capping
        if (inApp.Capping != null)
        {
            sb.Append($"Capping: TotalLimit={inApp.Capping.TotalLimit}");
            if (inApp.Capping.EligibilityLimit != null)
                sb.Append($", Eligibility(Original={inApp.Capping.EligibilityLimit.Original}, " +
                          $"Actual={inApp.Capping.EligibilityLimit.Actual}, " +
                          $"IsUsed={inApp.Capping.EligibilityLimit.IsEligibilityUsed})");
            if (inApp.Capping.RecurrentLimit != null)
                sb.Append($", Recurrent(Amount={inApp.Capping.RecurrentLimit.Amount}, " +
                          $"Period={inApp.Capping.RecurrentLimit.Period})");
            if (inApp.Capping.Cooldown != null)
                sb.Append($", Cooldown(Period={inApp.Capping.Cooldown.Period})");
            if (inApp.Capping.SessionCooldown != null)
                sb.Append($", SessionCooldown(Count={inApp.Capping.SessionCooldown.Count})");
            sb.AppendLine();
        }

        // Scheduling
        if (inApp.Scheduling != null)
        {
            sb.Append($"Scheduling: Start={inApp.Scheduling.StartTimeMs}");
            if (inApp.Scheduling.EndTimeMs != null)
                sb.Append($", End={inApp.Scheduling.EndTimeMs}");
            sb.AppendLine();
        }

        // Progression score
        if (inApp.ProgressionScore != null)
            sb.AppendLine($"ProgressionScore: Current={inApp.ProgressionScore.Current}, " +
                          $"Previous={inApp.ProgressionScore.Previous}, Total={inApp.ProgressionScore.Total}");

        // Inbox stats
        if (inApp.InboxStats != null)
            sb.AppendLine($"InboxStats: Views={inApp.InboxStats.Views}, Usage={inApp.InboxStats.Usage}, " +
                          $"Reminders={inApp.InboxStats.Reminders}");

        // Segmentation
        var audiences = inApp.Audiences.Where(x => x.Value).Select(x => x.Key).ToList();
        if (audiences.Any()) sb.AppendLine($"Audiences: {string.Join(", ", audiences)}");
        var userLists = inApp.UserLists.Where(x => x.Value).Select(x => x.Key).ToList();
        if (userLists.Any()) sb.AppendLine($"UserLists: {string.Join(", ", userLists)}");
        if (inApp.AbTestDistribution != null)
        {
            var ab = inApp.AbTestDistribution;
            sb.AppendLine($"ABTest: Name={ab.AbTestName}, Group={ab.AbTestGroupName}, " +
                          $"Id={ab.AbTestId}, GroupId={ab.AbTestGroupId}");
        }

        // Configuration filters
        if (inApp.ConfigurationFilters.Any())
            sb.AppendLine($"ConfigurationFilters: {string.Join(", ", inApp.ConfigurationFilters.Select(x => $"{x.Key}={x.Value}"))}");
        if (inApp.ConfiguredFilters.Any())
            sb.AppendLine($"ConfiguredFilters: {string.Join(", ", inApp.ConfiguredFilters.Select(x => $"{x.Key}={x.Value}"))}");

        // Extra fields
        if (inApp.Extra?.Any() == true)
            sb.AppendLine($"Extra: {string.Join(", ", inApp.Extra.Select(e => $"{e.Name}={e.Value}"))}");

        // Custom parameters
        LogCustomFields(sb, "CustomParams", inApp.CustomParams);

        // Feature configurations
        if (inApp.FeatureConfigurations?.Any() == true)
        {
            sb.AppendLine($"FeatureConfigurations ({inApp.FeatureConfigurations.Count}):");
            var dailyBonuses = inApp.GetFeatureConfigurations<InAppDailyBonusFeatureConfiguration>();
            foreach (var bonus in dailyBonuses)
                sb.AppendLine($"\tDailyBonus: Coins={bonus.Coins}, LevelFilter={bonus.LevelFilter}");
        }

        // Bundle resources
        if (inApp.BundleResources?.Any() == true)
        {
            sb.AppendLine($"BundleResources ({inApp.BundleResources.Count}):");
            foreach (var (bundleKey, resources) in inApp.BundleResources)
                foreach (var r in resources)
                    sb.AppendLine($"\t[{bundleKey}] {r.ResourceKey}: {r.Amount} (Body: {r.Body})");
        }

        // Security
        sb.AppendLine($"Security: Checksum={inApp.SecurityData.Checksum}, " +
                      $"SequenceId={inApp.SequenceData.SequenceId}, " +
                      $"StateChangeSequenceId={inApp.SequenceData.StateChangeSequenceId}");

        Debug.Log(sb.ToString());
    }

    #endregion

    #region Logging Helpers

    private static void LogSimpleTemplate(StringBuilder sb, InAppSimpleTemplateData simple)
    {
        sb.AppendLine("Template: Simple");
        LogImage(sb, "\tPortraitImage", simple.MainPortraitImage);
        LogImage(sb, "\tLandscapeImage", simple.MainLandscapeImage);
        LogClickConfig(sb, "\tClickConfig", simple.ClickConfig);
        LogResources(sb, "\tResources", simple.Resources);
        LogPackages(sb, "\tPackages", simple.Packages);
    }

    private static void LogCustomTemplate(StringBuilder sb, InAppCustomTemplateData custom)
    {
        sb.AppendLine($"Template: Custom (Key: {custom.TemplateKey})");
        LogButtons(sb, custom.Buttons);
        LogImages(sb, custom.Images);
        LogTexts(sb, custom.Texts);
        LogCustomElements(sb, custom.Customs);
        LogMilestonesFeature(sb, custom.Feature as InAppMilestonesFeature);
    }

    private static void LogImage(StringBuilder sb, string prefix, InAppImage image)
    {
        if (image == null) return;
        sb.AppendLine($"{prefix}: {image.Content} ({image.ContentType})");
    }

    private static void LogButtons(StringBuilder sb, Dictionary<string, InAppCustomButton> buttons)
    {
        if (buttons == null) return;
        foreach (var (key, btn) in buttons)
        {
            sb.AppendLine($"\tButton[{key}]: Label={btn.Label}");
            LogImage(sb, "\t\tBackgroundImage", btn.BackgroundImage);
            LogClickConfig(sb, "\t\tClickConfig", btn.ClickConfig);
            LogResources(sb, "\t\tResources", btn.Resources);
            LogPackages(sb, "\t\tPackages", btn.Packages);
            LogCustomFields(sb, "\t\tCustomFields", btn.CustomFields);
        }
    }

    private static void LogImages(StringBuilder sb, Dictionary<string, InAppCustomImage> images)
    {
        if (images == null) return;
        foreach (var (key, img) in images)
        {
            LogImage(sb, $"\tImage[{key}]", img);
            LogCustomFields(sb, "\t\tCustomFields", img.CustomFields);
        }
    }

    private static void LogTexts(StringBuilder sb, Dictionary<string, InAppCustomText> texts)
    {
        if (texts == null) return;
        foreach (var (key, txt) in texts)
        {
            sb.AppendLine($"\tText[{key}]: {txt.Content}");
            LogCustomFields(sb, "\t\tCustomFields", txt.CustomFields);
        }
    }

    private static void LogCustomElements(StringBuilder sb, Dictionary<string, InAppCustomElement> customs)
    {
        if (customs == null) return;
        foreach (var (key, elem) in customs)
        {
            sb.AppendLine($"\tCustom[{key}]: {elem.Value}");
            LogCustomFields(sb, "\t\tCustomFields", elem.CustomFields);
        }
    }

    private static void LogMilestonesFeature(StringBuilder sb, InAppMilestonesFeature milestones)
    {
        if (milestones == null) return;
        sb.AppendLine($"\tMilestones: Progress={milestones.Progress}, Previous={milestones.PreviousProgress}");
        for (var i = 0; i < milestones.Steps.Count; i++)
        {
            var step = milestones.Steps[i];
            sb.AppendLine($"\t\tStep[{i}]: Score={step.Score}, Status={step.Status}");
            LogCustomFields(sb, "\t\t\tCustomFields", step.CustomFields);
        }
    }

    private static void LogClickConfig(StringBuilder sb, string prefix, InAppClickConfiguration click)
    {
        if (click == null) return;
        sb.Append($"{prefix} Action: ");
        sb.AppendLine(click switch
        {
            InAppCloseClickConfiguration => nameof(InAppCloseClickConfiguration),
            InAppCollectResourceClickConfiguration => nameof(InAppCollectResourceClickConfiguration),
            InAppBillingClickConfiguration => nameof(InAppBillingClickConfiguration),
            InAppWebLinkClickConfiguration link => $"{nameof(InAppWebLinkClickConfiguration)} " +
                $"(Web Link: {link.Link})",
            InAppDeepLinkClickConfiguration link => $"{nameof(InAppDeepLinkClickConfiguration)} " +
                $"(Deep Link: {link.Link})",
            InAppSoftBillingClickConfiguration soft => $"{nameof(InAppSoftBillingClickConfiguration)} " +
                $"(Price: {string.Join(", ", soft.PriceResources.Select(r => $"{r.ResourceKey}:{r.Amount}"))})",
            InAppShowAdClickConfiguration => nameof(InAppShowAdClickConfiguration),
            InAppPromiseRewardsClickConfiguration => nameof(InAppPromiseRewardsClickConfiguration),
            InAppUpdateAppVersionClickConfiguration => nameof(InAppUpdateAppVersionClickConfiguration),
            _ => $"Unknown ({click.GetType().Name})"
        });
    }

    private static void LogResources(StringBuilder sb, string prefix, List<Resource> resources)
    {
        if (resources == null || !resources.Any()) return;
        foreach (var r in resources)
            sb.AppendLine($"{prefix}: {r.ResourceKey}={r.Amount} (Body: {r.Body})");
    }

    private static void LogPackages(StringBuilder sb, string prefix, InAppStorePackages packages)
    {
        if (packages == null) return;
        sb.AppendLine($"{prefix}:");
        sb.AppendLine($"\tIosPackageID: {packages.IosPackageID}");
        sb.AppendLine($"\tIosDiscountPackageID: {packages.IosDiscountPackageID}");
        sb.AppendLine($"\tAndroidPackageID: {packages.AndroidPackageID}");
        sb.AppendLine($"\tAndroidDiscountPackageID: {packages.AndroidDiscountPackageID}");
    }

    private static void LogCustomFields(StringBuilder sb, string prefix, Dictionary<string, object> fields)
    {
        if (fields == null || !fields.Any()) return;
        sb.AppendLine($"{prefix}:");
        foreach (var (key, value) in fields)
        {
            var display = value switch
            {
                InAppCustomFieldOfTypeImage img => $"Image[{img.Image?.Content}, {img.Image?.ContentType}]",
                null => "null",
                _ => value.ToString()
            };
            sb.AppendLine($"{prefix}\t{key}: {display}");
        }
    }

    #endregion
}
