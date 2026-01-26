using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
///     Component for clothing items that should hide the name of the speaker.
///     The name is replaced with a generic descriptor of their appearance.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(VoiceObfuscatorSystem))]
public sealed partial class VoiceObfuscatorComponent : Component;
