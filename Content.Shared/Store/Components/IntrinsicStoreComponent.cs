using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Store.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IntrinsicStoreComponent : Component
{
    [DataField]
    public EntProtoId ShopActionId = "IntrinisicShopOpenShop";

    [DataField, AutoNetworkedField]
    public EntityUid? ShopAction;
}
