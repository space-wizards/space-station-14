using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Lithping!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(FrontalLispSystem))]
public sealed partial class FrontalLispComponent : BaseAccentComponent;
