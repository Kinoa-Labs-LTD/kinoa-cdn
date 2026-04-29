using Kinoa.Data.Messaging.InApp;
using UnityEngine;

namespace Core.Data
{
    /// <summary>
    ///     The sample class for the In-app message <see cref="InAppMessage"/> Game Object.
    /// </summary>
    /// TODO: Implement the In-app message Game Object that represents the In-app message on the UI.
    public class InAppGameObject //: TODO: MonoBehaviour
    {
        /// <summary>
        ///     The In-app message data.
        /// </summary>
        public InAppMessage InAppMessage { get; private set; }

        /// <summary>
        ///     Initialize a new instance of <see cref="InAppGameObject"/>.
        /// </summary>
        /// <param name="inAppMessage">The In-app message data.</param>
        public InAppGameObject(InAppMessage inAppMessage)
        {
            //TODO: Create the In-app on UI, download the In-app content, set the lobby icon, etc.
            InAppMessage = inAppMessage;
        }

        /// <summary>
        ///     Displays the Game In-app message.
        /// </summary>
        public void Display()
        {
            //TODO: Implement the In-app message display logic instead of the debug log.
            Debug.Log($"The In-app message {InAppMessage.Uuid} is displayed.");
            KinoaGameEventsService.Instance.SendInAppImpressionEvent(InAppMessage);
        }

        /// <summary>
        ///     Closes the Game In-app message.
        /// </summary>
        public void Close()
        {
            //TODO: Implement the In-app message close logic.
            KinoaGameEventsService.Instance.SendInAppCloseEvent(InAppMessage);
        }

        /// <summary>
        ///     Click the Game In-app message CTA.
        /// </summary>
        public async void Click()
        {
            //TODO: Implement the In-app message click (CTA) logic.
            await KinoaMessagingService.Instance.UseInboxMessageEligibilityAsync(InAppMessage);
            KinoaGameEventsService.Instance.SendInAppClickEvent(InAppMessage);
        }
    }
}