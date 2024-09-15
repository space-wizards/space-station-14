using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;

namespace Content.Shared.GreyStation.Clothing;

/// <summary>
/// Handles wielding attempts for <see cref="ClothingPreventsWieldingComponent"/>.
/// </summary>
public sealed partial class ClothingPreventsWieldingSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingPreventsWieldingComponent, BeforeWieldEvent>(OnBeforeWield);
    }

    private void OnBeforeWield(Entity<ClothingPreventsWieldingComponent> ent, ref BeforeWieldEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.Wielder;
        if (!TryComp<InventoryComponent>(user, out var inventory))
            return;

        var (uid, comp) = ent;
        foreach (var (id, whitelist) in comp.Slots)
        {
            if (!_inventory.TryGetSlotContainer(user, id, out var slot, out _, inventory) || slot.ContainedEntity is not {} item)
                continue;

            if (_whitelist.IsWhitelistPass(whitelist, item))
            {
                args.Cancel();
                return;
            }
        }
    }
}
