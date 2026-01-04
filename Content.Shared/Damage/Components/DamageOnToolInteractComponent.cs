using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Marks an entity to inflict configured damage when interacted with using a qualifying tool.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnToolInteractComponent : Component
{
    /// <summary>
    /// Tool quality required for the default tool-based damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> Tools;

    /// <summary>
    /// Optional damage override used for welding tools.
    /// TODO: Remove this snowflake stuff, make damage per-tool quality perhaps?
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? WeldingDamage;

    /// <summary>
    /// Default damage applied when a qualifying tool is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? DefaultDamage;
}
