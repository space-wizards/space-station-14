using Content.Shared.Arcade.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArcadeSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ArcadeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultNewGameSounds = "ArcadeNewGame";

    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultWinSounds = "ArcadeWin";

    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultLoseSounds = "ArcadeLose";

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Player;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArcadeGameState State = ArcadeGameState.Idle;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? NewGameSound = new SoundCollectionSpecifier(DefaultNewGameSounds);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? WinSound = new SoundCollectionSpecifier(DefaultWinSounds);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? LoseSound = new SoundCollectionSpecifier(DefaultLoseSounds);
}
