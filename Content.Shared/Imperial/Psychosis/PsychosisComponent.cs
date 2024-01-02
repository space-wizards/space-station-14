using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for Psychosis, which causes auditory hallucinations.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPsychosisSystem))]
public sealed partial class PsychosisComponent : Component
{
    /// <summary>
    /// The maximum time between incidents in seconds
    /// </summary>
    [DataField("maxTimeBetweenSounds", required: true), ViewVariables(VVAccess.ReadWrite)]

    public float MaxTimeBetweenSounds = 30f;

    /// <summary>
    /// The minimum time between incidents in seconds
    /// </summary>
    [DataField("minTimeBetweenSounds", required: true), ViewVariables(VVAccess.ReadWrite)]

    public float MinTimeBetweenSounds = 20f;

    [DataField("maxTimeBetweenItems", required: true), ViewVariables(VVAccess.ReadWrite)]

    public float MaxTimeBetweenItems = 40f;

    [DataField("minTimeBetweenItems", required: true), ViewVariables(VVAccess.ReadWrite)]

    public float MinTimeBetweenItems = 30f;

    [DataField("maxSoundDistance", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxDistance = 10f;

    [DataField("maxItemDistance", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxItemDistance = 15f;
    [DataField("nextIncrease"), ViewVariables(VVAccess.ReadWrite)]

    public TimeSpan NextIncrease = TimeSpan.Zero;
    [DataField("IncreaseTime"), ViewVariables(VVAccess.ReadWrite)]

    public TimeSpan IncreaseTime = TimeSpan.FromSeconds(22.5);
    [DataField("increase"), ViewVariables(VVAccess.ReadWrite)]

    public float Increase = 5f;
    [DataField("current"), ViewVariables(VVAccess.ReadWrite)]

    public float Current = 0f;
    [DataField("maxstage"), ViewVariables(VVAccess.ReadWrite)]

    public int Maxstage = 3;
    [DataField("firstWayToHeal")]
    public string HealFirst = "";
    [DataField("secondWayToHeal")]
    public string HealSecond = "";
    [DataField("thirdWayToHeal")]
    public string HealThird = "";

    /// <summary>
    /// The sounds to choose from
    /// </summary>
    [DataField("sounds", required: true)]
    public SoundSpecifier Sounds = new SoundPathSpecifier("/Audio/Effects/singularity.ogg");

    [DataField("NextItemTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]

    public TimeSpan NextItemTime = TimeSpan.Zero;
    [DataField("NextSoundTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]

    public TimeSpan NextSoundTime = TimeSpan.Zero;

    [DataField("stage"), ViewVariables(VVAccess.ReadWrite)]

    public int Stage = 1;
    [DataField("SoundsTable"), ViewVariables(VVAccess.ReadWrite)]

    public List<SoundSpecifier> SoundsTable = new();
    [DataField("ItemsTable"), ViewVariables(VVAccess.ReadWrite)]

    public List<string> ItemsTable = new();

    [DataField("CreatureTable"), ViewVariables(VVAccess.ReadWrite)]

    public List<string> CreatureTable = new();
    public float ChanceForCreature = 0.3f;

    public string PopUp = string.Empty;

    public IPlayingAudioStream? Stream;
}
