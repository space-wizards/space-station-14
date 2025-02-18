namespace Content.Shared._Impstation.Traits.Assorted;

/// <summary>
/// Used for the Hemophilia trait. BloodstreamSystem will check for this component and modify bleed reduction accordingly. 
/// </summary>
[RegisterComponent]
public sealed partial class HemophiliaComponent : Component
{
    [DataField("bleedReductionMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BleedReductionMultiplier = 0.33f;
}
