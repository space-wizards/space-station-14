namespace Content.Server.LegallyDistinctSpaceFerret;

[RegisterComponent]
public sealed partial class BrainrotComponent : Component
{
    [DataField]
    public float Duration = 10.0f;

    [DataField]
    public string BrainRotApplied = "brainrot-applied";

    [DataField]
    public string BrainRotLost = "brainrot-lost";

    [DataField]
    public string[] BrainRotReplacementStrings =
    [
        "brainrot-replacement-1",
        "brainrot-replacement-2",
        "brainrot-replacement-3",
        "brainrot-replacement-4",
        "brainrot-replacement-5",
        "brainrot-replacement-6"
    ];
}
