using Content.Shared.Actions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Events;

/// <summary>
/// Action event to toggle the voice/sex modification controller menu.
/// </summary>
public sealed partial class VoiceSexModificationControllerToggleMenuEvent : InstantActionEvent;

/// <summary>
/// UI key for the voice/sex modification controller.
/// </summary>
[Serializable, NetSerializable]
public enum VoiceSexModificationControllerKey : byte
{
    Key,
}

/// <summary>
/// BUI state for the voice/sex modification controller.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceSexModificationControllerBuiState : BoundUserInterfaceState
{
    /// <summary>
    /// The species of the entity being targetted.
    /// </summary>
    public ProtoId<SpeciesPrototype>? Species;

    /// <summary>
    /// The current sex of the entity being targetted.
    /// </summary>
    public Sex? CurrentSex;

    /// <summary>
    /// The current voice of the entity being targetted.
    /// </summary>
    public ProtoId<EmoteSoundsPrototype>? CurrentVoice;
}

/// <summary>
/// Predicted message with information for what voice/sex should be changed.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceSexModificationMessage(ProtoId<EmoteSoundsPrototype>? voice, Sex sex)
    : BoundUserInterfaceMessage
{
    /// <summary>
    /// The selected voice prototype.
    /// </summary>
    public readonly ProtoId<EmoteSoundsPrototype>? Voice = voice;

    /// <summary>
    /// The selected sex.
    /// </summary>
    public readonly Sex Sex = sex;
}
