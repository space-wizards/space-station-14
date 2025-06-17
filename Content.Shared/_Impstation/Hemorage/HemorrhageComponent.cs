namespace Content.Shared._Impstation.Hemorrhage;

/// <summary>
/// Causes mobs to take increased bloodloss per blood stack. BloodstreamSystem will check for this component and modify bleed bloodloss accordingly.
/// </summary>
[RegisterComponent]
public sealed partial class HemorrhageComponent : Component
{
    [DataField("bleedIncreaseMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BleedIncreaseMultiplier = 1.4f;
}
