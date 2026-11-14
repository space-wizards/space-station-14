using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Das accent, ja?
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(GermanAccentSystem))]
public sealed partial class GermanAccentComponent : BaseAccentComponent;
