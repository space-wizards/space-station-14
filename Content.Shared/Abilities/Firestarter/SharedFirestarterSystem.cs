using Content.Shared.Actions;

namespace Content.Shared.Abilities.Firestarter;

public sealed class SharedFirestarterSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FirestarterComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// Adds the firestarter action.
    /// </summary>
    private void OnComponentInit(EntityUid uid, FirestarterComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.FireStarterActionEntity, component.FireStarterAction, uid);
    }
}
