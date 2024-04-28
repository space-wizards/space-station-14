using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.Radiation.Components;

[RegisterComponent]
public sealed partial class RadiationProtectionComponent : Component
{
    /// <summary>
    ///     The radiation damage modifier for entities with this component.
    /// </summary>
    [DataField("modifier")]
    public ProtoId<DamageModifierSetPrototype> RadiationProtectionModifierSetId = "PotassiumIodide";
}
