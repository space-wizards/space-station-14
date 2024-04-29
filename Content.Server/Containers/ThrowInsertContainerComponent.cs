using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Containers;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ThrowInsertContainerSystem))]
public sealed partial class ThrowInsertContainerComponent : Component
{
    [DataField(required: true)]
    public string? ContainerId;

    /// <summary>
    /// Throw chance of hitting the container
    /// </summary>
    [DataField]
    public float Probability = 0.75f;

    /// <summary>
    /// Sound played when an object is throw into the container.
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Effects/trashbag1.ogg");

    /// <summary>
    /// Sound played when an item is thrown and misses the container.
    /// </summary>
    [DataField]
    public SoundSpecifier? MissSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField]
    public LocId MissLocString = "container-thrown-missed";
}
