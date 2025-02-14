using Content.Shared.Charges.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Charges.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedChargesSystem))]
[AutoGenerateComponentState]
public sealed partial class LimitedChargesComponent : Component
{
    /// <summary>
    /// The maximum number of charges
    /// </summary>
    [DataField("maxCharges"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 MaxCharges = 3;

    /// <summary>
    /// The current number of charges
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 Charges = 3;
}
