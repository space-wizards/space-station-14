using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Removes the specified components when triggered.
/// If TargetUser is true they will be from the user instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveComponentsOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The list of components that will be removed.
    /// </summary>
    /// <summary>
    /// TODO: Using a ComponentRegistry for this is cursed because it stores all the datafields along with it,
    /// but ComponentNameSerializer will complain if you have components that are not in shared.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// If this component has been triggered at least once already.
    /// If this is true the components have been removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Triggered = false;

    /// <summary>
    /// If this effect can only be triggered once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TriggerOnce = false;
}
