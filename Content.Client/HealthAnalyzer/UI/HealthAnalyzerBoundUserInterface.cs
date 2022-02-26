using JetBrains.Annotations;
using Robust.Client.GameObjects;

using static Content.Shared.HealthAnalyzer.SharedHealthAnalyzerComponent;

namespace Content.Client.HealthAnalyzer.UI
{
    [UsedImplicitly]
    public class HealthAnalyzerBoundUserInterface : BoundUserInterface
    {
        private HealthAnalyzerWindow? _window;

        private HealthAnalyzerBoundUserInterfaceState? _lastState;

        public HealthAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new HealthAnalyzerWindow
            {
                Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (HealthAnalyzerBoundUserInterfaceState)state;
            _lastState = castState;

            _window?.Populate(castState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }
    }
}
