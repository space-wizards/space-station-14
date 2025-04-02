using Content.Shared.Actions;

namespace Content.Shared._DV.Harpy
{
    public sealed class HarpySingerSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HarpySingerComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<HarpySingerComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnStartup(EntityUid uid, HarpySingerComponent component, ComponentStartup args)
        {
            _actionsSystem.AddAction(uid, ref component.MidiAction, component.MidiActionId);
        }

        private void OnShutdown(EntityUid uid, HarpySingerComponent component, ComponentShutdown args)
        {
            _actionsSystem.RemoveAction(uid, component.MidiAction);
        }
    }
}
