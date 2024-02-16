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
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype>? ActivatedSize = null;

    /// <summary>
    ///     Item's size when deactivated. If none is mentioned, it uses the item's default size instead.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype>? DeactivatedSize = null;
}
