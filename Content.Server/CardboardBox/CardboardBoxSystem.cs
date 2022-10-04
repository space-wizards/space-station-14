using System.Linq;
using Content.Shared.CardboardBox.Components;
using Content.Server.Storage.Components;
using Content.Shared.CardboardBox;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Player;

namespace Content.Server.CardboardBox;

public sealed class CardboardBoxSystem : SharedCardboardBoxSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CardboardBoxComponent, StorageBeforeCloseEvent>(OnBeforeStorageClosed);
        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterOpenEvent>(AfterStorageOpen);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var box in EntityQuery<CardboardBoxComponent>())
        {
            if (box.EffectPlayed)
                box.Accumulator += frameTime;

            if (box.Accumulator >= box.EffectCooldown)
            {
                box.Accumulator -= box.EffectCooldown;
                box.EffectPlayed = false;
            }
        }
    }

    private void OnBeforeStorageClosed(EntityUid uid, CardboardBoxComponent component, StorageBeforeCloseEvent args)
    {

        var mobMover = args.Contents.Where(HasComp<MobMoverComponent>).ToList();

        //Grab the first mob to set as the mover and to prevent other mobs from entering.
        foreach (var mover in mobMover)
        {
            //Set the movement relay for the box as the first mob
            if (component.Mover == null && args.Contents.Contains(mover))
            {
                var relay = EnsureComp<RelayInputMoverComponent>(mover);
                _mover.SetRelay(mover, uid, relay);
                component.Mover = mover;
            }

            if (mover != component.Mover)
                args.Contents.Remove(mover);
        }
    }

    private void AfterStorageOpen(EntityUid uid, CardboardBoxComponent component, StorageAfterOpenEvent args)
    {
        //Remove the mover after the box is opened and play the effect if it hasn't been played yet.
        if (component.Mover != null)
        {
            RemComp<RelayInputMoverComponent>(component.Mover.Value);
            if (!component.EffectPlayed)
            {
                RaiseNetworkEvent(new PlayBoxEffectMessage(component.Owner, component.Mover.Value), Filter.PvsExcept(component.Owner));
                _audio.PlayPvs(component.EffectSound, component.Owner);
                component.EffectPlayed = true;
            }
        }

        component.Mover = null;
    }
}
