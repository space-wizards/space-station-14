using System.Numerics;

namespace Content.Shared.Throwing;

[ByRefEvent]
public struct BeforeThrowEvent
{
    public BeforeThrowEvent(EntityUid itemUid, Vector2 direction, float throwStrength,  EntityUid playerUid)
    {
        ItemUid = itemUid;
        Direction = direction;
        ThrowStrength = throwStrength;
        PlayerUid = playerUid;
    }

    public EntityUid ItemUid { get; set; }
    public Vector2 Direction { get; }
    public float ThrowStrength { get; set;}
    public EntityUid PlayerUid { get; }

    public bool Cancelled = false;
}
