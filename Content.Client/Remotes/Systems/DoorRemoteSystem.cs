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

        Subs.ItemStatus<DoorRemoteComponent>(ent => new DoorRemoteStatusControl(ent, Timing));
    }
}
