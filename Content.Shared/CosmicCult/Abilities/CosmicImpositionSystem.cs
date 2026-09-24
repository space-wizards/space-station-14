using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult.Abilities;

public abstract partial class CosmicImpositionSystem : EntitySystem
{
    [Dependency] protected CosmicCultSystem Cult = default!;
    [Dependency] protected IGameTiming Timing = default!;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CosmicImpositionInvulnerableComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (Timing.CurTime >= comp.Expiry)
            {
                RemComp(ent, comp);
            }
        }
    }

    [SubscribeLocalEvent]
    protected virtual void OnCosmicImposition(Entity<CosmicActionImpositionComponent> ent, ref EventCosmicImposition args)
    {
        if (!Cult.CultActionQuery.TryComp(ent, out var action) || args.Handled)
            return;

        args.Handled = true;
        var duration = action.Empowered ? ent.Comp.DurationEmpowered : ent.Comp.DurationDefault;
        var slowDown = action.Empowered ? ent.Comp.MovePenaltyEmpowered : ent.Comp.MovePenaltyDefault;

        EnsureComp<CosmicImpositionInvulnerableComponent>(args.Performer, out var comp);
        comp.Expiry = Timing.CurTime + duration;

        _audio.PlayPredicted(action.Sfx, args.Performer, args.Performer, AudioParams.Default.WithVariation(0.05f));
        _movementMod.TryAddMovementSpeedModDuration(args.Performer, MovementModStatusSystem.ImpositionSlowdown, duration, slowDown);
    }

    [SubscribeLocalEvent]
    private void OnImpositionDamaged(Entity<CosmicImpositionInvulnerableComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }
}
