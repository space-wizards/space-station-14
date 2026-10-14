using Robust.Shared.Serialization;

namespace Content.Shared.Genetics.GeneticsConsole;

[Serializable, NetSerializable]
public sealed class GeneticsConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool ScannerConnected;
    public readonly bool ScannerInRange;
    public readonly string? SubjectName;
    public readonly int Instability;
    public readonly int Health;
    public readonly int MaxHealth;
    public readonly int GeneticDamage;
    public readonly int MaxGeneticDamage;
    public readonly bool CanHumanizeSubject;
    public readonly bool IsHumanoidSubject;
    public readonly string? ResultMessage;
    public readonly List<GeneticsMutationUiEntry> Mutations;
    public readonly List<GeneticsMemoryUiEntry> Memory;
    public readonly int MaxMemorySlots;
    public readonly int JokerCooldown;

    public GeneticsConsoleBoundUserInterfaceState(
        bool scannerConnected,
        bool scannerInRange,
        string? subjectName,
        int instability,
        int health,
        int maxHealth,
        int geneticDamage,
        int maxGeneticDamage,
        bool canHumanizeSubject,
        bool isHumanoidSubject,
        string? resultMessage,
        List<GeneticsMutationUiEntry> mutations,
        List<GeneticsMemoryUiEntry> memory,
        int maxMemorySlots,
        int jokerCooldown = 0)
    {
        ScannerConnected = scannerConnected;
        ScannerInRange = scannerInRange;
        SubjectName = subjectName;
        Instability = instability;
        Health = health;
        MaxHealth = maxHealth;
        GeneticDamage = geneticDamage;
        MaxGeneticDamage = maxGeneticDamage;
        CanHumanizeSubject = canHumanizeSubject;
        IsHumanoidSubject = isHumanoidSubject;
        ResultMessage = resultMessage;
        Mutations = mutations;
        Memory = memory;
        MaxMemorySlots = maxMemorySlots;
        JokerCooldown = jokerCooldown;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsMutationUiEntry
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Description;
    public readonly string Sequence;
    public readonly string CandidateSequence;
    public readonly string CandidateComplementSequence;
    public readonly int SequenceProgress;
    public readonly bool Discovered;
    public readonly bool Active;
    public readonly int Instability;
    public readonly HashSet<int> KnownPositions;
    public readonly HashSet<int> HalfKnownPositions;

    public GeneticsMutationUiEntry(
        string id,
        string name,
        string description,
        string sequence,
        string candidateSequence,
        string candidateComplementSequence,
        int sequenceProgress,
        bool discovered,
        bool active,
        int instability,
        HashSet<int>? knownPositions = null,
        HashSet<int>? halfKnownPositions = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Sequence = sequence;
        CandidateSequence = candidateSequence;
        CandidateComplementSequence = candidateComplementSequence;
        SequenceProgress = sequenceProgress;
        Discovered = discovered;
        Active = active;
        Instability = instability;
        KnownPositions = knownPositions ?? new HashSet<int>();
        HalfKnownPositions = halfKnownPositions ?? new HashSet<int>();
    }
}

[Serializable, NetSerializable]
public sealed class GeneticsMemoryUiEntry
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Description;
    public readonly int Instability;

    public GeneticsMemoryUiEntry(string id, string name, string description, int instability)
    {
        Id = id;
        Name = name;
        Description = description;
        Instability = instability;
    }
}

[Serializable, NetSerializable]
public enum GeneticsConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum GeneticsConsoleAction : byte
{
    SetSequence,
    GuessSequence,
    Apply,
    SaveToMemory,
    CreateInjector,
    HumanizeMonkey,
    Analyze,
    ScrambleDna
}

[Serializable, NetSerializable]
public sealed class GeneticsConsoleMessage : BoundUserInterfaceMessage
{
    public readonly GeneticsConsoleAction Action;
    public readonly string MutationId;
    public readonly string Sequence;
    public readonly string ComplementSequence;

    public GeneticsConsoleMessage(
        GeneticsConsoleAction action,
        string mutationId,
        string sequence = "",
        string complementSequence = "")
    {
        Action = action;
        MutationId = mutationId;
        Sequence = sequence;
        ComplementSequence = complementSequence;
    }
}
