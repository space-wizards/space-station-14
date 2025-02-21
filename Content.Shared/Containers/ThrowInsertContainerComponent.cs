using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers;

/// <summary>
/// Allows objects to fall inside the Container when thrown
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ThrowInsertContainerComponent : Component
{
    [DataField(required: true)]
    public string ContainerId = string.Empty;

    /// <summary>
    /// Probability of missing the container
    /// </summary>
    [DataField]
    public float BaseHitProbability = 0.25f;

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

/// <summary>
///     Event raised on the item when it's been thrown into a container.
/// </summary>
[ByRefEvent]
public record struct ThrownIntoContainerEvent(float Modifier);

/// <summary>
///     Event raised on the person who threw an item into a container.
/// </summary>
[ByRefEvent]
public record struct ThrownIntoContainerThrowerEvent(float Modifier);
