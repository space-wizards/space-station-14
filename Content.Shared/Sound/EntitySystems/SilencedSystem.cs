using Content.Shared.Bed.Sleep;
using Content.Shared.Nutrition;
using Content.Shared.Sound.Components;

namespace Content.Shared.Sound.EntitySystems;

/// <summary>
/// Blocks attempts by the entity to make sound in various ways, as
/// configured by <see cref="SilencedComponent"/>.
/// </summary>
public sealed partial class SilencedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SilencedComponent, AttemptStartSnoringEvent>(OnAttemptSnore);
        SubscribeLocalEvent<SilencedComponent, AttemptMakeEatingSoundEvent>(OnAttemptMakeEatingSound);
        SubscribeLocalEvent<SilencedComponent, AttemptMakeDrinkingSoundEvent>(OnAttemptMakeDrinkingSound);
    }

    private void OnAttemptSnore(Entity<SilencedComponent> entity, ref AttemptStartSnoringEvent args)
    {
        if (!entity.Comp.AllowSnoring)
            args.Cancelled = true;
    }

    private void OnAttemptMakeEatingSound(Entity<SilencedComponent> entity, ref AttemptMakeEatingSoundEvent args)
    {
        if (!entity.Comp.AllowEatingSounds)
            args.Cancelled = true;
    }

    private void OnAttemptMakeDrinkingSound(Entity<SilencedComponent> entity, ref AttemptMakeDrinkingSoundEvent args)
    {
        if (!entity.Comp.AllowDrinkingSounds)
            args.Cancelled = true;
    }
}
