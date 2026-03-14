using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

public abstract partial class SharedStunSystem
{
    public void InitializeAppearance()
    {
        SubscribeLocalEvent<StunVisualsComponent, MobStateChangedEvent>(OnStunMobStateChanged);
        SubscribeLocalEvent<StunVisualsComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    private bool GetStarsData(Entity<StunVisualsComponent, StunnedComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return false;

        return Blocker.CanConsciouslyPerformAction(entity);
    }

    private void OnStunMobStateChanged(Entity<StunVisualsComponent> entity, ref MobStateChangedEvent args)
    {
        Appearance.SetData(entity, StunVisuals.SeeingStars, GetStarsData(entity));
    }

    private void OnSleepStateChanged(Entity<StunVisualsComponent> entity, ref SleepStateChangedEvent args)
    {
        Appearance.SetData(entity, StunVisuals.SeeingStars, GetStarsData(entity));
    }

    public void TrySeeingStars(Entity<AppearanceComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        // Here so server can tell the client to do things
        // Don't dirty the component if we don't need to
        if (!Appearance.TryGetData<bool>(entity, StunVisuals.SeeingStars, out var stars, entity.Comp) && stars)
            return;

        if (!Blocker.CanConsciouslyPerformAction(entity))
            return;

        Appearance.SetData(entity, StunVisuals.SeeingStars, true);
        Dirty(entity);
    }

    [Serializable, NetSerializable, Flags]
    public enum StunVisuals
    {
        SeeingStars,
    }
}
