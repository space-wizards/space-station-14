using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// sdrawkcaB
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BackwardsAccentSystem))]
public sealed partial class BackwardsAccentComponent : BaseAccentComponent;
