using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// This component brings up a radial menu with different triggers that activate depending on what you select.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnRadialMenuComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<TriggerRadialMenuEntry> RadialMenuEntries = [];
}

/// <summary>
/// Information about one specific radial menu button for the <see cref="TriggerOnRadialMenuComponent"/>
/// Contains all information about one specific button!
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class TriggerRadialMenuEntry
{
    /// <summary>
    /// Name of the tooltip that will be displayed
    /// </summary>
    [DataField]
    public LocId? Name;

    /// <summary>
    /// Will use this entity as the icon, this has priority over <see cref="SpriteSpecifierIcon"/>
    /// </summary>
    [DataField]
    public EntProtoId? ProtoIdIcon;

    /// <summary>
    /// A sprite specifier to use for the radial menu icon
    /// </summary>
    [DataField]
    public SpriteSpecifier? SpriteSpecifierIcon;

    /// <summary>
    /// The key that will be triggered when the radial menu option is selected.
    /// </summary>
    [DataField(required: true)]
    public string Key = "";
}

/// <summary>
/// Message sent from the client w/ the index of the radial menu entry they selected
/// </summary>
[Serializable, NetSerializable]
public sealed class TriggerOnRadialMenuSelectMessage(int index) : BoundUserInterfaceMessage
{
    public int Index = index;
}

[Serializable, NetSerializable]
public enum TriggerOnRadialMenuUiKey : byte
{
    Key
}
