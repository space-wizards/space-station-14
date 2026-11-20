using Content.Shared.Damage;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// This component causes its entity to continuously take damage based on the entity's current satiation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SatiationDamageSystem))]
public sealed partial class SatiationDamageComponent : Component
{
    /// <summary>
    /// Damage values by satiation threshold, for a satiation type.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<DamageSpecifier?>> Satiations;

    /// <summary>
    /// How often the damage is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Runtime data, indicating when the next damage application will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamageTime;
}
