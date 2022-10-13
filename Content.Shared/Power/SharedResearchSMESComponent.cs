using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class ResearchSMESBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool  HasPower; //tf
    public readonly float ChargeStored; //units -analysedCharge
    public readonly bool  ShieldingActive; //tf
    public readonly float ShieldingCost; //units
    public readonly float AnalysisChargeSiphon; //%
    public readonly bool  MaxCapReached; //tf - the battery cannot get any bigger if true
    public readonly float OverloadThreshold; //units
    public readonly bool  ResearchMode; //tf
    public readonly float LastIncrease; //units
    public readonly float LastDischarge; //units
    public readonly float AnalysisCap; //units
    public readonly bool ResearchComplete; //units
    public readonly float SmesCap; //units

    public ResearchSMESBoundUserInterfaceState(bool hasPower, float chargeStored, bool shieldingActive, float shieldingCost, float analysisChargeSiphon, bool maxCapReached, float overloadThreshold, bool researchMode, float lastIncrease, float analysisCap, float lastDischarge , bool researchComplete, float smesCap)
    {
        HasPower = hasPower;
        ChargeStored = chargeStored;
        ShieldingActive = shieldingActive;
        ShieldingCost = shieldingCost;
        AnalysisChargeSiphon = analysisChargeSiphon;
        MaxCapReached = maxCapReached;
        OverloadThreshold = overloadThreshold;
        ResearchMode = researchMode;
        LastIncrease = lastIncrease;
        AnalysisCap = analysisCap;
        LastDischarge = lastDischarge;
        ResearchComplete = researchComplete;
        SmesCap = smesCap;
    }
}

[Serializable, NetSerializable]
public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly UiButton Button;

    public UiButtonPressedMessage(UiButton button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public enum ResearchSMESUiKey
{
    Key
}

public enum UiButton
{
    ToggleResearchMode,
    ToggleShield,
    IncreaseSiphon,
    DecreaseSiphon,
}

