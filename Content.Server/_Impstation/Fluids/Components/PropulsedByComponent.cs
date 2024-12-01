using Content.Server._Impstation.Fluids.Components;
using Robust.Shared.GameStates;

namespace Content.Server._Impstation.Fluids;

[RegisterComponent, NetworkedComponent]
public sealed partial class PropulsedByComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public HashSet<Entity<PropulsionComponent>> Sources = new();
}
