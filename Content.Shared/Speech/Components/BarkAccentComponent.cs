using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Arf! Arso makes "L" into "R"!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BarkAccentSystem))]
public sealed partial class BarkAccentComponent : BaseAccentComponent;
