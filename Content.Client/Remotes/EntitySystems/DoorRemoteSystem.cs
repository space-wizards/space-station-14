using Content.Client.Remote.UI;
using Content.Client.Items;
using Content.Shared.Remotes.EntitySystems;
using Content.Shared.Remotes.Components;

namespace Content.Client.Remotes.EntitySystems;

public sealed class DoorRemoteSystem : SharedDoorRemoteSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<DoorRemoteComponent>(ent => new DoorRemoteStatusControl(ent));
    }
}
