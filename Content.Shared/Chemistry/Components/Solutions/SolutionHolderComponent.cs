using Content.Shared.Chemistry.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components.Solutions;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SolutionHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> SolutionIds = new(SharedSolutionSystem.SolutionAlloc);

    [DataField, AutoNetworkedField]
    public List<EntityUid> SolutionEntities = new(SharedSolutionSystem.SolutionAlloc);

    public List<Entity<SolutionComponent>> Solutions = new(SharedSolutionSystem.SolutionAlloc);
}
