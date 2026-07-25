// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeadSpace.Sandevistan;

[RegisterComponent]
public sealed partial class SandevistanImplanterComponent : Component
{
    public DoAfterId? ActiveDoAfter;
    public TimeSpan NextScreamTime;
}

[RegisterComponent]
public sealed partial class SandevistanImplantComponent : Component
{
    [DataField]
    public float Duration = 25f;

    [DataField]
    public float SoftcapTime = 18f;

    [DataField]
    public float CooldownMultiplier = 1.5f;

    [DataField]
    public float MovementSpeedModifier = 1.7f;

    [DataField]
    public float AttackRateModifier = 1.75f;

    [DataField]
    public float OverloadInterval = 1f;

    [DataField]
    public DamageSpecifier OverloadDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", 5 },
            { "Piercing", 5 },
        },
    };

    public TimeSpan NextReadyTime = TimeSpan.Zero;

    [DataField]
    public float ExhaustionStaminaDamage = 100f;

    [DataField]
    public float SoftcapPopupInterval = 2f;

    [DataField]
    public float SoftcapPopupInitialDelay = 2f;

    [DataField]
    public float ShoutInitialDelay = 1.25f;

    [DataField]
    public float ShoutMinInterval = 3.5f;

    [DataField]
    public float ShoutMaxInterval = 4.5f;

    [DataField]
    public List<LocId> SoftcapPopups = new()
    {
        "sandevistan-softcap-popup-1",
        "sandevistan-softcap-popup-2",
        "sandevistan-softcap-popup-3",
        "sandevistan-softcap-popup-4",
        "sandevistan-softcap-popup-5",
        "sandevistan-softcap-popup-6",
        "sandevistan-softcap-popup-7",
        "sandevistan-softcap-popup-8",
        "sandevistan-softcap-popup-9",
        "sandevistan-softcap-popup-10",
        "sandevistan-softcap-popup-11",
    };

    [DataField]
    public List<LocId> SoftcapShouts = new()
    {
        "sandevistan-softcap-shout-1",
        "sandevistan-softcap-shout-2",
        "sandevistan-softcap-shout-3",
        "sandevistan-softcap-shout-4",
        "sandevistan-softcap-shout-5",
        "sandevistan-softcap-shout-6",
        "sandevistan-softcap-shout-7",
        "sandevistan-softcap-shout-8",
        "sandevistan-softcap-shout-9",
        "sandevistan-softcap-shout-10",
        "sandevistan-softcap-shout-11",
        "sandevistan-softcap-shout-12",
        "sandevistan-softcap-shout-13",
        "sandevistan-softcap-shout-14",
        "sandevistan-softcap-shout-15",
        "sandevistan-softcap-shout-16",
        "sandevistan-softcap-shout-17",
        "sandevistan-softcap-shout-18",
        "sandevistan-softcap-shout-19",
        "sandevistan-softcap-shout-20",
        "sandevistan-softcap-shout-21",
        "sandevistan-softcap-shout-22",
        "sandevistan-softcap-shout-23",
        "sandevistan-softcap-shout-24",
        "sandevistan-softcap-shout-25",
        "sandevistan-softcap-shout-26",
        "sandevistan-softcap-shout-27",
        "sandevistan-softcap-shout-28",
        "sandevistan-softcap-shout-29",
        "sandevistan-softcap-shout-30",
    };

    [DataField]
    public float EndWarningLeadTime = 5f;

    [DataField]
    public float EndWarningInterval = 2.5f;

    [DataField]
    public int EndWarningPopupCount = 1;

    [DataField]
    public List<LocId> EndWarningPopups = new()
    {
        "sandevistan-end-warning-popup-1",
        "sandevistan-end-warning-popup-2",
        "sandevistan-end-warning-popup-3",
        "sandevistan-end-warning-popup-4",
        "sandevistan-end-warning-popup-5",
        "sandevistan-end-warning-popup-6",
        "sandevistan-end-warning-popup-7",
        "sandevistan-end-warning-popup-8",
        "sandevistan-end-warning-popup-9",
        "sandevistan-end-warning-popup-10",
    };

    [DataField]
    public float RecoveryTickInterval = 3f;

    [DataField]
    public DamageSpecifier RecoveryDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Asphyxiation", 2 },
        },
    };

    [DataField]
    public float RecoveryJitterAmplitude = 3.5f;

    [DataField]
    public float RecoveryJitterFrequency = 18f;

    [DataField]
    public float RecoveryJitterRefreshTime = 0.35f;

    [DataField]
    public float RecoveryPopupInterval = 5f;

    [DataField]
    public List<LocId> RecoveryPopups = new()
    {
        "sandevistan-recovery-popup-1",
        "sandevistan-recovery-popup-2",
        "sandevistan-recovery-popup-3",
    };

    [DataField]
    public float InitialJitterProgress = 0.2f;

    [DataField]
    public int MaxJitterHits = 5;

    [DataField]
    public float MaxJitterAmplitude = 5f;

    [DataField]
    public float MaxJitterFrequency = 30f;

    [DataField]
    public float JitterLerpRate = 5f;

    [DataField]
    public float JitterRefreshTime = 0.35f;

    [DataField]
    public float AfterimageInterval = 0.01f;

    [DataField]
    public float AfterimageMinDistance = 1f;

    [DataField]
    public float AfterimageLifetime = 1.25f;

    [DataField]
    public float DeactivationVisualDuration = 1.5f;

    [DataField]
    public Color AfterimageColor = Color.FromHex("#00ffd0dd");

    [DataField]
    public string AfterimageFallbackEffect = "MantisDodgeEffect";

    [DataField]
    public List<SoundSpecifier> ActivationSounds = new()
    {
        new SoundPathSpecifier("/Audio/_DeadSpace/Sandevistan/sandevistan_activate_1.ogg"),
    };

    [DataField]
    public List<float> ActivationSoundDurations = new()
    {
        2.9f,
    };

    [DataField]
    public float WorkingSoundDelay = 2.9f;

    [DataField]
    public SoundSpecifier WorkingSound = new SoundPathSpecifier(
        "/Audio/_DeadSpace/Sandevistan/sandevistan_working.ogg",
        AudioParams.Default.WithLoop(true));

    [DataField]
    public SoundSpecifier DeactivationSound = new SoundPathSpecifier("/Audio/_DeadSpace/Sandevistan/sandevistan_off.ogg");

    [DataField]
    public LocId? Popup = "sandevistan-implant-activated";
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanMeleeAttackRateComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Modifier = 1f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ActiveSandevistanComponent : Component
{
    [DataField]
    public EntityUid? SourceImplant;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SoftcapTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextOverloadTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSoftcapPopupTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextShoutTime;

    [DataField]
    public float CooldownMultiplier = 1.5f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan StartTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan WorkingSoundStartTime;

    [DataField]
    public SoundSpecifier WorkingSound = new SoundPathSpecifier(
        "/Audio/_DeadSpace/Sandevistan/sandevistan_working.ogg",
        AudioParams.Default.WithLoop(true));

    [DataField]
    public SoundSpecifier DeactivationSound = new SoundPathSpecifier("/Audio/_DeadSpace/Sandevistan/sandevistan_off.ogg");

    [DataField]
    public bool WorkingSoundStarted;

    public EntityUid? WorkingSoundStream;

    [DataField]
    public float SoftcapPopupInterval = 2f;

    [DataField]
    public float ShoutMinInterval = 3.5f;

    [DataField]
    public float ShoutMaxInterval = 4.5f;

    [DataField]
    public List<LocId> SoftcapPopups = new();

    [DataField]
    public List<LocId> SoftcapShouts = new();

    public int LastSoftcapPopupIndex = -1;
    public int LastSoftcapShoutIndex = -1;
    public readonly List<int> SoftcapPopupBag = new();
    public readonly List<int> SoftcapShoutBag = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEndWarningTime;

    [DataField]
    public float EndWarningLeadTime = 5f;

    [DataField]
    public float EndWarningInterval = 2.5f;

    [DataField]
    public int EndWarningPopupCount = 1;

    [DataField]
    public List<LocId> EndWarningPopups = new();

    public int LastEndWarningPopupIndex = -1;
    public readonly List<int> EndWarningPopupBag = new();

    [DataField, AutoNetworkedField]
    public float MovementSpeedModifier = 1.7f;

    [DataField, AutoNetworkedField]
    public float AttackRateModifier = 1.75f;

    [DataField]
    public float OverloadInterval = 1f;

    [DataField]
    public DamageSpecifier OverloadDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", 5 },
            { "Piercing", 5 },
        },
    };

    [DataField]
    public float ExhaustionStaminaDamage = 100f;

    [DataField]
    public bool ManualStopRequested;

    [DataField]
    public float ManualStopVisualIntensity = -1f;

    [DataField]
    public float JitterCurrentProgress;

    [DataField]
    public float JitterTargetProgress = 0.2f;

    [DataField]
    public float InitialJitterProgress = 0.2f;

    [DataField]
    public int JitterHits;

    [DataField]
    public int MaxJitterHits = 5;

    [DataField]
    public float MaxJitterAmplitude = 5f;

    [DataField]
    public float MaxJitterFrequency = 30f;

    [DataField]
    public float JitterLerpRate = 5f;

    [DataField]
    public float JitterRefreshTime = 0.35f;

    [DataField, AutoNetworkedField]
    public float AfterimageInterval = 0.01f;

    [DataField, AutoNetworkedField]
    public float AfterimageMinDistance = 1f;

    [DataField, AutoNetworkedField]
    public float AfterimageLifetime = 1.25f;

    [DataField, AutoNetworkedField]
    public float DeactivationVisualDuration = 1.5f;

    [DataField, AutoNetworkedField]
    public Color AfterimageColor = Color.FromHex("#00ffd0dd");

    [DataField, AutoNetworkedField]
    public string AfterimageFallbackEffect = "MantisDodgeEffect";

    [DataField]
    public float RecoveryTickInterval = 3f;

    [DataField]
    public DamageSpecifier RecoveryDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Asphyxiation", 2 },
        },
    };

    [DataField]
    public float RecoveryJitterAmplitude = 3.5f;

    [DataField]
    public float RecoveryJitterFrequency = 18f;

    [DataField]
    public float RecoveryJitterRefreshTime = 0.35f;

    [DataField]
    public float RecoveryPopupInterval = 5f;

    [DataField]
    public List<LocId> RecoveryPopups = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class SandevistanRecoveryComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTickTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPopupTime;

    [DataField, AutoNetworkedField]
    public float Duration;

    [DataField]
    public float TickInterval = 3f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Asphyxiation", 2 },
        },
    };

    [DataField]
    public float JitterAmplitude = 3.5f;

    [DataField]
    public float JitterFrequency = 18f;

    [DataField]
    public float JitterRefreshTime = 0.35f;

    [DataField]
    public float PopupInterval = 5f;

    [DataField]
    public List<LocId> Popups = new();

    public int LastPopupIndex = -1;
    public readonly List<int> PopupBag = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class SandevistanVisualFadeoutComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public float Duration = 1.5f;

    [DataField, AutoNetworkedField]
    public float StartIntensity = 1f;

    [DataField, AutoNetworkedField]
    public bool AllowRampIn;

    [DataField, AutoNetworkedField]
    public float SoftcapProgress;

    [DataField, AutoNetworkedField]
    public float AfterimageInterval = 0.01f;

    [DataField, AutoNetworkedField]
    public float AfterimageMinDistance = 1f;

    [DataField, AutoNetworkedField]
    public float AfterimageLifetime = 1.25f;

    [DataField, AutoNetworkedField]
    public Color AfterimageColor = Color.FromHex("#00ffd0dd");

    [DataField, AutoNetworkedField]
    public string AfterimageFallbackEffect = "MantisDodgeEffect";
}
