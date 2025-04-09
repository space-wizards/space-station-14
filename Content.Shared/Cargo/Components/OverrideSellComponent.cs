using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class OverrideSellComponent : Component
{
    [DataField]
    public ProtoId<CargoAccountPrototype> OverrideAccount;
}
