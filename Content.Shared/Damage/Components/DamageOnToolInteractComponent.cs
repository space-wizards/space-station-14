using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnToolInteractComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> Tools { get; set; }

    // TODO: Remove this snowflake stuff, make damage per-tool quality perhaps?
    [DataField, AutoNetworkedField]
    public DamageSpecifier? WeldingDamage { get; set; }

    [DataField, AutoNetworkedField]
    public DamageSpecifier? DefaultDamage { get; set; }
}
