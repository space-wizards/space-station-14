using Content.Server.Cargo.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CashType = "Credit";
}
