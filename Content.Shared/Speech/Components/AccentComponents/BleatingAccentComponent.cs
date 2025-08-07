using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components.AccentComponents;

/// <summary>
/// Makes this entity speak like a sheep or a goat in all chat messages it sends.
/// Repeats the vowel in certain consonant-vowel pairs so you taaaalk liiiike thiiiis.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BleatingAccentComponent : Component
{

}
