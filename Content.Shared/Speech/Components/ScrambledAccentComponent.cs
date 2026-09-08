using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Structure is sentence weird accent this with!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ScrambledAccentSystem))]
public sealed partial class ScrambledAccentComponent : BaseAccentComponent;
