namespace Content.Shared.Lube;

[RegisterComponent]
public sealed partial class LubedComponent : Component
{
    /// <summary>
    /// Reverts name to before prefix event (essentially removes prefix).
    /// </summary>
    [DataField("beforeLubedEntityName")]
    public string BeforeLubedEntityName = string.Empty;

    [DataField("slipsLeft"), ViewVariables(VVAccess.ReadWrite)]
    public int SlipsLeft;

    [DataField("slipStrength"), ViewVariables(VVAccess.ReadWrite)]
    public int SlipStrength;
}
