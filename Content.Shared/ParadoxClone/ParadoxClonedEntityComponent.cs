using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.ParadoxClone;

/// <summary>
/// Used so that a paradox clone can be hidden inside of an entity
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ParadoxClonedEntityComponent : Component
{
    /// <summary>
    ///     A container used to store a paradox clone
    /// </summary>
    [DataField]
    public ContainerSlot ParadoxCloneBox;

}
