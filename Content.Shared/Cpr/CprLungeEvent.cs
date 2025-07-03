using Robust.Shared.Serialization;

namespace Content.Shared.Cpr;

/// <summary>
/// Data for CPR animations
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CprLungeEvent : EntityEventArgs
{
    public NetEntity Ent;

    public CprLungeEvent(NetEntity entity)
    {
        Ent = entity;
    }
}
