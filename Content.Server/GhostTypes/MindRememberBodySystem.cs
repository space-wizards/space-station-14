using Content.Shared.Damage;
using Content.Shared.Gibbing.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.Containers;

namespace Content.Server.GhostTypes;

public sealed class MindRememberBodySystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MindRememberBodyComponent, AttemptEntityGibEvent>(SaveBody);
    }

    /// <summary>
    /// Saves the damage of a player body inside their MindComponent
    /// </summary>
    private void SaveBody(EntityUid uid, MindRememberBodyComponent component, AttemptEntityGibEvent args)
    {
        if (!_container.TryGetContainingContainer(uid, out var container)
            || !TryComp<DamageableComponent>(container.Owner, out var damageable)
            || !TryComp<MindContainerComponent>(container.Owner, out var mindContainer)
            || !TryComp<MindComponent>(mindContainer.Mind, out var mind))
            return;

        mind.DamagePerGroup = damageable.DamagePerGroup;
        mind.Damage = damageable.Damage;
    }
}
