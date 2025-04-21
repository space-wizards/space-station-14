using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Makes a sellable object portion out its value to a specified department rather than the station default
/// </summary>
[RegisterComponent]
public sealed partial class OverrideSellComponent : Component
{
    /// <summary>
    /// The account that will receive the primary funds from this being sold.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CargoAccountPrototype> OverrideAccount;

    /// <summary>
    /// The cut that the OverrideAccount will get from the price. The rest is given to the primary station account.
    /// </summary>
    [DataField]
    public float OverrideCut = 0.75f;
}
