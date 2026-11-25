using System.Text.Json.Serialization;
using Kinoa.Data.FeaturesSettings;

/// <summary>
///     Sample Wheel of Fortune Feature Settings data model.
///     TODO: Replace with your actual Feature Settings data model.
/// </summary>
public class WheelOfFortuneSettings : FeatureSettingsData
{
    /// <summary>
    ///     Prize reward name.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Prize")]
    public string Prize { get; private set; }

    /// <summary>
    ///     Coins reward amount.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Coins")]
    public double Coins { get; private set; }

    /// <summary>
    ///     Bundle key referencing BundleResources in the response <see cref="FeatureSettingsResponse.BundleResources"/>.
    ///     The JsonPropertyName must match the Feature Schema field of type "Bundle Key".
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("FooBundleKey")]
    public string FooBundleKey { get; private set; }
}
