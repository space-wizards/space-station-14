using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Charges.Components;

/// <summary>
/// Specifies the attached action has discrete charges, separate to a cooldown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimitedChargesComponent : Component
{
    [DataField, AutoNetworkedField]
    public int LastCharges;

    /// <summary>
    ///     The max charges this action has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxCharges = 1;

    /// <summary>
    /// Last time charges was changed. Used to derive current charges.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan LastUpdate;
}
