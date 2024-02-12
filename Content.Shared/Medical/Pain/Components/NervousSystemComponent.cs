using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NervousSystemComponent : Component
{

    [DataField, AutoNetworkedField]
    public FixedPoint2 RawPain = FixedPoint2.Zero;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 1f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 UnConsciousThreshold = 60;

    public FixedPoint2 UnConsciousThresholdPain => NominalMaxPain * UnConsciousThreshold / 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ShockThreshold = 70;

    public FixedPoint2 ShockThresholdPain => NominalMaxPain * UnConsciousThreshold / 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 HeartAttackThreshold = 90;

    public FixedPoint2 HeartAttackThresholdPain => NominalMaxPain * UnConsciousThreshold / 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 NominalMaxPain = 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MitigatedPercentage = 0;

    public FixedPoint2 Pain => RawPain * Multiplier - RawPain * MitigatedPercentage/100;

    [DataField, AutoNetworkedField]
    public PainEffect AppliedEffects = PainEffect.None;

}

[Flags]
public enum PainEffect
{
    None = 0,
    UnConscious = 1<<0,
    Shock = 1<<0,
    HeartAttack = 1<<0,
}
