using Content.Shared.Actions;

namespace Content.Shared.PAI
{
    public abstract class SharedCommandPAISystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CommandPAIComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<CommandPAIComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnMapInit(EntityUid uid, CommandPAIComponent component, MapInitEvent args)
        {
            _actionsSystem.AddAction(uid, ref component.CrewMonitorAction, component.CrewMonitorActionId);

        }

        private void OnShutdown(EntityUid uid, CommandPAIComponent component, ComponentShutdown args)
        {
            _actionsSystem.RemoveAction(uid, component.CrewMonitorAction);
        }
    }
}

