using Content.Shared._Impstation.Fluids.Components;
using Robust.Shared.GameStates;

namespace Content.Server._Impstation.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PropulsionComponent : SharedPropulsionComponent
{
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<Entity<PropulsedByComponent>> AffectingEntities = new();
}
