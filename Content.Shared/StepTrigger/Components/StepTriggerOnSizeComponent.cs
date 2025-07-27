using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
///     This is used for marking step trigger events that require the user to be
///     some specific <see cref="CollisionGroup"/> (e.g., CollisionGroup.MobMask, CollisionGroup.LargeMobMask).
///     Useful for making small creatures like cockroaches only react to being stepped on by mobs or players,
///     but not by other small entities.
/// </summary>
/// <remarks>
///     Works only with <see cref="StepTriggerComponent"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class StepTriggerOnSizeComponent : Component
{
    /// <summary>
    ///     The collision mask an entity must match in order to activate the step trigger.
    ///     By default, is <see cref="CollisionGroup.MobMask"/> and <see cref="CollisionGroup.LargeMobMask"/>,
    ///     to allow activation by players and mobs and mechs.
    /// </summary>
    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.MobMask | CollisionGroup.LargeMobMask;
}
