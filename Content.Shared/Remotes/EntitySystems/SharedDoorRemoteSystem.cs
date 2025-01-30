using Content.Shared.Popups;
using Content.Shared.Remotes.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Remotes.EntitySystems;

public abstract class SharedDoorRemoteSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DoorRemoteComponent, DoorRemoteModeChangeMessage>(OnDoorRemoteModeChange);
    }

    private void OnDoorRemoteModeChange(Entity<DoorRemoteComponent> ent, ref DoorRemoteModeChangeMessage args)
    {
        ent.Comp.Mode = args.Mode;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed class DoorRemoteModeChangeMessage : BoundUserInterfaceMessage
{
    public OperatingMode Mode;
}

[Serializable, NetSerializable]
public enum DoorRemoteUiKey : byte
{
    Key
}
