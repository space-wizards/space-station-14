using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Content.Shared.Physics;
using Content.Shared.Damage;
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

    /// <summary>
    /// Damage dealt after dashing through someone.
    /// </summary>
    [DataField]
    public DamageSpecifier DashDamage = new()
    {
        DamageDict = new()
        {
            { "Slash", 30 }
        }
    };

    [DataField]
    public int CollisionMask = (int) CollisionGroup.Opaque;
}

public sealed partial class DashEvent : WorldTargetActionEvent { }
