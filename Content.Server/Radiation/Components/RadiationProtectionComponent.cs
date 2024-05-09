using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.Radiation.Components;

/// <summary>
///     Exists for use as a status effect. Applies the specified DamageModifierSet when the entity takes damage.
/// </summary>
[RegisterComponent]
public sealed partial class RadiationProtectionComponent : Component
{
    /// <summary>
    ///     The radiation damage modifier for entities with this component.
    /// </summary>
    [DataField("modifier")]
    public ProtoId<DamageModifierSetPrototype> RadiationProtectionModifierSetId = "PotassiumIodide";
}
