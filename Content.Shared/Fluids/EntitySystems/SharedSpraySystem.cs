using Content.Shared.Actions;
using Content.Shared.Fluids.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map;

namespace Content.Shared.Fluids.EntitySystems;

public abstract class SharedSpraySystem : EntitySystem
{
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

        if (!TryComp<SprayComponent>(equipSprayEnt, out var sprayComponent))
        {
            Log.Warning($"{ev.Action.Comp.AttachedEntity} tried to use the SprayLiquidEvent on {equipSprayEnt} but the SprayComponent did not exist.");
            return;
        }

        Spray((equipSprayEnt.Value, sprayComponent), ev.Performer);
    }

    private void OnGetVerb(Entity<EquipSprayComponent> entity, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (entity.Comp.VerbLocId == null || !args.CanAccess || !args.CanInteract)
            return;

        var sprayComponent = Comp<SprayComponent>(entity);
        var user = args.User;

        var verb = new EquipmentVerb
        {
            Act = () =>
            {
                Spray((entity, sprayComponent), user);
            },
            Text = Loc.GetString(entity.Comp.VerbLocId),
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Spray starting from the entity, to the given coordinates. If the user is supplied, will give them failure
    /// popups and will also push them in space.
    /// </summary>
    /// <param name="entity">Entity that is spraying.</param>
    /// <param name="mapcoord">The coordinates being aimed at.</param>
    /// <param name="user">The user that is using the spraying device.</param>
    public virtual void Spray(Entity<SprayComponent> entity, MapCoordinates mapcoord, EntityUid? user = null)
    {
        // do nothing!
    }

    /// <summary>
    /// Spray starting from the entity and facing the direction its pointing.
    /// </summary>
    /// <param name="entity">Entity that is spraying.</param>
    /// <param name="user">User that is using the spraying device.</param>
    public virtual void Spray(Entity<SprayComponent> entity, EntityUid? user = null)
    {
        // do nothing!
    }
}

public sealed partial class SprayLiquidEvent : InstantActionEvent;

