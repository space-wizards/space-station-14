using Content.Shared.Radiation.Components;
using Robust.Shared.Audio;

namespace Content.Server.Radiation.Components;

/// <inheritdoc/>
[RegisterComponent]
[ComponentReference(typeof(SharedGeigerComponent))]
public sealed class GeigerComponent : SharedGeigerComponent
{
    /// <summary>
    ///     Map of sounds that should be play on loop for different radiation levels.
    /// </summary>
    [DataField("sounds")]
    public Dictionary<GeigerDangerLevel, SoundSpecifier> Sounds = new()
    {
        {GeigerDangerLevel.Low, new SoundPathSpecifier("/Audio/Items/Geiger/low.ogg")},
        {GeigerDangerLevel.Med, new SoundPathSpecifier("/Audio/Items/Geiger/med.ogg")},
        {GeigerDangerLevel.High, new SoundPathSpecifier("/Audio/Items/Geiger/high.ogg")},
        {GeigerDangerLevel.Extreme, new SoundPathSpecifier("/Audio/Items/Geiger/ext.ogg")}
    };

    /// <summary>
    ///     If true it will be active only when player equipped it.
    /// </summary>
    [DataField("attachedToSuit")]
    public bool AttachedToSuit;

    /// <summary>
    ///     Current player that equipped geiger counter.
    ///     Because sound is annoying, geiger counter clicks will play
    ///     only for player that equipped it.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User;

    /// <summary>
    ///     Current stream of geiger counter audio.
    ///     Played only for current user.
    /// </summary>
    public IPlayingAudioStream? Stream;
}
