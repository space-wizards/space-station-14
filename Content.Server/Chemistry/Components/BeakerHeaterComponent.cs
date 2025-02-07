namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class BeakerHeaterComponent : Component
{
    /// <summary>
    /// How much heat is added per second to the solution, taking upgrades into account.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BeakerHeatPerSecond;
}
