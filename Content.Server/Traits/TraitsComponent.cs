using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

/// <summary>
/// Keeps track of all the TraitPrototypes that currently applied to an entity.
/// </summary>
[RegisterComponent]
public sealed partial class TraitsComponent : Component
{
    /// <summary>
    /// Traits applied to this entity.
    /// If a Trait gets reverted, it is removed from this list.
    /// </summary>
    [DataField]
    public HashSet<TraitStatus> AppliedTraits = new();
}

/// <summary>
/// Stores a trait and related metadata regarding the trait's status on the entity.
/// </summary>
[DataDefinition]
public partial record struct TraitStatus
{
    /// <summary>
    /// The prototype for the stored trait.
    /// </summary>
    [DataField]
    public ProtoId<TraitPrototype> Trait;

    /// <summary>
    /// If the trait is selected to be revertible.
    /// Used in the event some system should revert traits.
    /// </summary>
    [DataField]
    public bool Revertible;

    public TraitStatus(ProtoId<TraitPrototype> traitProtoId, bool revertible)
    {
        Trait = traitProtoId;
        Revertible = revertible;
    }
}
