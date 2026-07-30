using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;

namespace Content.Shared.Ninja.Systems;

public sealed partial class EnergyKatanaSystem : EntitySystem
{
    [Dependency] private SharedSpaceNinjaSystem _ninja = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<EnergyKatanaComponent> ent, ref GotEquippedEvent args)
    {
        _ninja.BindKatana(args.EquipTarget, ent);
    }
}
