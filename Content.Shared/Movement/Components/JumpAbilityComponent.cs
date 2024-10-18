using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;


namespace Content.Shared.Movement.Components;

/// <summary>
/// Jump setting component.
/// To add a jump entity use <see cref="ActionGrantComponent"/> and <see cref="ItemActionGrantComponent"/> for an item.
/// The basic action prototype is "ActionGravityJump".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JumpAbilityComponent : Component
{
    /// <summary>
    /// How powerful the jump will be when activated
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpPower = 5f;

    /// <summary>
    /// This gets played whenever a used jump. Predicted by the client.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier SoundJump = new SoundPathSpecifier("/Audio/Effects/gravity_jump.ogg");
}


public sealed partial class GravityJumpEvent : InstantActionEvent;

