using Content.Server.Box.Components;
using Content.Server.Storage.Components;
using Content.Shared.Box;
using Content.Shared.Box.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.Player;

namespace Content.Server.Box;

public sealed class BoxSystem : SharedBoxSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BoxComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<BoxComponent, StorageAfterOpenEvent>(AfterStorageOpen);
    }

    private void OnRelayMovement(EntityUid uid, BoxComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        var relay = EnsureComp<RelayInputMoverComponent>(args.Entity);
        relay.RelayEntity = uid;
        component.Mover = args.Entity;
    }

    private void AfterStorageOpen(EntityUid uid, BoxComponent component, StorageAfterOpenEvent args)
    {
        if (component.Mover != null)
        {
            RemComp<RelayInputMoverComponent>(component.Mover.Value);
            component.Mover = null;
        }

        RaiseNetworkEvent(new PlayBoxEffectMessage(component.Owner), Filter.PvsExcept(component.Owner));

        _audio.PlayPvs(component.EffectSound, component.Owner);
    }
}
