using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared.Weapons.Hitscan.Events;

/// <summary>
/// Raised on the hitscan entity when "fired". This could be from reflections or from the gun.
/// </summary>
[ByRefEvent]
public record struct HitscanFiredEvent
{
    /// <summary>
    /// Location the hitscan was fired from.
    /// </summary>
    public EntityCoordinates FromCoordinates;

    /// <summary>
    /// Direction that was fired.
    /// </summary>
    public Vector2 ShotDirection;

    /// <summary>
    /// Gun that was fired.
    /// </summary>
    public EntityUid GunUid;

    /// <summary>
    /// Who shot the gun. Could be the gun itself!
    /// </summary>
    public EntityUid Shooter;

    /// <summary>
    /// Target that was being aimed at (Not necessarly hit)
    /// </summary>
    public EntityUid? Target;
}

/// <summary>
/// Gets raised on a hitscan laser if it has hit an entity.
/// </summary>
[ByRefEvent]
public record struct HitscanHitEntityEvent
{
    /// <summary>
    /// Location the hitscan was fired from.
    /// </summary>
    public EntityCoordinates FromCoordinates;

    /// <summary>
    /// Direction that was fired.
    /// </summary>
    public Vector2 ShotDirection;

    /// <summary>
    /// The entity that got hit, if null the raycast didn't hit anyone.
    /// </summary>
    public EntityUid HitEntity;

    /// <summary>
    /// Gun that fired the raycast.
    /// </summary>
    public EntityUid GunUid;

    /// <summary>
    /// Who shot the gun. Could be the gun itself!
    /// </summary>
    public EntityUid Shooter;

    /// <summary>
    /// Was this canceled? Used for stuff like reflections.
    /// </summary>
    public bool Canceled;
}

/// <summary>
/// Results of a hitscan raycast - useful for visuals and things that rely on more than just what entity was hit.
/// </summary>
[ByRefEvent]
public record struct HitscanRaycastResultsEvent
{
    /// <summary>
    /// Results of the raycast - if null, the raycast didn't hit anything!
    /// </summary>
    public RayCastResults? RaycastResults;

    /// <summary>
    /// Location the hitscan was fired from.
    /// </summary>
    public EntityCoordinates FromCoordinates;

    /// <summary>
    /// Direction that was fired.
    /// </summary>
    public Vector2 ShotDirection;

    /// <summary>
    /// How far the hitscan tried to go to intersect with a target.
    /// </summary>
    public float DistanceTried;
}
