using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Verbs.Prototypes;

/// <summary>
/// Defines a target-filtered admin verb as an ordered sequence of operations.
/// </summary>
[DataDefinition]
public abstract partial class AdminVerbPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField]
    public LocId? Description { get; private set; }

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    [DataField]
    public EntityWhitelist? Whitelist { get; private set; }

    [DataField]
    public EntityWhitelist? Blacklist { get; private set; }

    /// <summary>
    /// Executed synchronously in the listed order, so later operations can rely on earlier ones.
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public AdminOperation[] Operations { get; private set; } = [];
}
