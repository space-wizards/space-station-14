using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

/// <summary>
/// This handles the chemistry guidebook and caching it.
/// </summary>
public abstract class SharedChemistryGuideDataSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    protected readonly Dictionary<string, ReagentGuideEntry> ReagentRegistry = new();

    public IReadOnlyDictionary<string, ReagentGuideEntry> ReagentGuideRegistry => ReagentRegistry;

    protected readonly Dictionary<string, Dictionary<string, uint>> ReactionRegistry = new();

    public IReadOnlyDictionary<string, Dictionary<string, uint>> ReactionGuideRegistry => ReactionRegistry;
}

[Serializable, NetSerializable]
public sealed class ReagentGuideRegistryChangedEvent : EntityEventArgs
{
    public ReagentGuideChangeset Changeset;

    public ReagentGuideRegistryChangedEvent(ReagentGuideChangeset changeset)
    {
        Changeset = changeset;
    }
}

[Serializable, NetSerializable]
public sealed class ReagentGuideChangeset
{
    public Dictionary<string, ReagentGuideEntry> ReagentEffectEntries;

    public HashSet<string> ReagentEffectRemoved;

    public Dictionary<string, Dictionary<string, uint>> ReactionSolidProductEntries;

    public HashSet<string> ReactionSolidProductRemoved;

    public ReagentGuideChangeset(Dictionary<string, ReagentGuideEntry> reagentEffects, HashSet<string> reagentEffectsRemoved, Dictionary<string, Dictionary<string, uint>> reactionSolidProducts, HashSet<string> reactionSolidProductsRemoved)
    {
        ReagentEffectEntries = reagentEffects;
        ReagentEffectRemoved = reagentEffectsRemoved;
        ReactionSolidProductEntries = reactionSolidProducts;
        ReactionSolidProductRemoved = reactionSolidProductsRemoved;
    }
}
