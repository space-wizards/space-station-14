using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

[RegisterComponent]
public sealed partial class ReactiveComponent : Component
{
    /// <summary>
    ///     A dictionary of reactive groups -> methods that work on them.
    /// </summary>
    [DataField("groups")]
    public Dictionary<ProtoId<ReactiveGroupPrototype>, HashSet<ReactionMethod>>? ReactiveGroups;

    /// <summary>
    ///     Special reactions that this prototype can specify, outside of any that reagents already apply.
    ///     Useful for things like monkey cubes, which have a really prototype-specific effect.
    /// </summary>
    [DataField]
    public List<ReactiveReagentEffectEntry>? Reactions;
}

[DataDefinition]
public sealed partial class ReactiveReagentEffectEntry
{
    [DataField]
    public HashSet<ReactionMethod> Methods = default!;

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>>? Reagents;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField("groups")]
    public Dictionary<ProtoId<ReactiveGroupPrototype>, HashSet<ReactionMethod>>? ReactiveGroups { get; private set; }
}
