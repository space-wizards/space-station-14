using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

/// <summary>
///     Adds components to entity on certain mobstate. Remove when it changes.
/// </summary>
public sealed class AddCompOnMobStateChangeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddCompOnMobStateChangeComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, AddCompOnMobStateChangeComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobState))
            return;

        if (mobState.CurrentState == component.MobState)
        {
            EntityManager.AddComponents(uid, component.Components);
        }
        else
        {
            EntityManager.RemoveComponents(uid, component.Components);
        }
    }
}
