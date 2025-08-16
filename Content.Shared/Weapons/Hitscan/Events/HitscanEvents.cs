using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Hitscan.Events;

/// <summary>
/// Raised on the hitscan entity when "fired". This could be from reflections or from the gun. This is the catalyst that
/// other systems will listen for to actually shoot the gun.
/// </summary>
[ByRefEvent]
public record struct HitscanTraceEvent
{
    /// <summary>
    /// Location the hitscan was fired from.
    /// </summary>
    public EntityCoordinates FromCoordinates;

    /// <summary>
    /// Direction that the ray was fired towards.
    /// </summary>
    public Vector2 ShotDirection;

    /// <summary>
    /// Gun that was fired - this will always be the original weapon even if reflected.
    /// </summary>
    public EntityUid Gun;

    /// <summary>
    /// Player who shot the gun, if null the gun was fired by itself.
    /// </summary>
    public EntityUid? Shooter;

    /// <summary>
    /// Target that was being aimed at (Not necessarily hit).
    /// </summary>
    public EntityUid? Target;
}

/// <summary>
/// Results of a hitscan raycast and will be raised on the raycast entity on itself. Stuff like the reflection system
/// or damage system will listen for this.
/// </summary>
[ByRefEvent]
public record struct HitscanRaycastFiredEvent
{
    /// <summary>
    /// Location the hitscan was fired from.
    /// </summary>
    public EntityCoordinates FromCoordinates;

    /// <summary>
    /// Direction that the ray was fired towards.
    /// </summary>
    public Vector2 ShotDirection;

    /// <summary>
    /// The entity that got hit, if null the raycast didn't hit anyone.
    /// </summary>
    public EntityUid? HitEntity;

    /// <summary>
    /// Gun that fired the raycast.
    /// </summary>
    public EntityUid Gun;

    /// <summary>
    /// Player who shot the gun, if null the gun was fired by itself.
    /// </summary>
    public EntityUid? Shooter;

    /// <summary>
    /// How far the hitscan tried to go to intersect with a target.
    /// </summary>
    public float DistanceTried;

    /// <summary>
    /// Set to true the hitscan is cancelled (e.g. due to reflection).
    /// Cancelled hitscans should not apply damage or trigger follow-up effects.
    /// </summary>
    public bool Canceled;
}

[ByRefEvent]
public record struct HitscanDamageDealtEvent
{
    /// <summary>
    /// Target that was dealt damage.
    /// </summary>
    public EntityUid Target;

    /// <summary>
    /// The amount of damage that the target was dealt.
    /// </summary>
    public DamageSpecifier DamageDealt;
}
