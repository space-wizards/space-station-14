using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Action components which should write a message to ICChat on use
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSpeakOnActionSystem))]
public sealed partial class SpeakOnActionComponent : Component
{
    /// <summary>
    /// The ftl id of the sentence that the user will speak.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? Sentence;
}
