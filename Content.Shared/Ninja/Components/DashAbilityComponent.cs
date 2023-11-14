using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Adds an action to dash, teleport to clicked position, when this item is held.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DashAbilitySystem)), AutoGenerateComponentState]
public sealed partial class DashAbilityComponent : Component
{
    /// <summary>
    /// The action id for dashing.
    /// </summary>
    [DataField]
    public EntProtoId DashAction = "ActionEnergyKatanaDash";

    [DataField, AutoNetworkedField]
    public EntityUid? DashActionEntity;

    /// <summary>
    /// Sound played when using dash action.
    /// </summary>
    [DataField("blinkSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f)
    };
}

public sealed partial class DashEvent : WorldTargetActionEvent { }
