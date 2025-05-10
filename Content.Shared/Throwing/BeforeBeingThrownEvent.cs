using System.Numerics;


namespace Content.Shared.Throwing;

[ByRefEvent]
public struct BeforeBeingThrownEvent
{
    public BeforeBeingThrownEvent(EntityUid itemUid, Vector2 direction, float throwSpeed)
    {
        ItemUid = itemUid;
        Direction = direction;
        ThrowSpeed = throwSpeed;
    }

    public EntityUid ItemUid { get; set; }
    public Vector2 Direction { get; }
    public float ThrowSpeed { get; set;}
}
