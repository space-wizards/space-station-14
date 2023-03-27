using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.Mech.Equipment.Components;

/// <summary>
/// A piece of mech equipment that drills entities and stores them
/// inside of a container so large objects can be moved.
/// </summary>
[RegisterComponent]
public sealed class MechDrillComponent : Component
{
    /// <summary>
    /// The change in energy after each drill.
    /// </summary>
    [DataField("drillEnergyDelta")]
    public float DrillEnergyDelta = -30;
    /// <summary>
    /// How long does it take to drill something?
    /// </summary>
    [DataField("drillDelay")]
    public float DrillDelay = 0.25f;
    /// <summary>
    /// The sound played when a mech is drilling something
    /// </summary>
    [DataField("drillSound")]
    public SoundSpecifier DrillSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");
    public IPlayingAudioStream? AudioStream;
}
