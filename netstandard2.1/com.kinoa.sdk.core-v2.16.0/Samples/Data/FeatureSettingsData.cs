using System.Text.Json.Serialization;

/// <summary>
///     The polymorphic Feature Settings base data model.
///     Concrete Feature Settings classes should inherit from this class.
///     TODO: Replace sample derived types with your actual Feature Settings data models.
///     TODO: The JsonDerivedType typeDiscriminator must match the Feature Settings key on the Kinoa Dashboard.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(DailyBonusSettings), "DailyBonus")]
[JsonDerivedType(typeof(WheelOfFortuneSettings), "WheelOfFortune")]
public class FeatureSettingsData
{
}
