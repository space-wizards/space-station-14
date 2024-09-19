using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Adds an action to dash, teleport to clicked position, when this item is held.
/// Cancel <see cref="CheckDashEvent"/> to prevent using it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DashAbilitySystem)), AutoGenerateComponentState]
public sealed partial class DashAbilityComponent : Component
{
    /// <summary>
    /// The action id for dashing.
    /// </summary>
    [DataField]
    public EntProtoId<WorldTargetActionComponent> DashAction = "ActionEnergyKatanaDash";

    [DataField, AutoNetworkedField]
    public EntityUid? DashActionEntity;
}

public sealed partial class DashEvent : WorldTargetActionEvent;
