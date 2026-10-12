using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Makes... the... entity... talk... like... this...
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SlowAccentSystem))]
public sealed partial class SlowAccentComponent : BaseAccentComponent;
