using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SelectableComponentAdder;

/// <summary>
/// Brings up a verb menu that allows players to select components that will get added to the item with this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SelectableComponentAdderComponent : Component
{
    /// <summary>
    /// List of verb -> components to add for that verb when selected basically!
    /// </summary>
    [DataField(required: true)]
    public List<ComponentAdderEntry> Entries = new();

    /// <summary>
    /// The amount of times players can make a selection and add a component. If null, there is no limit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Selections;

    /// <summary>
    /// The verb category name that will be used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId VerbCategoryName = "selectable-component-adder-category-name";
}

[DataDefinition]
public sealed partial class ComponentAdderEntry
{
    /// <summary>
    /// Name of the verb that will add the components in <see cref="ComponentsToAdd"/>.
    /// </summary>
    [DataField(required: true)]
    public LocId VerbName;

    /// <summary>
    /// Popup to show when this option is selected.
    /// </summary>
    [DataField(required: true)]
    public LocId? Popup;

    /// <summary>
    /// List of all the components that will get added when the verb is selected.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry? ComponentsToAdd;

    /// <summary>
    /// The type of behavior that occurs when the component(s) already exist on the entity.
    /// </summary>
    [DataField]
    public ComponentExistsSetting ComponentExistsBehavior = ComponentExistsSetting.Skip;

    /// <summary>
    /// The priorty of the verb in the list
    /// </summary>
    [DataField]
    public int Priority;
}

[Serializable, NetSerializable]
public enum ComponentExistsSetting : byte
{
    // If one of the components exist, skip adding it and continue adding the rest.
    // If all components already exist, disable the verb.
    Skip = 0,
    // If a component already exists, replace it with the new one.
    // The verb is always enabled.
    Replace = 1,
    // Disable the verb if any one of the components already exists.
    Block = 2,
}
