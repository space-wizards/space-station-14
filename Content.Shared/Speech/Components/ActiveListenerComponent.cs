using Content.Shared.Chat;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// This component is used to relay speech events to other systems.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveListenerComponent : Component
{
    /// <summary>
    /// The range in which to listen to speech.
    /// </summary>
    [DataField]
    public float Range = SharedChatSystem.VoiceRange;
}
