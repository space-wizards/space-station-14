using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Buzzz!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(MothAccentSystem))]
public sealed partial class MothAccentComponent : BaseAccentComponent;
