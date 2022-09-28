using System.Linq;
using Content.Server.Box.Components;
using Content.Server.Storage.Components;
using Content.Shared.Box;
using Content.Shared.Box.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Player;

namespace Content.Server.Box;

public sealed class BoxSystem : SharedBoxSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BoxComponent, StorageBeforeCloseEvent>(OnBeforeStorageClosed);
        SubscribeLocalEvent<BoxComponent, StorageAfterOpenEvent>(AfterStorageOpen);
    }

    private void OnBeforeStorageClosed(EntityUid uid, BoxComponent component, StorageBeforeCloseEvent args)
    {
        //Grab the first mob in the hash to set as the mover and to prevent other mobs from entering.
        var firstMob = args.Contents.Where(e => HasComp<MobMoverComponent>(e)).Select(e => e).FirstOrDefault();

        //Set the movement relay for the box as the first mob
        if (component.Mover == null && args.Contents.Contains(firstMob))
        {
            var relay = EnsureComp<RelayInputMoverComponent>(firstMob);
            relay.RelayEntity = uid;
            component.Mover = firstMob;
        }

        //Check the contents of the box and remove any mobs other than the driver.
        foreach (var entity in args.Contents)
        {
            if (HasComp<MobMoverComponent>(entity) && firstMob != entity)
                args.Contents.Remove(entity);
        }
    }

    private void AfterStorageOpen(EntityUid uid, BoxComponent component, StorageAfterOpenEvent args)
    {
        if (component.Mover != null)
        {
            RemComp<RelayInputMoverComponent>(component.Mover.Value);
            RaiseNetworkEvent(new PlayBoxEffectMessage(component.Owner, component.Mover.Value), Filter.PvsExcept(component.Owner));
            _audio.PlayPvs(component.EffectSound, component.Owner);
        }

        component.Mover = null;
    }
}
