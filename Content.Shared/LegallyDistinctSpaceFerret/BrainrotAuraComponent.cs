namespace Content.Shared.LegallyDistinctSpaceFerret;

[RegisterComponent]
public sealed partial class BrainrotAuraComponent : Component
{
    [DataField]
    public float Range = 3.0f;

    [DataField]
    public float Time = 10.0f;
}
