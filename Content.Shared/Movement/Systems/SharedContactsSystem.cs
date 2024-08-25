using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public abstract class SharedContactsSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly MovementSpeedModifierSystem SpeedModifierSystem = default!;

    // TODO full-game-save
    // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
    protected HashSet<EntityUid> ToUpdate = [];
    protected HashSet<EntityUid> ToRemove = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentShutdown>(OnShutdown);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    protected void EntityUpdateContacts(EntityUid uid)
    {
        ToUpdate.Add(uid);
    }

    private void OnShutdown(EntityUid uid, T component, ComponentShutdown args)
    {
        if (!TryComp(uid, out PhysicsComponent? phys))
            return;

        // Note that the entity may not be getting deleted here. E.g., glue puddles.
        ToUpdate.UnionWith(Physics.GetContactingEntities(uid, phys));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ToRemove.Clear();

        foreach (var uid in ToUpdate)
        {
            OnUpdate(uid);
        }

        foreach (var ent in ToRemove)
        {
            RemComp<T>(ent);
        }

        ToUpdate.Clear();
    }

    protected virtual void OnUpdate(EntityUid uid)
    {

    }
}
