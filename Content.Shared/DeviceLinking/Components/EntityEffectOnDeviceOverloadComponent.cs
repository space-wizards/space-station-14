using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// Raises effects an entity when a device link overloads.
/// An overload happens when a device link sink is invoked to many times per tick
/// and it raises a <see cref="DeviceLinkOverloadedEvent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EntityEffectOnDeviceOverloadSystem))]
public sealed partial class EntityEffectOnDeviceOverloadComponent : Component
{
    /// <summary>
    /// The effects to apply to the entity when it overloads.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects;
}
