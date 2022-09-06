using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised whenever a muzzle flash client-side entity needs to be spawned.
/// </summary>
[Serializable, NetSerializable]
public sealed class MuzzleFlashEvent : EntityEventArgs
{
    public EntityUid Uid;
    public string Prototype;

    /// <summary>
    /// Should the effect match the rotation of the entity.
    /// </summary>
    public bool MatchRotation;

    public MuzzleFlashEvent(EntityUid uid, string prototype, bool matchRotation = false)
    {
        Uid = uid;
        Prototype = prototype;
        MatchRotation = matchRotation;
    }
}
