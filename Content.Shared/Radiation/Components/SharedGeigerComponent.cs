using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

[NetworkedComponent]
public abstract class SharedGeigerComponent : Component
{
    [DataField("showExamine")]
    public bool ShowExamine = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentRadiation;

    [ViewVariables(VVAccess.ReadOnly)]
    public GeigerDangerLevel DangerLevel = GeigerDangerLevel.None;

    [DataField("sounds")]
    public Dictionary<GeigerDangerLevel, SoundSpecifier> Sounds = new()
    {
        {GeigerDangerLevel.Low, new SoundPathSpecifier("/Audio/Items/Geiger/low.ogg")},
        {GeigerDangerLevel.Med, new SoundPathSpecifier("/Audio/Items/Geiger/med.ogg")},
        {GeigerDangerLevel.High, new SoundPathSpecifier("/Audio/Items/Geiger/high.ogg")},
        {GeigerDangerLevel.Extreme, new SoundPathSpecifier("/Audio/Items/Geiger/ext.ogg")}
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User;

    public IPlayingAudioStream? Stream;

}

[Serializable, NetSerializable]
public sealed class GeigerComponentState : ComponentState
{
    public float CurrentRadiation;
    public GeigerDangerLevel DangerLevel;
    public EntityUid? Equipee;
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
    Base,
    Screen
}

[Serializable, NetSerializable]
public enum GeigerVisuals : byte
{
    DangerLevel
}
