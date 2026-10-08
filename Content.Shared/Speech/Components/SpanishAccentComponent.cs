using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Mios dios!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SpanishAccentSystem))]
public sealed partial class SpanishAccentComponent : BaseAccentComponent;
