using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed partial class DamageOnToolInteractComponent : Component
{
    [DataField]
    public ProtoId<ToolQualityPrototype> Tools { get; private set; }

    // TODO: Remove this snowflake stuff, make damage per-tool quality perhaps?
    [DataField]
    public DamageSpecifier? WeldingDamage { get; private set; }

    [DataField]
    public DamageSpecifier? DefaultDamage { get; private set; }
}
