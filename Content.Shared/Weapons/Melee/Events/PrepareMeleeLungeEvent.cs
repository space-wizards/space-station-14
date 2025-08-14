using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised on the attacker before spawning melee lunge visuals on the client.
/// Systems may adjust the visual behavior by setting the flags below.
/// </summary>
[ByRefEvent]
public record struct PrepareMeleeLungeEvent(
    EntityUid User,
    EntityUid Weapon,
    Angle Angle,
    Vector2 LocalPos,
    string? Animation)
{
    /// <summary>
    /// If true, spawns the visual at world MapCoordinates (based on the user's current world position)
    /// instead of at the user's local coordinates. Use this to avoid inheriting user rotation.
    /// </summary>
    public bool SpawnAtMap;

    /// <summary>
    /// If true, disables tracking the visual to the user (i.e., removes the TrackUser behavior).
    /// </summary>
    public bool DisableTracking;
}
