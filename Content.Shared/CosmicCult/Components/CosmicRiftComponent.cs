using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CosmicRiftComponent : Component
{
    [DataField]
    public bool Occupied;

    [DataField]
    public TimeSpan AbsorbTime = TimeSpan.FromSeconds(25);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))] [AutoPausedField]
    public TimeSpan? HitTimer;

    [DataField]
    public int MaxHits = 7;

    [DataField]
    public int CurrentHits;

    /// <summary>
    /// The probability of the text PopUp when hit by a Lambda Particle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TextChance = 0.55f;

    /// <summary>
    /// A dataset of possible flavor text that can Pop Up when purging a Rift.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LocalizedDatasetPrototype> PopUpDataset = "DatasetCosmicRiftPopups";

    [DataField]
    public SoundSpecifier ExpungeSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser_cannon2.ogg");
}
