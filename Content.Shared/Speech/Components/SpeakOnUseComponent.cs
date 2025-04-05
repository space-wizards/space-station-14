using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSpeakOnUseSystem))]
public sealed partial class SpeakOnUseComponent : Component
{
    /// <summary>
    /// The ftl id of the sentence that the user will speak.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? Sentence;
}
