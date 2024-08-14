using Content.Shared.Mobs.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Mobs.Systems;

/// <summary>
///     Adds and removes defined components when a mob's <see cref="MobState"/> changes.
/// </summary>
public sealed class MobStateComponentsSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponentsComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponentsComponent component, MobStateChangedEvent args)
    {
        foreach (var comp in component.GrantedComponents)
        {
            EntityManager.RemoveComponent(uid, comp);
        }
        component.GrantedComponents.Clear();

        if (!component.Components.TryGetValue(args.NewMobState, out var toGrant))
            return;

        foreach (var compType in toGrant)
        {
            if (!EntityManager.HasComponent(uid, compType))
            {
                EntityManager.AddComponent(uid, compType);
                component.GrantedComponents.Add(compType);
            }
        }
    }
}
