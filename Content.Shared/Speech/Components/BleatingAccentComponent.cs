using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Makes this entity speak like a sheep or a goat in all chat messages it sends.
/// Repeats the vowel in certain consonant-vowel pairs so you taaaalk liiiike thiiiis.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BleatingAccentSystem))]
public sealed partial class BleatingAccentComponent : BaseAccentComponent;
