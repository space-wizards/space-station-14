using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Verbs.Prototypes;

/// <summary>
/// Defines a target-filtered admin verb.
/// </summary>
[Prototype]
public sealed partial class AdminVerbPrototype : IPrototype
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

    [DataField]
    public EntityEffect[] Effects { get; private set; } = [];
}
