using System.Text.Json.Serialization;
using Kinoa.Data.Messaging.InApp;

/// <summary>
///     Sample In-app Feature Configuration data model for "DailyBonus" schema.
///     TODO: Replace with your actual In-app Feature Configuration schema.
///     TODO: Register before SDK.Initialize().
/// </summary>
public class InAppDailyBonusFeatureConfiguration : InAppFeatureConfiguration
{
    /// <summary>
    ///     Coins reward amount.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Coins")]
    public double Coins { get; private set; }

    /// <summary>
    ///     Player Level filter (exact match).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("filter: Level")]
    public float? LevelFilter { get; private set; }

    /// <summary>
    ///     Player Level filter range — from (inclusive).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("filter: Level:from")]
    public float? LevelFilterFrom { get; private set; }

    /// <summary>
    ///     Player Level filter range — to (inclusive).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("filter: Level:to")]
    public float? LevelFilterTo { get; private set; }
}