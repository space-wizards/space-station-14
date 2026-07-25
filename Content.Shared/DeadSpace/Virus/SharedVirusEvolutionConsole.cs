// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Body.Prototypes;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Virus;

[Serializable, NetSerializable]
public sealed class VirusEvolutionConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public int MutationPoints { get; }
    public int MultiPriceDeleteSymptom { get; }
    public bool DataServerConnected { get; }
    public bool SolutionAnalyzerConnected { get; }
    public bool DataServerInRange { get; }
    public bool SolutionAnalyzerInRange { get; }
    public VirusSolutionAnalyzerStatus SolutionAnalyzerStatus { get; }
    public bool HasVirus { get; }
    public List<ProtoId<VirusSymptomPrototype>> ActiveSymptoms = new();
    public List<ProtoId<BodyPrototype>> BodyWhitelist = new();

    // Статистика вируса
    public bool IsSentientVirus { get; }
    public float MaxThreshold { get; }
    public float Infectivity { get; }
    public int InfectedCount { get; }
    public int PointsPerSecond { get; }
    public bool IsTaipan { get; }

    public VirusEvolutionConsoleBoundUserInterfaceState(
        int mutationPoints,
        int multyPriceDeleteSymptom,
        bool dataServerConnected,
        bool solutionAnalyzerConnected,
        bool dataServerInRange,
        bool solutionAnalyzerInRange,
        bool hasVirus = false,
        List<ProtoId<VirusSymptomPrototype>>? activeSymptoms = null,
        List<ProtoId<BodyPrototype>>? bodyWhitelist = null,
        float maxThreshold = 100f,
        float infectivity = 0f,
        int infectedCount = 0,
        int pointsPerSecond = 0,
        bool isSentientVirus = false,
        VirusSolutionAnalyzerStatus solutionAnalyzerStatus = VirusSolutionAnalyzerStatus.Off,
        bool isTaipan = false)
    {
        MutationPoints = mutationPoints;
        MultiPriceDeleteSymptom = multyPriceDeleteSymptom;
        DataServerConnected = dataServerConnected;
        SolutionAnalyzerConnected = solutionAnalyzerConnected;
        DataServerInRange = dataServerInRange;
        SolutionAnalyzerInRange = solutionAnalyzerInRange;
        SolutionAnalyzerStatus = solutionAnalyzerStatus;
        ActiveSymptoms = activeSymptoms ?? new List<ProtoId<VirusSymptomPrototype>>();
        BodyWhitelist = bodyWhitelist ?? new List<ProtoId<BodyPrototype>>();
        IsSentientVirus = isSentientVirus;
        HasVirus = hasVirus;
        MaxThreshold = maxThreshold;
        Infectivity = infectivity;
        InfectedCount = infectedCount;
        PointsPerSecond = pointsPerSecond;
        IsSentientVirus = isSentientVirus;
        IsTaipan = isTaipan;
    }
}


[Serializable, NetSerializable]
public sealed class EvolutionConsoleUiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly EvolutionConsoleUiButton Button;
    public string? Symptom { get; } = null;
    public string? Body { get; } = null;

    public EvolutionConsoleUiButtonPressedMessage(
        EvolutionConsoleUiButton button,
        string? symptom = null,
        string? body = null
        )
    {
        Button = button;
        Symptom = symptom;
        Body = body;
    }
}


[Serializable, NetSerializable]
public enum EvolutionConsoleUiButton : byte
{
    EvolutionSymptom,
    EvolutionBody,
    DeleteSymptom,
    DeleteBody
}

[Serializable, NetSerializable]
public enum VirusEvolutionConsoleUiKey : byte
{
    Key
}
