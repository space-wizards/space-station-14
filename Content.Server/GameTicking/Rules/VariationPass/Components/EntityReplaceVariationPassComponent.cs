using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This is used for replacing a certain amount of entities with other entities in a variation pass.
///
/// </summary>
/// <remarks>
/// POTENTIALLY REPLACEABLE ENTITIES MUST BE MARKED WITH A REPLACEMENT MARKER
/// AND HAVE A SYSTEM INHERITING FROM <see cref="BaseEntityReplaceVariationPassSystem{TEntComp,TGameRuleComp}"/>
/// SEE <see cref="WallReplaceVariationPassSystem"/>
/// </remarks>
[RegisterComponent]
public sealed partial class EntityReplaceVariationPassComponent : Component
{
    /// <summary>
    ///     Number of matching entities before one will be replaced on average.
    /// </summary>
    [DataField(required: true)]
    public float EntitiesPerReplacementAverage;

    [DataField(required: true)]
    public float EntitiesPerReplacementStdDev;

    /// <summary>
    ///     Prototype(s) to replace matched entities with.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Replacements = default!;
}
