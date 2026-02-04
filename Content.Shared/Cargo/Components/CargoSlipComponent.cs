using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Holds data for an order slip required for insertion into a console
/// </summary>
[RegisterComponent]
public sealed partial class CargoSlipComponent : Component
{
    /// <summary>
    /// The requested product
    /// </summary>
    [DataField]
    public ProtoId<CargoProductPrototype> Product;

    /// <summary>
    /// The provided value for the requester form field
    /// </summary>
    [DataField]
    public string Requester;

    /// <summary>
    /// The provided value for the reason form field
    /// </summary>
    [DataField]
    public string Reason;

    /// <summary>
    /// How many of the product to order
    /// </summary>
    [DataField]
    public int OrderQuantity;

    /// <summary>
    /// How many of the product to order
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> Account;
}
