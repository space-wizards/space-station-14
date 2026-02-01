using Content.Shared.Dragon;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Collections.Generic;

namespace Content.Server.Dragon;

[RegisterComponent]
public sealed partial class DragonRiftComponent : SharedDragonRiftComponent
{
    /// <summary>
    /// Dragon that spawned this rift.
    /// </summary>
    [DataField]
    public EntityUid? Dragon;

    /// <summary>
    /// How long the rift has been active.
    /// </summary>
    [DataField]
    public float Accumulator = 0f;

    /// <summary>
    /// The maximum amount we can accumulate before becoming impervious.
    /// </summary>
    [DataField] public float MaxAccumulator = 300f;

    /// <summary>
    /// Announcement sound of partly charging rift
    /// </summary>
    [DataField]
    public SoundSpecifier? PartlyChargingAnnouncementSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");


    /// <summary>
    /// Announcement sound of fully charging rift
    /// </summary>
    [DataField]
    public SoundSpecifier? FullChargingAnnouncementSound = new SoundPathSpecifier("/Audio/Announcements/attention.ogg");

    /// <summary>
    /// Announcement of fully charging rift
    /// </summary>
    [DataField]
    public LocId FullChargingAnnouncement = "carp-rift-max-warning";

    /// <summary>
    /// Accumulation of the spawn timer.
    /// </summary>
    [DataField]
    public float SpawnAccumulator = 30f;

    /// <summary>
    /// How long it takes for a new spawn to be added.
    /// </summary>
    [DataField]
    public float SpawnCooldown = 30f;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnPrototype = "MobCarpDragon";
}
