using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Components;

/// <summary>
/// Causes this entity to react to ghost player using the "Boo!" action by speaking
/// a randomly chosen message from a specified set.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SpookySpeakerComponent : Component
{
    /// <summary>
    /// ProtoId of the LocalizedDataset to use for messages.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> MessageSet;

    /// <summary>
    /// Probability that this entity will speak if activated by a Boo action.
    /// This is so whole banks of entities don't trigger at the same time.
    /// </summary>
    [DataField]
    public float SpeakChance = 0.5f;

    /// <summary>
    /// Minimum time that must pass after speaking before this entity can speak again.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Time when the cooldown will have elapsed and the entity can speak again.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextSpeakTime;
}
