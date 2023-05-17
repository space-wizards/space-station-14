using Content.Server.Worldgen.Prototypes;
using Content.Server.Worldgen.Systems.GC;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Worldgen.Components.GC;

/// <summary>
///     This is used for whether or not a GCable object is "dirty". Firing GCDirtyEvent on the object is the correct way to
///     set this up.
/// </summary>
[RegisterComponent]
[Access(typeof(GCQueueSystem))]
public sealed class GCAbleObjectComponent : Component
{
    /// <summary>
    ///     Which queue to insert this object into when GCing
    /// </summary>
    [DataField("queue", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<GCQueuePrototype>))]
    public string Queue = default!;
}

