using System.Numerics;
using Robust.Shared.Serialization;

/// <summary>
/// Data for CPR animations
/// </summary>
namespace Content.Shared.CPR;

[Serializable, NetSerializable]
public sealed partial class CPRLungeEvent : EntityEventArgs
{
    public NetEntity Ent;

    public CPRLungeEvent(NetEntity entity)
    {
        Ent = entity;
    }
}
