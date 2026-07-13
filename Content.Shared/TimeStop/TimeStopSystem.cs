using Robust.Shared.Physics.Events;

namespace Content.Shared.TimeStop;

public sealed partial class TimeStopSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<TimeStopZoneComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<TimeStopZoneComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<TimeStopZoneComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(Entity<TimeStopZoneComponent> ent, ref ComponentRemove args)
    {
        foreach (var entity in ent.Comp.FrozenEntities)
        {
            SetPaused(entity, false);
        }
    }

    private void OnEndCollide(Entity<TimeStopZoneComponent> ent, ref EndCollideEvent args)
    {
        if (IsTimePausable(args.OtherEntity))
        {
            SetPaused(args.OtherEntity, false);
            ent.Comp.FrozenEntities.Remove(args.OtherEntity);
        }
    }

    private void OnCollide(Entity<TimeStopZoneComponent> ent, ref StartCollideEvent args)
    {
        if (IsTimePausable(args.OtherEntity))
        {
            SetPaused(args.OtherEntity, true);
            ent.Comp.FrozenEntities.Add(args.OtherEntity);
        }
    }

    private bool IsTimePausable(EntityUid ent)
    {
        if (HasComp<TimeStopImmuneComponent>(ent))
            return false;

        if (HasComp<TimeStopZoneComponent>(ent))
            return false;

        return true;
    }
}
