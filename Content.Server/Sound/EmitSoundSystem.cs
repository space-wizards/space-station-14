using Content.Server.Explosion.EntitySystems;
using Content.Server.Sound.Components;
using Content.Shared.UserInterface;
using Content.Shared.Sound;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Sound;

public sealed class EmitSoundSystem : SharedEmitSoundSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SpamEmitSoundComponent>();

        while (query.MoveNext(out var uid, out var soundSpammer))
        {
            if (!soundSpammer.Enabled)
                continue;

            if (_timing.CurTime >= soundSpammer.NextSound)
            {
                if (soundSpammer.PopUp != null)
                    Popup.PopupEntity(Loc.GetString(soundSpammer.PopUp), uid);
                TryEmitSound(uid, soundSpammer, predict: false);

                SpamEmitSoundReset((uid, soundSpammer));
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(HandleEmitSoundOnTrigger);
        SubscribeLocalEvent<EmitSoundOnUIOpenComponent, AfterActivatableUIOpenEvent>(HandleEmitSoundOnUIOpen);

        SubscribeLocalEvent<SpamEmitSoundComponent, MapInitEvent>(HandleSpamEmitSoundMapInit);
        SubscribeLocalEvent<SpamEmitSoundComponent, EntityUnpausedEvent>(HandleSpamEmitSoundUnpause);
    }

    private void HandleEmitSoundOnUIOpen(EntityUid uid, EmitSoundOnUIOpenComponent component, AfterActivatableUIOpenEvent args)
    {
        TryEmitSound(uid, component, args.User, false);
    }

    private void HandleEmitSoundOnTrigger(EntityUid uid, EmitSoundOnTriggerComponent component, TriggerEvent args)
    {
        TryEmitSound(uid, component, args.User, false);
        args.Handled = true;
    }

    private void HandleSpamEmitSoundMapInit(Entity<SpamEmitSoundComponent> entity, ref MapInitEvent args)
    {
        SpamEmitSoundReset(entity);

        // Prewarm so multiple entities have more variation.
        entity.Comp.NextSound -= Random.Next(entity.Comp.MaxInterval);
    }

    private void HandleSpamEmitSoundUnpause(Entity<SpamEmitSoundComponent> entity, ref EntityUnpausedEvent args)
    {
        entity.Comp.NextSound += args.PausedTime;
    }

    private void SpamEmitSoundReset(Entity<SpamEmitSoundComponent> entity)
    {
        entity.Comp.NextSound = _timing.CurTime + ((entity.Comp.MinInterval < entity.Comp.MaxInterval)
            ? Random.Next(entity.Comp.MinInterval, entity.Comp.MaxInterval)
            : entity.Comp.MaxInterval);
    }

    public void SetEnabled(Entity<SpamEmitSoundComponent?> entity, bool enabled)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.Enabled = enabled;

        if (!enabled)
            entity.Comp.DisabledTime = _timing.CurTime;
        else
        {
            var elapsed = _timing.CurTime - entity.Comp.DisabledTime;
            entity.Comp.NextSound += elapsed;
        }
    }
}
