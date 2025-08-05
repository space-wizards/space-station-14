using Content.Shared.Animals.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Animals.Components;

/// <summary>
/// Makes an entity able to memorize chat/radio messages.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class ParrotMemoryComponent : Component
{
    /// <summary>
    /// List of SpeechMemory records this entity has learned.
    /// </summary>
    [DataField]
    public List<SpeechMemory> SpeechMemories = new();

    /// <summary>
    /// The % chance an entity with this component learns a phrase when learning is off cooldown.
    /// </summary>
    [DataField]
    public float LearnChance = 0.6f;

    /// <summary>
    /// Time after which another attempt can be made at learning a phrase.
    /// </summary>
    [DataField]
    public TimeSpan LearnCooldown = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Next time at which the parrot can attempt to learn something.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextLearnInterval = TimeSpan.Zero;

    /// <summary>
    /// The number of speech entries that are remembered.
    /// </summary>
    [DataField]
    public int MaxSpeechMemory = 50;

    /// <summary>
    /// Minimum length of a speech entry.
    /// </summary>
    [DataField]
    public int MinEntryLength = 4;

    /// <summary>
    /// Maximum length of a speech entry.
    /// </summary>
    [DataField]
    public int MaxEntryLength = 50;
}

[Serializable, NetSerializable]
public record struct SpeechMemory(NetUserId? NetUserId, string Message);
