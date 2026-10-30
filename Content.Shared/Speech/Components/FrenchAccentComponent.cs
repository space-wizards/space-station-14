using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// French accent replaces spoken letters. "th" becomes "z" and "H" at the start of a word becomes "'".
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(FrenchAccentSystem))]
public sealed partial class FrenchAccentComponent : BaseAccentComponent;
