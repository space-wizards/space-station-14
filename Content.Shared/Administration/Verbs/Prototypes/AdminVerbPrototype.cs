using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Verbs.Prototypes;

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
    /// Operations are executed synchronously in the order they are listed.
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public AdminOperation[] Operations { get; private set; } = [];
}
