namespace Core.Constants
{
    /// <summary>
    ///     Kinoa in-app template_key constants — predefined templates shipped by the SDK
    ///     and game-custom Dashboard-defined templates registered on Kinoa Dashboard.
    ///     Extend with your custom template_keys as your game registers more on the Dashboard.
    /// </summary>
    public static class KinoaInAppTemplateConstants
    {
        /// <summary>
        ///     Predefined Kinoa template_key for the Simple template — deserializes as
        ///     <see cref="Kinoa.Data.Messaging.InApp.Templates.Simple.InAppSimpleTemplateData"/>
        ///     (distinct data type, no buttons map).
        /// </summary>
        public const string TemplateKeySimple = "simple";

        /// <summary>
        ///     Predefined Kinoa template_key for the One-CTA custom template — deserializes as
        ///     <see cref="Kinoa.Data.Messaging.InApp.Templates.Custom.InAppCustomTemplateData"/>
        ///     with this key.
        /// </summary>
        public const string TemplateKeyOneCtaPredefined = "one_cta_predefined";

        //TODO: Add your game-custom Dashboard-defined template_keys here, e.g.:
        // public const string TemplateKeyWeeklyOffer = "weekly_offer";
        // public const string TemplateKeySeasonPass  = "season_pass";
    }
}
