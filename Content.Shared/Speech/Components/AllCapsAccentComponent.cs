using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Marks a speech status effect that transforms spoken text to uppercase.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AllCapsAccentSystem))]
public sealed partial class AllCapsAccentComponent : BaseAccentComponent;
