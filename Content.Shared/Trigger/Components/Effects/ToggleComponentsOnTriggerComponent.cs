using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Adds or removes the specified components when triggered.
/// If TargetUser is true they will be added to or removed from the user instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleComponentsOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The list of components that will be added/removed.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Are the components currently added?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ComponentsAdded;

    /// <summary>
    /// Should components that already exist on the entity be overwritten?
    /// (They will still be removed when toggling again).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveExisting = false;
}
