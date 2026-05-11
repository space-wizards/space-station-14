using Content.Client.Items;
using Content.Client.Remotes.UI;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;

namespace Content.Client.Remotes.Systems;

public sealed class DoorRemoteSystem : SharedDoorRemoteSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<DoorRemoteComponent>(ent => new DoorRemoteStatusControl(ent));
        SubscribeLocalEvent<DoorRemoteComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
    }

    private void OnAutoHandleState(Entity<DoorRemoteComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ent.Comp.IsStatusControlUpdateRequired = true;
    }
}
