using Content.Server.Explosion.EntitySystems;
using Content.Server.Sound.Components;
using Content.Server.UserInterface;
using Content.Shared.Sound;
using Robust.Shared.Random;

namespace Content.Server.Sound;

public sealed class EmitSoundSystem : SharedEmitSoundSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SpamEmitSoundComponent>();

        while (query.MoveNext(out var uid, out var soundSpammer))
        {
            if (!soundSpammer.Enabled)
                continue;

            soundSpammer.Accumulator += frameTime;
            if (soundSpammer.Accumulator < soundSpammer.RollInterval)
            {
                continue;
            }
            soundSpammer.Accumulator -= soundSpammer.RollInterval;

            if (Random.Prob(soundSpammer.PlayChance))
            {
                if (soundSpammer.PopUp != null)
                    Popup.PopupEntity(Loc.GetString(soundSpammer.PopUp), uid);
                TryEmitSound(uid, soundSpammer);
            }
        }

        var intervalQuery = EntityQueryEnumerator<EmitSoundIntervalComponent>();

        while (intervalQuery.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
            {
                // While disabled, keep pushing the next emit time out,
                // effectively pausing the timer.
                comp.NextEmitTime += TimeSpan.FromSeconds(frameTime);
                continue;
            }

            if (Timing.CurTime > comp.NextEmitTime)
            {
                TryEmitSound(uid, comp);
                SelectNextInterval(uid, comp);
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(HandleEmitSoundOnTrigger);
        SubscribeLocalEvent<EmitSoundOnUIOpenComponent, AfterActivatableUIOpenEvent>(HandleEmitSoundOnUIOpen);
        SubscribeLocalEvent<EmitSoundIntervalComponent, EntityUnpausedEvent>(OnIntervalUnpause);
        SubscribeLocalEvent<EmitSoundIntervalComponent, ComponentInit>(OnIntervalInit);
    }

    private void HandleEmitSoundOnUIOpen(EntityUid uid, EmitSoundOnUIOpenComponent component, AfterActivatableUIOpenEvent args)
    {
        TryEmitSound(uid, component, args.User);
    }

    private void HandleEmitSoundOnTrigger(EntityUid uid, EmitSoundOnTriggerComponent component, TriggerEvent args)
    {
        TryEmitSound(uid, component);
        args.Handled = true;
    }

    private void OnIntervalInit(EntityUid uid, EmitSoundIntervalComponent component, ComponentInit args)
    {
        SelectNextInterval(uid, component);
    }

    private void SelectNextInterval(EntityUid uid, EmitSoundIntervalComponent component)
    {
        component.NextEmitTime = Timing.CurTime + Random.Next(component.MinInterval, component.MaxInterval);
    }

    private void OnIntervalUnpause(EntityUid uid, EmitSoundIntervalComponent component, ref EntityUnpausedEvent args)
    {
        component.NextEmitTime += args.PausedTime;
    }
}
