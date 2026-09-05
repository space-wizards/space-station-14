using Content.Shared.Damage;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Damage all items in specified containers
/// </summary>
[DataDefinition]
public sealed partial class DamageContainerContentsBehavior : IThresholdBehavior
{
    /// <summary>
    /// List of containers to apply damage to contents
    /// </summary>
    [DataField]
    public List<string> Containers = new();

    /// <summary>
    /// Damage specifier to apply to container contents.
    /// If null, apply the current damage of the owner entity to all the contents
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage = null;

    [DataField]
    public bool IgnoreResistances = false;

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        if (!system.EntityManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
            return;

        var containerSys = system.EntityManager.System<ContainerSystem>();
        var damageSys = system.EntityManager.System<DamageableSystem>();

        foreach (var containerId in Containers)
        {
            if (!containerSys.TryGetContainer(owner, containerId, out var container, containerManager))
                continue;

            if (Damage == null)
            {
                if (!system.EntityManager.TryGetComponent<DamageableComponent>(owner, out var damageable))
                    return;
                Damage = damageable.Damage;
            }
            foreach (var ent in container.ContainedEntities)
            {
                if(!system.EntityManager.TryGetComponent<DamageableComponent>(ent, out var damageable))
                    continue;
                damageSys.TryChangeDamage(ent, Damage, IgnoreResistances, true, damageable);
            }
        }
    }
}
