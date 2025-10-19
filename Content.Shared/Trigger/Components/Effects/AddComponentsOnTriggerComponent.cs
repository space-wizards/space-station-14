using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Adds the specified components when triggered.
/// If TargetUser is true they will be added to the user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddComponentsOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The list of components that will be added.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// If this component has been triggered at least once already.
    /// If this is true the components have been added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Triggered = false;

    /// <summary>
    /// If this effect can only be triggered once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TriggerOnce = false;

    /// <summary>
    /// Should components that already exist on the entity be overwritten?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveExisting = false;
}
