namespace Content.Server.Flash.Components;

/// <summary>
///     Makes the entity immune to being flashed.
///     When given to clothes in the "head", "eyes" or "mask" slot it protects the wearer.
/// </summary>
[RegisterComponent, Access(typeof(FlashSystem))]
public sealed partial class FlashImmunityComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enabled")]
    public bool Enabled { get; set; } = true;
}
