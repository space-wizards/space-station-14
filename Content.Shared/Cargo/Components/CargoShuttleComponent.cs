namespace Content.Shared.Cargo.Components;

/// <summary>
/// Present on cargo shuttles to provide metadata such as preventing spam calling.
/// </summary>
[RegisterComponent]
public sealed class CargoShuttleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("lastCall")]
    public TimeSpan LastCall;

    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public float Cooldown = 15f;
}
