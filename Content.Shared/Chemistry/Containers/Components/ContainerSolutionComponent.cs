using Content.Shared.Chemistry.Containers.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Containers.Components;

/// <summary>
/// 
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSolutionContainerSystem))]
public sealed partial class ContainerSolutionComponent : Component
{
    [DataField]
    public EntityUid Container;

    [DataField]
    public string Name = default!;
}


[Serializable, NetSerializable]
public sealed partial class ContainerSolutionState : ComponentState
{
    public NetEntity Container;
    public string Name;

    public ContainerSolutionState(NetEntity container, string name)
    {
        Container = container;
        Name = name;
    }
}
