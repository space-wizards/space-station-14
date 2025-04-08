using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Item.ItemToggle.Components;

/// <summary>
/// Makes it so only entities that have the specified component can toggle an item
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ItemToggleUserRestrictSystem)), AutoGenerateComponentState]
public sealed partial class ItemToggleUserRestrictComponent : Component
{
    /// <summary>
    /// the component required to toggle the item
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// the message a person unable to toggle it on will get if they try. null for no message
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? RestrictMessage;

    /// <summary>
    /// can only restricted people toggle it on?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OpenRestrict = true;

    /// <summary>
    /// can only restricted people toggle it off?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CloseRestrict = true;
}
