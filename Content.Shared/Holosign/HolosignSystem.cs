using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.PowerCell;
using Content.Shared.Storage;
using Robust.Shared.Network;

namespace Content.Shared.Holosign;

public sealed class HolosignSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolosignProjectorComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        SubscribeLocalEvent<HolosignProjectorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HolosignProjectorComponent> ent, ref ExaminedEvent args)
    {
        // TODO: This should probably be using an itemstatus
        // TODO: I'm too lazy to do this rn but it's literally copy-paste from emag.
        var charges = _powerCell.GetRemainingUses(ent.Owner, ent.Comp.ChargeUse);
        var maxCharges = _powerCell.GetMaxUses(ent.Owner, ent.Comp.ChargeUse);

        using (args.PushGroup(nameof(HolosignProjectorComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

            if (charges > 0 && charges == maxCharges)
            {
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
            }
        }
    }

    private void OnBeforeInteract(Entity<HolosignProjectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach // prevent placing out of range
            || HasComp<StorageComponent>(args.Target) // if it's a storage component like a bag, we ignore usage so it can be stored
            || !_powerCell.TryUseCharge(ent.Owner, ent.Comp.ChargeUse, user: args.User, predicted: true) // if no battery or no charge, doesn't work
            )
            return;

        // overlapping of the same holo on one tile remains allowed to allow holofan refreshes
        if (ent.Comp.PredictedSpawn || _net.IsServer)
            PredictedSpawnAtPosition(ent.Comp.SignProto, args.ClickLocation);

        args.Handled = true;
    }
}
