using Content.Client.Charges.Components;
using Content.Client.Charges.UI;
using Content.Client.Items;
using Content.Shared.Charges.Systems;

namespace Content.Client.Charges.EntitySystems;

/// <summary>
/// Wires up item status logic for <see cref="ChargeItemStatusComponent"/>.
/// </summary>
/// <seealso cref="ChargeStatusControl"/>
public sealed partial class ChargeItemStatusSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _chargesSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<ChargeItemStatusComponent>(entity =>
            new ChargeStatusControl(entity, EntityManager, _chargesSystem));
    }
}
