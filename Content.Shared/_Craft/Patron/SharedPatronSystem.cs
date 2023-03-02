using Robust.Shared.GameStates;

namespace Content.Shared.Patron
{
    public abstract class SharedPatronSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PatronEarsVisualizerComponent, ComponentGetState>(OnEarsGetState);
            SubscribeLocalEvent<PatronEarsVisualizerComponent, ComponentHandleState>(OnEarsHandleState);
        }
        private void OnEarsGetState(EntityUid uid, PatronEarsVisualizerComponent component, ref ComponentGetState args)
        {
            args.State = new PatronEarsVisualizerComponentState(component.RsiPath);
        }
        private void OnEarsHandleState(EntityUid uid, PatronEarsVisualizerComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PatronEarsVisualizerComponentState state) return;
            var ev = new OnPatronEarsVisualizerChangedEvent(state.RsiPath);
            RaiseLocalEvent(ev);
            component.RsiPath = state.RsiPath;
        }
    }
    public sealed class OnPatronEarsVisualizerChangedEvent : EntityEventArgs
    {
        public string RsiPath;
        public OnPatronEarsVisualizerChangedEvent(string rsipath)
        {
            RsiPath = rsipath;
        }
    }
}
