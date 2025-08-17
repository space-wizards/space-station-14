using Content.Shared.Containers.ItemSlots;
using Content.Shared.Lock;
using Content.Shared.TurretController;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.StationAi;

public sealed partial class StationAiFixerConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    private readonly string _intellicardContainer = "intellicard_holder";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiFixerConsoleComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<StationAiFixerConsoleComponent, StationAiFixerConsoleMessage>(OnMessage);
    }

    private void OnInserted(Entity<StationAiFixerConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_userInterface.TryGetOpenUi(ent.Owner, StationAiFixerConsoleUiKey.Key, out var bui))
            bui.Update<StationAiFixerConsoleBoundUserInterfaceState>();
    }

    private void OnRemoved(Entity<StationAiFixerConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_userInterface.TryGetOpenUi(ent.Owner, StationAiFixerConsoleUiKey.Key, out var bui))
            bui.Update<StationAiFixerConsoleBoundUserInterfaceState>();
    }

    private void OnMessage(Entity<StationAiFixerConsoleComponent> ent, ref StationAiFixerConsoleMessage args)
    {
        if (TryComp<LockComponent>(ent, out var lockable) && lockable.Locked)
            return;

        switch(args.Action)
        {
            case StationAiFixerConsoleAction.Eject:
                EjectIntellicard(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Repair:
                RepairStationAi(ent, args.Actor);
                break;
            case StationAiFixerConsoleAction.Purge:
                PurgeStationAi(ent, args.Actor);
                break;
        }
    }

    private void EjectIntellicard(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (!_itemSlots.TryGetSlot(ent, _intellicardContainer, out var intellicardSlot, slots))
            return;

        _itemSlots.TryEjectToHands(ent, intellicardSlot, user, true);
    }

    private void RepairStationAi(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {

    }

    private void PurgeStationAi(Entity<StationAiFixerConsoleComponent> ent, EntityUid user)
    {

    }
}
