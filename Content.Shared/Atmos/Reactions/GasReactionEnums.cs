namespace Content.Shared.Atmos.Reactions;

[Flags]
public enum ReactionResult : byte
{
    NoReaction = 0,
    Reacting = 1,
    StopReactions = 2,
}

public enum GasReaction : byte
{
    Fire = 0,
}

/// <summary>
///     Enum that corresponds to an entity that a GasMixture may hold.
/// </summary>
public enum GasReactionEntity : byte
{
    TritiumRadiation = 0,
}
