using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Plays a sound whenever InputMover is running.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MovementSoundComponent : Component
{
    /// <summary>
    /// Sound to play when InputMover has inputs.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? Sound;

    [DataField, AutoNetworkedField]
    public EntityUid? SoundEntity;
}
