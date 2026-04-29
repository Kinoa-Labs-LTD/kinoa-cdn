using System.Collections.Generic;
using System.Linq;
using Kinoa.Data.Messaging.InApp;
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

        private static string ToLogString(InAppMessage inApp) =>
            inApp == null ? "null" : $"{inApp.Uuid} ({inApp.Name})";

        private static string ToLogString(IEnumerable<InAppMessage> inApps) =>
            inApps == null ? string.Empty : string.Join(", ", inApps.Select(ToLogString));

        private static string ToLogString(IEnumerable<string> uuids) =>
            uuids == null ? string.Empty : string.Join(", ", uuids);
    }
}
