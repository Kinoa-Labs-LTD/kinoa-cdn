using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Constants;
using Kinoa.Data.Messaging.InApp;
using Kinoa.Data.Messaging.InApp.Templates.Custom;
using Kinoa.Data.Messaging.InApp.Templates.Simple;
using Kinoa.Data.ResourceManagement;
using UnityEngine;

namespace Core.Services
{
    /// <summary>
    ///     Stub implementation of the In-app UI service.
    ///     This is a mock API — all methods log to console only.
    ///     Replace with your game's UI logic: create, display, replace, and remove In-app game objects.
    /// </summary>
    public class KinoaUiService : KinoaSingleton<KinoaUiService>
    {
        /// <summary>
        ///     Tries to display the queued In-app messages.
        /// </summary>
        public void TryDisplayGameInApps()
        {
            //TODO: Display queued In-app messages in your game's UI.
            Debug.Log("[KinoaUiService] TryDisplayGameInApps");
        }

        /// <summary>
        ///     Creates a single In-app message Game object.
        /// </summary>
        public void CreateGameInApp(InAppMessage inAppMessage, string source, string reason,
            bool addToDisplayQueue)
        {
            //TODO: Create UI element for the In-app message.
            Debug.Log($"[KinoaUiService] CreateGameInApp: {ToLogString(inAppMessage)} (source: {source}, reason: {reason}, addToDisplayQueue: {addToDisplayQueue})");
        }

        /// <summary>
        ///     Creates multiple In-app message Game objects.
        /// </summary>
        public void CreateGameInApps(IEnumerable<InAppMessage> inAppMessages, string source, string reason,
            bool addToDisplayQueue)
        {
            //TODO: Create UI elements for the In-app messages.
            Debug.Log($"[KinoaUiService] CreateGameInApps: [{ToLogString(inAppMessages)}] (source: {source}, reason: {reason}, addToDisplayQueue: {addToDisplayQueue})");
        }

        /// <summary>
        ///     Removes a single In-app message Game object by UUID.
        /// </summary>
        public void RemoveGameInApp(string inAppMessageUuid, string source, string reason)
        {
            //TODO: Remove the In-app UI element.
            Debug.Log($"[KinoaUiService] RemoveGameInApp: {inAppMessageUuid} (source: {source}, reason: {reason})");
        }

        /// <summary>
        ///     Removes multiple In-app message Game objects by UUIDs.
        /// </summary>
        public void RemoveGameInApps(IEnumerable<string> inAppMessageUuids, string source, string reason)
        {
            //TODO: Remove the In-app UI elements.
            Debug.Log($"[KinoaUiService] RemoveGameInApps: [{ToLogString(inAppMessageUuids)}] (source: {source}, reason: {reason})");
        }

        /// <summary>
        ///     Replaces In-app message Game objects (removes old, creates new).
        /// </summary>
        public void ReplaceGameInApps(IEnumerable<InAppMessage> newInAppMessages, string source, string reason,
            bool addToDisplayQueue)
        {
            //TODO: Remove old In-app UI elements and create new ones.
            Debug.Log($"[KinoaUiService] ReplaceGameInApps: [{ToLogString(newInAppMessages)}] (source: {source}, reason: {reason}, addToDisplayQueue: {addToDisplayQueue})");
        }

        /// <summary>
        ///     Clears all In-app message Game objects.
        /// </summary>
        public void ClearGameInApps(string source, string reason)
        {
            //TODO: Remove all In-app UI elements.
            Debug.Log($"[KinoaUiService] ClearGameInApps (source: {source}, reason: {reason})");
        }

        #region Button Click Handling

        /// <summary>
        ///     Entry point — routes an In-app click by template family, then by ClickConfig type.
        ///     Game integrators extend by adding template_keys to <see cref="IsKnownCustomTemplateKey"/>
        ///     (entries from <see cref="KinoaInAppTemplateConstants"/>) and filling TODO blocks
        ///     in <see cref="RouteByClickConfigAsync"/>.
        /// </summary>
        //TODO: Wire this handler to your popup view's button OnClick events. Example:
        //          ctaButton.onClick.AddListener(() =>
        //              _ = KinoaUiService.Instance.HandleInAppButtonClickAsync(message, "main_cta"));
        public Task HandleInAppButtonClickAsync(InAppMessage message, string buttonKey = null,
            CancellationToken cancellationToken = default)
        {
            switch (message?.Data)
            {
                case InAppSimpleTemplateData simple:
                    return RouteByClickConfigAsync(message, simple.ClickConfig, simple.Resources,
                        simple.Packages, cancellationToken);

                case InAppCustomTemplateData custom when IsKnownCustomTemplateKey(custom.TemplateKey) &&
                    custom.Buttons != null && custom.Buttons.TryGetValue(buttonKey, out var button):
                    return RouteByClickConfigAsync(message, button.ClickConfig, button.Resources,
                        button.Packages, cancellationToken);

                default:
                    Debug.Log($"[KinoaUiService] Click ignored for In-app '{message?.Uuid}' " +
                              $"(unknown template_key, missing button, or unsupported Data type).");
                    return Task.CompletedTask;
            }
        }

        /// <summary>
        ///     Allowlist of supported custom template_keys. Extend with Dashboard-defined keys per game
        ///     (declare each key in <see cref="KinoaInAppTemplateConstants"/> and add an arm here).
        /// </summary>
        private static bool IsKnownCustomTemplateKey(string templateKey) => templateKey switch
        {
            KinoaInAppTemplateConstants.TemplateKeyOneCtaPredefined => true,
            //TODO: Add your game-custom keys, e.g.:
            // KinoaInAppTemplateConstants.TemplateKeyWeeklyOffer => true,
            _ => false,
        };

        /// <summary>
        ///     Dispatches a click by its <see cref="InAppClickConfiguration"/> runtime type.
        ///     <paramref name="resources"/> and <paramref name="packages"/> come from the button
        ///     (Custom template) or directly from the Simple template data. Game integrators fill the
        ///     TODO blocks with ad/IAP/deeplink/reward code.
        /// </summary>
        private async Task RouteByClickConfigAsync(InAppMessage message, InAppClickConfiguration clickConfig,
            List<Resource> resources, InAppStorePackages packages, CancellationToken cancellationToken)
        {
            switch (clickConfig)
            {
                case InAppCloseClickConfiguration:
                    RemoveGameInApp(message.Uuid, nameof(HandleInAppButtonClickAsync),
                        nameof(InAppCloseClickConfiguration));
                    return;

                case InAppCollectResourceClickConfiguration:
                    // No game-side pre-action — grant at the end of the method.
                    break;

                case InAppBillingClickConfiguration:
                    //TODO: Launch IAP using packages. Return early if IAP failed.
                    Debug.Log($"[KinoaUiService] Billing clicked: iOS={packages?.IosPackageID} (discount={packages?.IosDiscountPackageID}), " +
                              $"Android={packages?.AndroidPackageID} (discount={packages?.AndroidDiscountPackageID}).");
                    break;

                case InAppSoftBillingClickConfiguration soft:
                    //TODO: Validate + deduct soft.PriceResources from player state. Return early if deduction failed.
                    Debug.Log($"[KinoaUiService] Soft billing clicked: price=[" +
                              $"{string.Join(", ", soft.PriceResources.Select(r => $"{r.ResourceKey}:{r.Amount}"))}].");
                    break;

                case InAppPromiseRewardsClickConfiguration:
                    // IMPORTANT: do NOT call GrantRewards here — the SDK delivers the reward later
                    // via a follow-up in-app. Only TryUseEligibilityAsync runs.
                    await TryUseEligibilityAsync(message, cancellationToken);
                    return;

                case InAppUpdateAppVersionClickConfiguration:
                    //TODO: Open the platform-specific app store update page (UnityEngine.Application.OpenURL).
                    return;

                case InAppDeepLinkClickConfiguration deepLink:
                    //TODO: Navigate via your deep-link router using deepLink.Link.
                    Debug.Log($"[KinoaUiService] Deep link clicked: {deepLink.Link}.");
                    break;

                case InAppWebLinkClickConfiguration webLink:
                    //TODO: Open webLink.Link (UnityEngine.Application.OpenURL or your in-app browser).
                    Debug.Log($"[KinoaUiService] Web link clicked: {webLink.Link}.");
                    break;

                case InAppShowAdClickConfiguration:
                    //TODO: Show a rewarded ad. Return early if the ad failed.
                    //      Alternative (offline-tolerant): swap order — call GrantRewards (below) immediately,
                    //      then fire-and-forget TryUseEligibilityAsync via a local eligibility-debt cache.
                    break;

                default:
                    Debug.Log($"[KinoaUiService] Unhandled ClickConfig: {clickConfig?.GetType().Name ?? "null"}.");
                    return;
            }

            // Shared tail: every consuming click above falls through to server-confirm + grant.
            if (await TryUseEligibilityAsync(message, cancellationToken)) GrantRewards(resources);
        }

        /// <summary>
        ///     Consumes one eligibility on the server (or deletes the message if it has no
        ///     <c>EligibilityLimit</c>). Returns <c>true</c> iff the server confirmed consumption.
        ///     UI removal on success is handled by the MessagingService wrappers.
        /// </summary>
        private async Task<bool> TryUseEligibilityAsync(InAppMessage message,
            CancellationToken cancellationToken)
        {
            if (message.Capping?.EligibilityLimit == null)
                return (await KinoaMessagingService.Instance.DeleteInboxMessageAsync(message)).IsSuccessful();

            var response = await KinoaMessagingService.Instance
                .UseInboxMessageEligibilityAsync(message, cancellationToken);
            return response != null && response.IsSuccessful() && response.Data is { Processed: true };
        }

        /// <summary>
        ///     Grants rewards to the player. Sample logs only — replace with your economy.
        /// </summary>
        private void GrantRewards(List<Resource> resources)
        {
            //TODO: Apply resources to your player's economy. Typically:
            //          foreach (var r in resources) playerState.Add(r.ResourceKey, r.Amount);
            var summary = resources?.Any() == true
                ? string.Join(", ", resources.Select(r => $"{r.ResourceKey}: {r.Amount}"))
                : "no resources";
            Debug.Log($"[KinoaUiService] Reward granted (sample log only): {summary}.");
        }

        #endregion

        private static string ToLogString(InAppMessage inApp) =>
            inApp == null ? "null" : $"{inApp.Uuid} ({inApp.Name})";

        private static string ToLogString(IEnumerable<InAppMessage> inApps) =>
            inApps == null ? string.Empty : string.Join(", ", inApps.Select(ToLogString));

        private static string ToLogString(IEnumerable<string> uuids) =>
            uuids == null ? string.Empty : string.Join(", ", uuids);
    }
}
