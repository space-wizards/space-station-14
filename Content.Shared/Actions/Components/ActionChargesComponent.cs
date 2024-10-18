using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Actions;

/// <summary>
/// Specifies the attached action has discrete charges, separate to a cooldown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionChargesComponent : Component
{
    [DataField] public int LastCharges;

    /// <summary>
    ///     The max charges this action has.
    /// </summary>
    [DataField] public int MaxCharges = 1;

    /// <summary>
    /// Last time charges was changed. Used to derive current charges.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan LastUpdate;

    /// <summary>
    ///     If enabled, charges will regenerate after a <see cref="Cooldown"/> is complete
    /// </summary>
    [DataField] public bool RenewCharges;
}
