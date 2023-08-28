using Content.Shared.Chemistry.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Materials;

/// <summary>
/// This is a machine that handles converting entities
/// into the raw materials and chemicals that make them up.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMaterialReclaimerSystem))]
public sealed partial class MaterialReclaimerComponent : Component
{
    /// <summary>
    /// Whether or not the machine has power. We put it here
    /// so we can network and predict it.
    /// </summary>
    [DataField("powered"), ViewVariables(VVAccess.ReadWrite)]
    public bool Powered;

    /// <summary>
    /// An "enable" toggle for things like interfacing with machine linking
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>
    /// How efficiently the materials are reclaimed.
    /// In practice, a multiplier per material when calculating the output of the reclaimer.
    /// </summary>
    [DataField("efficiency"), ViewVariables(VVAccess.ReadWrite)]
    public float Efficiency = 1f;

    /// <summary>
    /// Whether or not the process
    /// speed scales with the amount of materials being processed
    /// or if it's just <see cref="MinimumProcessDuration"/>
    /// </summary>
    [DataField("scaleProcessSpeed")]
    public bool ScaleProcessSpeed = true;

    /// <summary>
    /// How quickly it takes to consume X amount of materials per second.
    /// For example, with a rate of 50, an entity with 100 total material takes 2 seconds to process.
    /// </summary>
    [DataField("baseMaterialProcessRate"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseMaterialProcessRate = 100f;

    /// <summary>
    /// How quickly it takes to consume X amount of materials per second.
    /// For example, with a rate of 50, an entity with 100 total material takes 2 seconds to process.
    /// </summary>
    [DataField("materialProcessRate"), ViewVariables(VVAccess.ReadWrite)]
    public float MaterialProcessRate = 100f;

    /// <summary>
    /// Machine part whose rating modifies <see cref="MaterialProcessRate"/>
    /// </summary>
    [DataField("machinePartProcessRate", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string MachinePartProcessRate = "Manipulator";

    /// <summary>
    /// How much the machine part quality affects the <see cref="MaterialProcessRate"/>
    /// </summary>
    [DataField("partRatingProcessRateMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float PartRatingProcessRateMultiplier = 1.5f;

    /// <summary>
    /// The minimum amount fo time it can take to process an entity.
    /// this value supercedes the calculated one using <see cref="MaterialProcessRate"/>
    /// </summary>
    [DataField("minimumProcessDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinimumProcessDuration = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// The id of our output solution
    /// </summary>
    [DataField("solutionContainerId"), ViewVariables(VVAccess.ReadWrite)]
    public string SolutionContainerId = "output";

    /// <summary>
    /// The solution itself.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Solution OutputSolution = default!;

    /// <summary>
    /// a whitelist for what entities can be inserted into this reclaimer
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// a blacklist for what entities cannot be inserted into this reclaimer
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The sound played when something is being processed.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound;

    /// <summary>
    /// whether or not we cut off the sound early when the reclaiming ends.
    /// </summary>
    [DataField("cutOffSound")]
    public bool CutOffSound = true;

    /// <summary>
    /// When the next sound will be allowed to be played. Used to prevent spam.
    /// </summary>
    [DataField("nextSound", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSound;

    /// <summary>
    /// Minimum time inbetween each <see cref="Sound"/>
    /// </summary>
    [DataField("soundCooldown")]
    public TimeSpan SoundCooldown = TimeSpan.FromSeconds(0.8f);

    public IPlayingAudioStream? Stream;

    /// <summary>
    /// A counter of how many items have been processed
    /// </summary>
    /// <remarks>
    /// I saw this on the recycler and i'm porting it because it's cute af
    /// </remarks>
    [DataField("itemsProcessed")]
    public int ItemsProcessed;
}

[Serializable, NetSerializable]
public sealed class MaterialReclaimerComponentState : ComponentState
{
    public bool Powered;

    public bool Enabled;

    public float MaterialProcessRate;

    public int ItemsProcessed;

    public MaterialReclaimerComponentState(bool powered, bool enabled, float materialProcessRate, int itemsProcessed)
    {
        Powered = powered;
        Enabled = enabled;
        MaterialProcessRate = materialProcessRate;
        ItemsProcessed = itemsProcessed;
    }
}

[NetSerializable, Serializable]
public enum RecyclerVisuals
{
    Bloody
}

public enum RecyclerVisualLayers : byte
{
    Main,
    Bloody
}
