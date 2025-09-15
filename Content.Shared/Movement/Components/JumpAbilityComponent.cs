using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Components;

/// <summary>
/// A component for configuring the settings for the jump action.
/// To give the jump action to an entity use <see cref="ActionGrantComponent"/> and <see cref="ItemActionGrantComponent"/>.
/// The basic action prototype is "ActionGravityJump".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedJumpAbilitySystem))]
public sealed partial class JumpAbilityComponent : Component
{
    /// <summary>
    /// The action prototype that allows you to jump.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionGravityJump";

    /// <summary>
    /// Entity to hold the action prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// How far you will jump (in tiles).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpDistance = 5f;

    /// <summary>
    /// Basic “throwing” speed for TryThrow method.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpThrowSpeed = 10f;

    /// <summary>
    /// Whether this entity can collide with another entity, leading to it getting knocked down.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanCollide = false;

    /// <summary>
    /// The duration of the knockdown in case of a collision from CanCollide.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CollideKnockdown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// This gets played whenever the jump action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? JumpSound;

    /// <summary>
    /// The popup to show if the entity is unable to perform a jump.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? JumpFailedPopup = "jump-ability-failure";
}

public sealed partial class GravityJumpEvent : InstantActionEvent;

