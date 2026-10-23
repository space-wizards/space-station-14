using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Trigger effect for removing and *deleting* all items in container(s) on the target.
/// </summary>
/// <remarks>
/// Be very careful when setting <see cref="BaseXOnTriggerComponent.TargetUser"/> to true or all your organs might fall out.
/// In fact, never set it to true.
/// </remarks>
/// <seealso cref="EmptyContainersOnTriggerComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CleanContainersOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Names of containers to empty.
    /// If null, all containers will be emptied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string>? Container;
}
