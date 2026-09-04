using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Hiss!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(LizardAccentSystem))]
public sealed partial class LizardAccentComponent : BaseAccentComponent;
