using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Manages the damage of a <see cref="DamageableComponent" /> with a 'simple damage' model
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InjurableComponent : Component
{
    /// <summary>
    ///     This <see cref="DamageContainerPrototype"/> specifies what damage types are supported by this component.
    ///     If null, all damage types will be supported.
    /// </summary>
    [DataField, AutoNetworkedField]
    // ReSharper disable once InconsistentNaming - This is wrong but fixing it is potentially annoying for downstreams.
    public ProtoId<DamageContainerPrototype>? DamageContainer;

    /// <summary>
    ///     Group types that affect the pain overlay.
    /// </summary>
    ///     TODO: Add support for adding damage types specifically rather than whole damage groups
    [DataField]
    // ReSharper disable once UseCollectionExpression - Cannot refactor this as it's a potential sandbox volation.
    public List<ProtoId<DamageGroupPrototype>> PainDamageGroups = new() { "Brute", "Burn" };

    [DataField]
    public Dictionary<MobState, ProtoId<HealthIconPrototype>> HealthIcons = new()
    {
        { MobState.Alive, "HealthIconFine" },
        { MobState.Critical, "HealthIconCritical" },
        { MobState.Dead, "HealthIconDead" },
    };

    [DataField]
    public ProtoId<HealthIconPrototype> RottingIcon = "HealthIconRotting";
}
