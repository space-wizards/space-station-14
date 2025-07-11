using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

/// <summary>
/// Keeps track of all the TraitPrototypes that were applied to an entity.
/// If a Trait gets reverted, it is removed from the list.
/// </summary>
[RegisterComponent]
public sealed partial class TraitsComponent : Component
{
    /// <summary>
    /// Traits applied to this entity.
    /// </summary>
    [DataField]
    public HashSet<TraitStatus> AppliedTraits = new();
}

[Serializable]
public struct TraitStatus
{
    [DataField]
    public ProtoId<TraitPrototype> Trait;

    [DataField]
    public bool Revertable;

    public TraitStatus(ProtoId<TraitPrototype> traitProtoId, bool revertable)
    {
        Trait = traitProtoId;
        Revertable = revertable;
    }
}
