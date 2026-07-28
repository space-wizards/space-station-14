using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Marks a speech status effect that transforms spoken text into Ratvarian.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RatvarianLanguageSystem))]
public sealed partial class RatvarianLanguageComponent : BaseAccentComponent;
