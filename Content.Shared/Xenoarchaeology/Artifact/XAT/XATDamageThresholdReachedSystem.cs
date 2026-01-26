using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires certain damage to be applied to artifact within a timeframe.
/// </summary>
public sealed class XATDamageThresholdReachedSystem : BaseXATSystem<XATDamageThresholdReachedComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<XenoArtifactComponent> artifact, Entity<XATDamageThresholdReachedComponent, XenoArtifactNodeComponent> node, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null || args.Origin == artifact.Owner)
            return;

        var damageTriggerComponent = node.Comp1;
        if (Timing.IsFirstTimePredicted)
            damageTriggerComponent.AccumulatedDamage += args.DamageDelta;

        foreach (var (type, needed) in damageTriggerComponent.TypesNeeded)
        {
            if (damageTriggerComponent.AccumulatedDamage.DamageDict.GetValueOrDefault(type) >= needed)
            {
                InvokeTrigger(artifact, node);
                return; // intentional. Do not continue checks
            }
        }

        foreach (var (group, needed) in damageTriggerComponent.GroupsNeeded)
        {
            var damageGroupPrototype = _prototype.Index(group);
            if (!damageTriggerComponent.AccumulatedDamage.TryGetDamageInGroup(damageGroupPrototype, out var damage))
                continue;

            if (damage >= needed)
            {
                InvokeTrigger(artifact, node);
                return; // intentional. Do not continue checks
            }
        }
    }

    private void InvokeTrigger(
        Entity<XenoArtifactComponent> artifact,
        Entity<XATDamageThresholdReachedComponent, XenoArtifactNodeComponent> node
    )
    {
        var damageTriggerComponent = node.Comp1;
        damageTriggerComponent.AccumulatedDamage.DamageDict.Clear();
        Dirty(node, damageTriggerComponent);
        Trigger(artifact, node);
    }
}
