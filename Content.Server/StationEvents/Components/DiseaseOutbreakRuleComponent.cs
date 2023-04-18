namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class DiseaseOutbreakRuleComponent : Component
{
    /// <summary>
    /// Disease prototypes I decided were not too deadly for a random event
    /// </summary>
    /// <remarks>
    /// Fire name
    /// </remarks>
    [DataField("notTooSeriousDiseases")]
    public readonly IReadOnlyList<string> NotTooSeriousDiseases = new[]
    {
        "SpaceCold",
        "VanAusdallsRobovirus",
        "VentCough",
        "AMIV",
        "SpaceFlu",
        "BirdFlew",
        "TongueTwister"
    };
}
