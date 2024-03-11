using Content.Shared.Nutrition;
using Content.Shared.Sound.Components;
using Content.Shared.Tag;

namespace Content.Shared.Sound.EntitySystems;

public sealed partial class SilencedSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SilencedComponent, EmitSoundAttemptEvent>(OnEmitSoundAttempt);
        SubscribeLocalEvent<SilencedComponent, UseSoundEmitterAttemptEvent>(OnUseSoundEmitterAttempt);
        SubscribeLocalEvent<SilencedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SilencedComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SilencedComponent, AttemptMakeEatingSoundEvent>(OnAttemptMakeEatingSound);
        SubscribeLocalEvent<SilencedComponent, AttemptMakeDrinkingSoundEvent>(OnAttemptMakeDrinkingSound);
    }

    private void OnEmitSoundAttempt(Entity<SilencedComponent> entity, ref EmitSoundAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnUseSoundEmitterAttempt(Entity<SilencedComponent> entity, ref UseSoundEmitterAttemptEvent args)
    {
        if (!entity.Comp.AllowEmitterUse)
            args.Cancel();
    }

    private void OnComponentStartup(Entity<SilencedComponent> entity, ref ComponentStartup args)
    {
        if (!entity.Comp.AllowFootsteps)
            entity.Comp.HadFootsteps = _tag.RemoveTag(entity, "FootstepSound");
    }

    private void OnComponentShutdown(Entity<SilencedComponent> entity, ref ComponentShutdown args)
    {
        if (Terminating(entity))
            return;

        if (!entity.Comp.AllowFootsteps && entity.Comp.HadFootsteps)
            _tag.AddTag(entity, "FootstepSound");
    }

    private void OnAttemptMakeEatingSound(Entity<SilencedComponent> entity, ref AttemptMakeEatingSoundEvent args)
    {
        if (!entity.Comp.AllowEatingSounds)
            args.Cancel();
    }

    private void OnAttemptMakeDrinkingSound(Entity<SilencedComponent> entity, ref AttemptMakeDrinkingSoundEvent args)
    {
        if (!entity.Comp.AllowDrinkingSounds)
            args.Cancel();
    }
}
