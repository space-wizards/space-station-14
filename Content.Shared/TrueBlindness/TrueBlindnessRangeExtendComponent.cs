namespace Content.Shared.TrueBlindness;

[RegisterComponent]
public sealed partial class TrueBlindnessRangeExtendComponent : Component
{
    /// <summary>
    ///     How far you can see when holding this.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 1.5f;
}
