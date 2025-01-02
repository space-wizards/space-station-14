using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class BreakOnSlipSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BreakOnSlipComponent, InventoryRelayedEvent<SlippedEvent>>(OnSlip);
    }

    private void OnSlip(EntityUid uid, BreakOnSlipComponent component, InventoryRelayedEvent<SlippedEvent> args)
    {
        if (!_random.Prob(component.BreakChance) || _net.IsClient)
            return;
        EntityManager.DeleteEntity(uid);
        var replacement = Spawn(component.ReplacementPrototype, Transform(args.Args.Slipped).Coordinates);
        _inventorySystem.TryEquip(args.Args.Slipped, replacement, component.Slot);
        _popup.PopupEntity(component.Message, args.Args.Slipped, PopupType.Medium);
    }
}
