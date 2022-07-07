using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised whenever a muzzle flash client-side entity needs to be spawned.
/// </summary>
[Serializable, NetSerializable]
public sealed class MuzzleFlashEvent : EntityEventArgs
{
    public Angle Angle;
    public string Prototype;

    public MuzzleFlashEvent(Angle angle, string prototype)
    {
        Angle = angle;
        Prototype = prototype;
    }
}
