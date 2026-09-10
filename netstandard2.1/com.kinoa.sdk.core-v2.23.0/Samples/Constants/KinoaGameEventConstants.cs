namespace Core.Constants
{
    /// <summary>
    ///     Kinoa game-event constants — centralized event names and parameter keys for
    ///     custom events and custom params added to predefined events. Single source of
    ///     truth prevents typos and keeps Dashboard registration easy to track.
    ///     Extend with your custom event names and parameter keys.
    /// </summary>
    public static class KinoaGameEventConstants
    {
        // Custom event names — register on Dashboard → Game Settings → Custom Events.
        //TODO: Add your custom event names here, e.g.:
        // public const string EventName_PurchaseSucceed = "on_purchase_succeed";
        // public const string EventName_EpisodeReached  = "episode_reached";

        // Parameter keys — used in customParams dict on ANY event (custom OR predefined),
        // or in .AddCustomParameter("key", value) calls.
        //TODO: Add your parameter keys here, e.g.:
        // public const string ParamKey_LevelId       = "level_id";
        // public const string ParamKey_JourneyLevel  = "journey_level";
    }
}
