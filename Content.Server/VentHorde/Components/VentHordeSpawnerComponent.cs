using Content.Server.VentHorde.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.VentHorde.Components;

/// <summary>
/// Marks an entity as selected by the <see cref="VentHordeRuleComponent"/>.
/// Will spawn all entities contained within <see cref="Entities"/> on its location at <see cref="SpawnTime"/>.
/// </summary>
[RegisterComponent, Access(typeof(VentHordeSystem))]
[AutoGenerateComponentPause]
public sealed partial class VentHordeSpawnerComponent : Component
{
    /// <summary>
    /// The mobs to spawn from the vent.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Entities = new ();

    /// <summary>
    /// Maximum speed at which the entities will be thrown out of the vent.
    /// </summary>
    [DataField]
    public float MaxThrowSpeed = 1.5f;

    /// <summary>
    /// Minimum speed at which the entities will be thrown out of the vent.
    /// </summary>
    [DataField]
    public float MinThrowSpeed = 0.5f;

    /// <summary>
    /// Maximum distance which travel when thrown out of the vent.
    /// </summary>
    [DataField]
    public float MaxThrowDistance = 4f;

    /// <summary>
    /// Minimum distance which travel when thrown out of the vent.
    /// </summary>
    [DataField]
    public float MinThrowDistance = 2f;

    /// <summary>
    /// The time at which the entities will spawn.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? SpawnTime;

    /// <summary>
    /// Plays on loop when a vent is selected as a spawner.
    /// </summary>
    [DataField]
    public SoundSpecifier PassiveSound = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Plays when the entities are thrown out of the vent.
    /// </summary>
    [DataField]
    public SoundSpecifier EndSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/grenade_launcher.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// The PassiveSound entity, used to cancel the audio.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? AudioStream;
}
