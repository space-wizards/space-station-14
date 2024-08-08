using Content.Server.Cargo.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    [DataField]
    public ProtoId<StackPrototype> CashType = "Credit";
}
