using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicImpositionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CosmicImpositionInvulnerableComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime >= comp.Expiry)
            {
                RemComp(ent, comp);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnCosmicImposition(Entity<CosmicActionImpositionComponent> ent, ref EventCosmicImposition args)
    {
        if (!TryComp<CosmicCultActionComponent>(ent, out var action))
            return;

        args.Handled = true;
        var duration = action.Empowered ? ent.Comp.DurationEmpowered : ent.Comp.DurationDefault;
        var overlayEffect = SpawnAttachedTo(ent.Comp.ImpositionOverlay, Transform(ent).Coordinates);

        SpawnAttachedTo(action.Vfx, Transform(ent).Coordinates);
        EnsureComp<CosmicImpositionInvulnerableComponent>(args.Performer, out var comp);
        EnsureComp<CosmicImpositionFadeComponent>(overlayEffect, out var fade);
        EnsureComp<TimedDespawnComponent>(overlayEffect, out var despawn);

        despawn.Lifetime = (float) duration.TotalSeconds;
        fade.Duration = (float) duration.TotalSeconds;
        comp.Expiry = _timing.CurTime + duration;

        Dirty(overlayEffect, fade);
        _audio.PlayPvs(action.Sfx, ent, AudioParams.Default.WithVariation(0.05f));
    }

    [SubscribeLocalEvent]
    private void OnImpositionDamaged(Entity<CosmicImpositionInvulnerableComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }
}
