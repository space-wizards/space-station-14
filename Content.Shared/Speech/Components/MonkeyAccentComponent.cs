using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// OOH AAH!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(MonkeyAccentSystem))]
public sealed partial class MonkeyAccentComponent : BaseAccentComponent;
