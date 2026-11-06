using Content.Client.Items;
using Content.Client.Remotes.UI;
using Content.Shared.Item;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Client.Remotes.Systems;

public sealed partial class DoorRemoteSystem : SharedDoorRemoteSystem
{
    public ProtoId<ItemStatusPrototype> DoorRemoteItemStatus = "DoorRemote";

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<DoorRemoteComponent>(ent => new DoorRemoteStatusControl(ent), DoorRemoteItemStatus);
        SubscribeLocalEvent<DoorRemoteComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
    }

    private void OnAutoHandleState(Entity<DoorRemoteComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ent.Comp.IsStatusControlUpdateRequired = true;
    }
}
