using Content.Server._EE.Flash.Components;
using Content.Shared.Flash;
using Robust.Shared.Random;

namespace Content.Server._EE.Flash;

public sealed class EyeDamageOnFlashingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeDamageOnFlashingComponent, FlashAttemptEvent>(OnFlashAttempt);
    }

    private void OnFlashAttempt(Entity<EyeDamageOnFlashingComponent> ent, ref FlashAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.DurationMultiplier = ent.Comp.FlashDurationMultiplier;

        if (_random.Prob(ent.Comp.EyeDamageChance))
            args.EyeDamage = ent.Comp.EyeDamage;
    }
}
