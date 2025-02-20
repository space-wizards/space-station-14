using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Drug.Components;

[RegisterComponent]
public sealed partial class InstantDrugAddicationComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DependencyLevel = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AddictionLevel = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Tolerance = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WithdrawalLevel = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EffectStrengthModify = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WithdrawalRate = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxWithdrawalLvl = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float StaminaMultiply = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeLastAppointment = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float SomeThresholdTime = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float StandartTemperature = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("updateDuration", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan UpdateDuration = TimeSpan.FromSeconds(1);

    [DataField("timeUtilUpdate", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilUpdate = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("sendMessageDuration", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan SendMessageDuration = TimeSpan.FromSeconds(5);

    [DataField("timeUtilSendMessage", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilSendMessage = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("changeAddictionDuration", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ChangeAddictionDuration = TimeSpan.FromSeconds(30);

    [DataField("timeUtilChangeAddiction", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilChangeAddiction = TimeSpan.Zero;

    [DataField("durationOfActionWeakDrug", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationOfActionWeakDrug = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsTimeSendMessage = true;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsTakeWeakDrug = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsStaminaEdit = false;

    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Asphyxiation", 2.5 }
        }
    };

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundHighEffect = new SoundPathSpecifier("/Audio/DeadSpace/Drug/high_effect.ogg");

}
