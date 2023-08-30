using Content.Server.Chat.Systems;

namespace Content.Server.Speech.Components;

/// <summary>
///     This component is used to relay speech events to other systems.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveListenerComponent : Component
{
    [DataField("range")]
    public float Range = ChatSystem.VoiceRange;
}
