using Content.Shared.Arcade.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArcadeSystem))]
[AutoGenerateComponentState]
public sealed partial class ArcadeEmitSoundOnWinComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSounds = "ArcadeWin";

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier(DefaultSounds);
}
