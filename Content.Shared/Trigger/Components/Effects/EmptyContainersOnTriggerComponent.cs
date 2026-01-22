using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Trigger effect for removing all items in container(s) on the target.
/// </summary>
/// <remarks>
/// Be very careful when setting <see cref="BaseXOnTriggerComponent.TargetUser"/> to true or all your organs might fall out.
/// In fact, never set it to true.
/// </remarks>
/// <seealso cref="CleanContainersOnTriggerComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmptyContainersOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Names of containers to empty.
    /// If null, all containers will be emptied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string>? Container;
}
