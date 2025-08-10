using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Healing;

/// <summary>
/// Applies a damage change to the target when used in an interaction.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HealingComponent : Component
{
    /// <remarks>
    /// The amount of damage to heal per use.
    /// </remarks>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    /// <remarks>
    /// This should generally be negative,
    /// since you're, like, trying to heal damage.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public float BloodlossModifier = 0.0f;

    /// <summary>
    /// Restore missing blood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifyBloodLevel = 0.0f;

    /// <remarks>
    /// The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. For a
    /// HealingComponent this filters what damage container type this component should work on. If null,
    /// all damage container types are supported.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageContainerPrototype>>? DamageContainers;

    /// <summary>
    /// How long it takes to apply the damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Delay = 3f;

    /// <summary>
    /// Delay multiplier when healing yourself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SelfHealPenaltyMultiplier = 3f;

    /// <summary>
    /// Sound played on healing begin.
    /// </summary>
    [DataField]
    public SoundSpecifier? HealingBeginSound = null;

    /// <summary>
    /// Sound played on healing end.
    /// </summary>
    [DataField]
    public SoundSpecifier? HealingEndSound = null;
}
