using Robust.Shared.GameStates;

namespace Content.Shared.Item.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ExtendedInventoryComponent : Component
{
    /// <summary>
    ///     The EntityUid of the container that is extending its inventory to the owner of this component.
    /// </summary>
    [AutoNetworkedField]
    public List<EntityUid> ConnectedContainer = [];
}
