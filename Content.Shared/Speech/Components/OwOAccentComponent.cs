using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// I am not typing out an example for this one.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(OwOAccentSystem))]
public sealed partial class OwOAccentComponent : BaseAccentComponent;
