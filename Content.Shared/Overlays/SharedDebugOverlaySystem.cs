using Robust.Shared.Serialization;

namespace Content.Shared.Overlays;

public abstract class SharedDebugOverlaySystem<TPayload> : EntitySystem
    where TPayload : DebugOverlayPayload, new()
{

}

[Serializable, NetSerializable]
public abstract class DebugOverlayPayload : EntityEventArgs
{
    public bool OverlayEnabled;
}
