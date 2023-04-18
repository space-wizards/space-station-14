namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class VentClogRuleComponent : Component
{
    [DataField("safeishVentChemicals")]
    public readonly IReadOnlyList<string> SafeishVentChemicals = new[]
    {
        "Water", "Blood", "Slime", "SpaceDrugs", "SpaceCleaner", "Nutriment", "Sugar", "SpaceLube", "Ephedrine", "Ale", "Beer"
    };

}
