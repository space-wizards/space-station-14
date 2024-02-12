using Content.Server.Store.Systems;

namespace Content.Server.Store.Components;

/// <summary>
///     Keeps track of entities bought from stores for refunds, especially useful if entities get deleted before they can be refunded.
/// </summary>
[RegisterComponent, Access(typeof(StoreSystem))]
public sealed partial class StoreRefundComponent : Component
{
    [ViewVariables, DataField]
    public EntityUid? StoreEntity;
}
