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
            if (soundSpammer.Accumulator < soundSpammer.RollInterval + soundSpammer.ExtraInterval)
            {
                continue;
            }
            soundSpammer.Accumulator -= soundSpammer.RollInterval + soundSpammer.ExtraInterval;
            soundSpammer.ExtraInterval = Random.NextFloat(soundSpammer.MaxExtraInterval);

            if (Random.Prob(soundSpammer.PlayChance))
            {
                if (soundSpammer.PopUp != null)
                    Popup.PopupEntity(Loc.GetString(soundSpammer.PopUp), uid);
                TryEmitSound(uid, soundSpammer, predict: false);
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(HandleEmitSoundOnTrigger);
        SubscribeLocalEvent<EmitSoundOnUIOpenComponent, AfterActivatableUIOpenEvent>(HandleEmitSoundOnUIOpen);
        SubscribeLocalEvent<SpamEmitSoundComponent, MapInitEvent>(HandleSpamEmitSoundMapInit);
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

    private void HandleSpamEmitSoundMapInit(Entity<SpamEmitSoundComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.MaxExtraInterval = Random.NextFloat(ent.Comp.MaxExtraInterval);
        // Give the accumulator a random initial boost so they don't all start at once
        ent.Comp.Accumulator += Random.NextFloat(ent.Comp.RollInterval + ent.Comp.MaxExtraInterval);
    }
}
