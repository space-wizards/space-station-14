using Content.Shared.Sound;
using Content.Shared.Sound.Components;
using Robust.Shared.Timing;

namespace Content.Server.Sound;

public sealed partial class EmitSoundSystem : SharedEmitSoundSystem
{
    private static readonly EntityTimerId SoundTimer = new("sound");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    private void OnTimer(Entity<SpamEmitSoundComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != SoundTimer || !ent.Comp.Enabled)
            return;

        if (ent.Comp.PopUp != null)
            Popup.PopupEntity(Loc.GetString(ent.Comp.PopUp), ent);
        TryEmitSound(ent, ent.Comp, predict: false);
        SpamEmitSoundReset(ent);
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpamEmitSoundComponent, MapInitEvent>(HandleSpamEmitSoundMapInit);
        SubscribeLocalEvent<SpamEmitSoundComponent, EntityTimerEvent>(OnTimer);
    }

    private void HandleSpamEmitSoundMapInit(Entity<SpamEmitSoundComponent> entity, ref MapInitEvent args)
    {
        SpamEmitSoundReset(entity);

        // Prewarm so multiple entities have more variation.
        entity.Comp.NextSound -= Random.Next(entity.Comp.MaxInterval);
        Dirty(entity);
        _timers.SetTimerAt(entity, SoundTimer, entity.Comp.NextSound);
    }

    private void SpamEmitSoundReset(Entity<SpamEmitSoundComponent> entity)
    {
        entity.Comp.NextSound = _timing.CurTime + ((entity.Comp.MinInterval < entity.Comp.MaxInterval)
            ? Random.Next(entity.Comp.MinInterval, entity.Comp.MaxInterval)
            : entity.Comp.MaxInterval);

        Dirty(entity);
        _timers.SetTimerAt(entity, SoundTimer, entity.Comp.NextSound);
    }

    public override void SetEnabled(Entity<SpamEmitSoundComponent?> entity, bool enabled)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.Enabled == enabled)
            return;

        entity.Comp.Enabled = enabled;

        if (enabled)
            SpamEmitSoundReset((entity, entity.Comp));
        else
            _timers.CancelTimer<SpamEmitSoundComponent>(entity, SoundTimer);
    }
}
