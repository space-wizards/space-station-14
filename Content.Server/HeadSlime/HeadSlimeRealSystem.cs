using Content.Server.Audio;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.HeadSlimeReal;
using Content.Shared.Popups;
using Content.Server.Inventory;
using Robust.Shared.Containers;
using Content.Shared.Inventory.Events;
using Content.Server.HeadSlime;

namespace Content.Server.HeadSlimeReal;

public sealed class HeadSlimeRealSystem : SharedHeadSlimeRealSystem
{
    [Dependency] private readonly HeadSlimeSystem _headslime = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadSlimeRealComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<HeadSlimeRealComponent, EntGotRemovedFromContainerMessage>(OnUnequipped);
        SubscribeLocalEvent<HeadSlimeRealComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void OnEquipped(EntityUid uid, HeadSlimeRealComponent component, GotEquippedEvent args)
    {
        component.CanTransfer = false;
        //Run Head Slime Comp stuff here
        _headslime.HeadSlimeEntity(args.Equipee, null, false, true);
    }

    private void OnUnequipped(EntityUid uid, HeadSlimeRealComponent component, EntGotRemovedFromContainerMessage args)
    {
        component.CanTransfer = true;
    }
    
    private void OnRemoveAttempt(EntityUid uid, HeadSlimeRealComponent comp, ContainerGettingRemovedAttemptEvent args)
    {
        if(!comp.CanTransfer)
            args.Cancel();
    }
}
