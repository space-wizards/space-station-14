using Content.Shared.Dataset;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Action components which should write a message to ICChat on use
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SpeakOnActionSystem))]
public sealed partial class SpeakOnActionComponent : Component
{
    /// <summary>
    /// The ftl id of the sentence that the user will speak.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? Sentence;

    /// <summary>
    /// A dataset of possible things the user could speak. If specified, the system will use this instead of the single LocId sentence.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LocalizedDatasetPrototype>? DialogueDataset;

    /// <summary>
    /// The probability of the user speaking when using this action. If less than 1, the user is not guaranteed to speak when using the action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeakChance = 1f;
}
