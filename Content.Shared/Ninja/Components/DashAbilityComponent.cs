using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(DashAbilitySystem))]
public sealed partial class DashAbilityComponent : Component;

public sealed partial class DashEvent : WorldTargetActionEvent;
