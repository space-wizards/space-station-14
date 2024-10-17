using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JumpComponent : Component
{
    /// <summary>
    /// How powerful the jump will be when activated
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JumpPower = 5f;


    // Logical variables
    [DataField]
    public EntProtoId Action = "ActionGravityJump";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public bool? IsClothing;

    [DataField, AutoNetworkedField]
    public EntityUid OnClothingEntity;


    // Sound
    [DataField("soundJump"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public SoundSpecifier SoundJump = new SoundPathSpecifier("/Audio/Effects/gravity_jump.ogg");
}


public sealed partial class GravityJumpEvent : InstantActionEvent;

