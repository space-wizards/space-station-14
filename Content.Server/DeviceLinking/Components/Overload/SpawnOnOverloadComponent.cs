using Content.Server.DeviceLinking.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components.Overload;

/// <summary>
/// Spawns an entity when a device link overloads.
/// An overload happens when a device link sink is invoked to many times per tick
/// and it raises a <see cref="Content.Server.DeviceLinking.Events.DeviceLinkOverloadedEvent"/>
/// </summary>
[RegisterComponent]
[Access(typeof(DeviceLinkOverloadSystem))]
public sealed partial class SpawnOnOverloadComponent : Component
{
    /// <summary>
    /// The entity prototype to spawn when the device overloads
    /// </summary>
    [DataField("spawnedPrototype")]
    public EntProtoId Prototype = "PuddleSparkle";
}
