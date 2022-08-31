using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Data for melee lunges from attacks.
/// </summary>
[Serializable, NetSerializable]
public sealed class MeleeLungeEvent : EntityEventArgs
{
    public EntityUid Entity;

    /// <summary>
    /// The relative local position to the <see cref="Entity"/>
    /// </summary>
    public Vector2 LocalPos;

    /// <summary>
    /// Entity to spawn for the animation
    /// </summary>
    public string? Animation;

    public MeleeLungeEvent(EntityUid uid, Vector2 localPos, string? animation)
    {
        Entity = uid;
        LocalPos = localPos;
        Animation = animation;
    }
}