using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components.AccentComponents;

/// <summary>
/// French accent replaces spoken letters. "th" becomes "z" and "H" at the start of a word becomes "'".
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FrenchAccentComponent : Component
{

}
