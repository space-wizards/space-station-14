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
    /// Sound to play when opening eyes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier EyeOpenSound = new SoundCollectionSpecifier(DefaultEyeOpen);

    /// <summary>
    /// Sound to play when closing eyes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier EyeCloseSound = new SoundCollectionSpecifier(DefaultEyeClose);

    /// <summary>
    /// Toggles whether the eyes are open or closed. This is really just exactly what it says on the tin. Honest.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool EyesClosed;

    /// <summary>
    /// The previous state of eyes closed. Used to ensure relevant audio / visual effects are only emitted once per change.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public bool PreviousEyelidPosition;

    /// <summary>
    /// Whether the eye closing was naturally created or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public bool NaturallyCreated;
}
