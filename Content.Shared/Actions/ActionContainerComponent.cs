using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions;

/// <summary>
/// This component indicates that this entity contains actions inside of some container.
/// </summary>
[NetworkedComponent, RegisterComponent]
[Access(typeof(ActionContainerSystem), typeof(SharedActionsSystem))]
public sealed partial class ActionsContainerComponent : Component
{
    public const string ContainerId = "actions";

    [ViewVariables]
    public Container Container = default!;
}
