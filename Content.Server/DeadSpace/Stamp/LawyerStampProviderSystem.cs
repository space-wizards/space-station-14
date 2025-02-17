// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.GameTicking;
using Content.Server.Inventory;
using Content.Shared.Paper;

namespace Content.Server.DeadSpace.Stamp;

public sealed class LawyerStampProviderSystem : EntitySystem
{
    [Dependency] private readonly ServerInventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LawyerStampProviderComponent, PlayerSpawnCompleteEvent>(OnSpawn);
        SubscribeLocalEvent<LawyerStampProviderComponent, MapInitEvent>(OnTakeGhostRole); // TODO: check why there two events
    }

    private void OnSpawn(EntityUid uid, LawyerStampProviderComponent comp, PlayerSpawnCompleteEvent args)
    {
        var stamp = Spawn(comp.StampPrototype, Transform(args.Mob).Coordinates);

        if (!TryComp<StampComponent>(stamp, out var stampComp))
        {
            return;
        }

        stampComp.StampedName = Loc.GetString("stamp-component-stamped-name-lawyer") + " " + args.Profile.Name; // StampedName gets Loc.GetString() later, soo thats needs to be fixed

        _inventory.TryEquip(uid, stamp, comp.Slot, true, true); // Slot = "pocket1", also not a greate sollution, in future better to store in backpack or rework
    }

    private void OnTakeGhostRole(EntityUid uid, LawyerStampProviderComponent comp, MapInitEvent args)
    {
        var stamp = Spawn(comp.StampPrototype, Transform(uid).Coordinates);

        if (!TryComp<StampComponent>(stamp, out var stampComp))
        {
            return;
        }

        stampComp.StampedName = Loc.GetString("stamp-component-stamped-name-lawyer") + " " + MetaData(uid).EntityName; // StampedName gets Loc.GetString() later, soo thats needs to be fixed

        _inventory.TryEquip(uid, stamp, comp.Slot, true, true); // Slot = "pocket1", also not a greate sollution, in future better to store in backpack or rework
    }
}
