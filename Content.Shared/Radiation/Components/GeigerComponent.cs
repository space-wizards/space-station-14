using Content.Shared.Radiation.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

/// <summary>
///     Geiger counter that shows current radiation level.
///     Can be added as a component to clothes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedGeigerSystem))]
public sealed partial class GeigerComponent : Component
{
    /// <summary>
    ///     If true it will be active only when player equipped it.
    /// </summary>
    [DataField]
    public bool AttachedToSuit;

    /// <summary>
    ///     Is geiger counter currently active?
    ///     If false attached entity will ignore any radiation rays.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsEnabled;

    /// <summary>
    ///     Should it shows examine message with current radiation level?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool ShowExamine;

    /// <summary>
    ///     Should it shows item control when equipped by player?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool ShowControl;

    /// <summary>
    ///     Map of sounds that should be play on loop for different radiation levels.
    /// </summary>
    [DataField]
    public Dictionary<GeigerDangerLevel, SoundSpecifier> Sounds = new()
    {
        {GeigerDangerLevel.Low, new SoundPathSpecifier("/Audio/Items/Geiger/low.ogg")},
        {GeigerDangerLevel.Med, new SoundPathSpecifier("/Audio/Items/Geiger/med.ogg")},
        {GeigerDangerLevel.High, new SoundPathSpecifier("/Audio/Items/Geiger/high.ogg")},
        {GeigerDangerLevel.Extreme, new SoundPathSpecifier("/Audio/Items/Geiger/ext.ogg")}
    };

    /// <summary>
    ///     Current radiation level in rad per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float CurrentRadiation;

    /// <summary>
    ///     Estimated radiation danger level.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public GeigerDangerLevel DangerLevel = GeigerDangerLevel.None;

    /// <summary>
    ///     Current player that equipped geiger counter.
    ///     Because sound is annoying, geiger counter clicks will play
    ///     only for player that equipped it.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    ///     Marked true if control needs to update UI with latest component state.
    /// </summary>
    [Access(typeof(SharedGeigerSystem), Other = AccessPermissions.ReadWrite)]
    public bool UiUpdateNeeded;

    /// <summary>
    ///     Current stream of geiger counter audio.
    ///     Played only for current user.
    /// </summary>
    public IPlayingAudioStream? Stream;
}

[Serializable, NetSerializable]
public enum GeigerDangerLevel : byte
{
    None,
    Low,
    Med,
    High,
    Extreme
}

[Serializable, NetSerializable]
public enum GeigerLayers : byte
{
    Screen
}

[Serializable, NetSerializable]
public enum GeigerVisuals : byte
{
    DangerLevel,
    IsEnabled
}
