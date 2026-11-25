using System.Text.Json.Serialization;

/// <summary>
///     Sample Daily Bonus Feature Settings data model.
///     TODO: Replace with your actual Feature Settings data model.
/// </summary>
public class DailyBonusSettings : FeatureSettingsData
{
    /// <summary>
    ///     Coins reward amount.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Coins")]
    public double Coins { get; private set; }

    // --- Filter properties (opt-in via IncludeFilters: true) ---
    // Filter fields use "filter: " prefix in JsonPropertyName.
    // Range filters use ":from" / ":to" suffixes.

    /// <summary>
    ///     Configuration filter: Player Level (exact match).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("filter: Level")]
    public float LevelFilter { get; private set; }

    /// <summary>
    ///     Configuration filter: Purchases count (exact match).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("filter: Purchases count")]
    public float PurchasesCountFilter { get; private set; }

    /// <summary>
    ///     Configuration filter: Average purchase amount — range from (inclusive).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("filter: Average purchase amount:from")]
    public float AveragePurchaseAmountFilterFrom { get; private set; }

    /// <summary>
    ///     Configuration filter: Average purchase amount — range to (inclusive).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("filter: Average purchase amount:to")]
    public float AveragePurchaseAmountFilterTo { get; private set; }
}
