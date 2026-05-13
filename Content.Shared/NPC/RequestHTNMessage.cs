using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed partial class RequestHTNMessage : EntityEventArgs
{
    public bool Enabled;
}

