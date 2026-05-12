using Robust.Shared.GameStates;

namespace Content.Shared._Tinystation.Knight.Components;

/// <summary>
///     Prevents non-BibleUser entities from picking up or equipping this item.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RequireBibleUserComponent : Component;
