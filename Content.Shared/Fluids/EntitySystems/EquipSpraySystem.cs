using System.Diagnostics;
using Content.Shared.Actions;
using Content.Shared.Fluids.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Fluids.EntitySystems;

public sealed class EquipSpraySystem : EntitySystem
{
    [Dependency] private readonly SharedSpaySystem _spray = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipSprayComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerb);
        SubscribeLocalEvent<SprayLiquidEvent>(SprayLiquid);
    }

    private void SprayLiquid(SprayLiquidEvent ev)
    {
        var equipSprayEnt = ev.Action.Comp.Container;

        if (equipSprayEnt == null)
        {
            Log.Warning($"{ev.Action.Comp.AttachedEntity} tried to use the SprayLiquidEvent but the entity was null.");
            return;
        }

        if (!HasComp<EquipSprayComponent>(equipSprayEnt))
        {
            Log.Warning($"{ev.Action.Comp.AttachedEntity} tried to  use the SprayLiquidEvent on {equipSprayEnt} but the EquipSprayComponent did not exist.");
            return;
        }

        if (!TryComp<SprayComponent>(equipSprayEnt, out var sprayComponent))
        {
            Log.Warning($"{ev.Action.Comp.AttachedEntity} tried to  use the SprayLiquidEvent on {equipSprayEnt} but the SprayComponent did not exist.");
            return;
        }

        _spray.Spray((equipSprayEnt.Value, sprayComponent), ev.Performer);
    }

    private void OnGetVerb(Entity<EquipSprayComponent> entity, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (entity.Comp.VerbLocId == null)
            return;

        var sprayComponent = Comp<SprayComponent>(entity);

        var verb = new EquipmentVerb
        {
            Act = () =>
            {
                _spray.Spray((entity, sprayComponent));
            },
            Text = Loc.GetString(entity.Comp.VerbLocId),
        };
        args.Verbs.Add(verb);
    }
}

public sealed partial class SprayLiquidEvent : InstantActionEvent;
