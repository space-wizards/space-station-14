using Content.Shared.Shuttles.Systems;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to a component when it is queued or is travelling via FTL.
/// </summary>
[RegisterComponent]
public sealed partial class FTLComponent : Component
{
    [ViewVariables]
    public FTLState State = FTLState.Available;

    [ViewVariables(VVAccess.ReadWrite)]
    public float StartupTime = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TravelTime = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator = 0f;

    /// <summary>
    /// Target Uid to dock with at the end of FTL.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("targetUid")]
    public EntityUid? TargetUid;

    [ViewVariables(VVAccess.ReadWrite), DataField("targetCoordinates")]
    public EntityCoordinates TargetCoordinates;

    /// <summary>
    /// Should we dock with the target when arriving or show up nearby.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("dock")]
    public bool Dock;

    /// <summary>
    /// If we're docking after FTL what is the prioritised dock tag (if applicable).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("priorityTag", customTypeSerializer:typeof(PrototypeIdSerializer<TagPrototype>))]
    public string? PriorityTag;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundTravel")]
    public SoundSpecifier? TravelSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_progress.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f).WithLoop(true)
    };

    public EntityUid? TravelStream;
}
