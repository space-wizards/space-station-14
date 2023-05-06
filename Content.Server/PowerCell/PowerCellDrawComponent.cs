using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.PowerCell;

/// <summary>
/// Indicates that the entity's ActivatableUI requires power or else it closes.
/// </summary>
[RegisterComponent, Access(typeof(PowerCellSystem))]
public sealed class PowerCellDrawComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("enabled")]
    public bool Enabled;

    /// <summary>
    /// How much the entity draws while the UI is open.
    /// Set to 0 if you just wish to check for power upon opening the UI.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("drawRate")]
    public float DrawRate = 1f;

    /// <summary>
    /// How much power is used whenever the entity is "used".
    /// This is used to ensure the UI won't open again without a minimum use power.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("useRate")]
    public float UseRate;

    /// <summary>
    /// When the next automatic power draw will occur
    /// </summary>
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdateTime;
}
