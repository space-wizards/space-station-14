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

        SubscribeLocalEvent<EquipSprayComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EquipSprayComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<SprayLiquidEvent>(SprayLiquid);
    }

    private void OnComponentInit(Entity<EquipSprayComponent> ent, ref ComponentInit args)
    {
        if (!HasComp<SprayComponent>(ent))
            Log.Warning($"{ent} did not have a SprayComponent (EquipSprayComponent entities should have a SprayComponent to work properly)");
    }

    private void SprayLiquid(SprayLiquidEvent ev)
    {
        var equipSprayEnt = ev.Action.Comp.Container;

        if (!TryComp<SprayComponent>(equipSprayEnt, out var sprayComponent) || !HasComp<EquipSprayComponent>(equipSprayEnt))
            return;

        _spray.Spray((equipSprayEnt.Value, sprayComponent), ev.Performer);
    }

    private void OnGetVerb(Entity<EquipSprayComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (entity.Comp.VerbLocId == null)
            return;

        if (!TryComp<SprayComponent>(entity, out var sprayComponent))
            return;

        var verb = new Verb
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
