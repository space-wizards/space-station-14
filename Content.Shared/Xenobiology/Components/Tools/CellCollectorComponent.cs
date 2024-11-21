using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellCollectorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(4f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Usages = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? Damage;
}
