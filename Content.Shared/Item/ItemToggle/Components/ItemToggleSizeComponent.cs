using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Item.ItemToggle.Components;

/// <summary>
/// Handles the changes to the item size when toggled.
/// </summary>
/// <remarks>
/// You can change the size when activated or not. By default the sizes are copied from the item.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleSizeComponent : Component
{
    /// <summary>
    ///     Item's size when activated
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype>? ActivatedSize = null;

    /// <summary>
    ///     Item's shape when activated
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2i>? ActivatedShape = null;

    /// <summary>
    ///     Item's size when deactivated. If none is mentioned, it uses the item's default size instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype>? DeactivatedSize = null;

    /// <summary>
    ///     Item's shape when deactivated. If none is mentioned, it uses the item's default shape instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2i>? DeactivatedShape = null;
}
