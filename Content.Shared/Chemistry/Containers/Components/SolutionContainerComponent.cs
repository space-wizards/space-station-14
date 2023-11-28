using Content.Shared.Chemistry.Containers.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Containers.Components;

/// <summary>
/// A map of the solution entities contained within this entity.
/// </summary>
/// <remarks>
/// <para>Not for use in prototypes.</para>
/// <para>This maps strings to <see cref="EntityUid"/>s which cannot be used in prototypes <see cref="MapInitEvent"/>.</para>
/// <para>Specifying solutions in prototypes should be done through <see cref="Content.Server.Chemistry.Containers.Components.SolutionContainerManagerComponent"/>.</para>
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSolutionContainerSystem))]
public sealed partial class SolutionContainerComponent : Component
{
    /// <summary>
    /// The default amount of space that will be allocated for solutions in solution containers.
    /// Most solution containers will only contain 1-2 solutions.
    /// </summary>
    public const int DefaultCapacity = 2;

    /// <summary>
    /// A map of solution names to solution entities attached to the owning entity.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, ContainerSlot> Solutions = new(DefaultCapacity);
}


/// <summary>
/// Component state wrapper for <see cref="SolutionContainerComponent"/>
/// Exists because <see cref="EntityUid"/> are not <see cref="NetSerializableAttribute"/>.
/// </summary>
[Serializable, NetSerializable]
internal sealed partial class SolutionContainerState : ComponentState
{
    public List<string>? Solutions;

    public SolutionContainerState(List<string>? solutions)
    {
        Solutions = solutions;
    }
}
