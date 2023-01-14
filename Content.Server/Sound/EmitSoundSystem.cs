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
        foreach (var soundSpammer in EntityQuery<SpamEmitSoundComponent>())
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
                    Popup.PopupEntity(Loc.GetString(soundSpammer.PopUp), soundSpammer.Owner);
                TryEmitSound(soundSpammer);
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(HandleEmitSoundOnTrigger);
        SubscribeLocalEvent<EmitSoundOnUIOpenComponent, AfterActivatableUIOpenEvent>(HandleEmitSoundOnUIOpen);
    }

    private void HandleEmitSoundOnUIOpen(EntityUid eUI, EmitSoundOnUIOpenComponent component, AfterActivatableUIOpenEvent args)
    {
        TryEmitSound(component, args.User);
    }

    private void HandleEmitSoundOnTrigger(EntityUid uid, EmitSoundOnTriggerComponent component, TriggerEvent args)
    {
        TryEmitSound(component, args.User);
        args.Handled = true;
    }
}
