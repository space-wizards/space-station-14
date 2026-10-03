using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     Allows mobs to toggle their eyes between being closed and being not closed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class EyeClosingComponent : Component
{
    /// <summary>
    /// Default eyes opening sound.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultEyeOpen = new("EyeOpen");

    /// <summary>
    /// Default eyes closing sound.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultEyeClose = new("EyeClose");

    /// <summary>
    /// The prototype to grant to enable eye-toggling action.
    /// </summary>
    [DataField]
    public EntProtoId EyeToggleAction = "ActionToggleEyes";

    /// <summary>
    /// The actual eye toggling action entity itself.
    /// </summary>
    [DataField]
    public EntityUid? EyeToggleActionEntity;

    /// <summary>
    /// Sound to play when opening eyes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier EyeOpenSound = new SoundCollectionSpecifier(DefaultEyeOpen);

    /// <summary>
    /// Sound to play when closing eyes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier EyeCloseSound = new SoundCollectionSpecifier(DefaultEyeClose);

    /// <summary>
    /// Toggles whether the eyes are open or closed. This is really just exactly what it says on the tin. Honest.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EyesClosed;

    /// <summary>
    /// The previous state of eyes closed. Used to ensure relevant audio / visual effects are only emitted once per change.
    /// </summary>
    [DataField]
    public bool PreviousEyelidPosition;

    /// <summary>
    /// Whether the eye closing was naturally created or not.
    /// </summary>
    [DataField]
    public bool NaturallyCreated;
}
