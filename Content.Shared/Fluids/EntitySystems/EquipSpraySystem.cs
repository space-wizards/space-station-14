using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Fluids.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map;

namespace Content.Shared.Fluids.EntitySystems;

public sealed class EquipSpraySystem : EntitySystem
{
    [Dependency] private readonly SharedSpaySystem _spray = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipSprayComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<SprayLiquidEvent>(SprayLiquid);
    }

    private void SprayLiquid(SprayLiquidEvent ev)
    {
        var equipSprayEnt = ev.Action.Comp.Container;

        if (!TryComp<SprayComponent>(equipSprayEnt, out var sprayComponent) || !TryComp<EquipSprayComponent>(equipSprayEnt, out var equipSpray))
            return;

        _spray.Spray((equipSprayEnt.Value, sprayComponent), ev.Performer, GetSprayDirection((equipSprayEnt.Value, equipSpray)));
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
                // TODO: Make a spray override that comes from the entity not the user
                _spray.Spray((entity, sprayComponent), entity, GetSprayDirection(entity));
            },
            Text = Loc.GetString(entity.Comp.VerbLocId),
        };
        args.Verbs.Add(verb);
    }

    private MapCoordinates GetSprayDirection(Entity<EquipSprayComponent> entity)
    {
        var xform = Transform(entity);
        var throwing = xform.LocalRotation.ToWorldVec();
        var direction = xform.Coordinates.Offset(throwing);

        return _transform.ToMapCoordinates(direction);
    }
}

public sealed partial class SprayLiquidEvent : InstantActionEvent;
