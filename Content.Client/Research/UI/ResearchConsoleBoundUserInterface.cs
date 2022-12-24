using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Research.UI
{
    [UsedImplicitly]
    public sealed class ResearchConsoleBoundUserInterface : BoundUserInterface
    {
        public int Points { get; private set; }
        public int PointsPerSecond { get; private set; }
        private ResearchConsoleMenu? _consoleMenu;
        private TechnologyDatabaseComponent? _technologyDatabase;
        private readonly IEntityManager _entityManager;
        private readonly SharedResearchSystem _research;

        public ResearchConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            SendMessage(new ConsoleServerSyncMessage());
            _entityManager = IoCManager.Resolve<IEntityManager>();
            _research = _entityManager.System<SharedResearchSystem>();
        }

        protected override void Open()
        {
            base.Open();

            if (!_entityManager.TryGetComponent(Owner.Owner, out _technologyDatabase))
                return;

            _consoleMenu = new ResearchConsoleMenu(this);

            _consoleMenu.OnClose += Close;

            _consoleMenu.ServerSyncButton.OnPressed += (_) =>
            {
                SendMessage(new ConsoleServerSyncMessage());
            };

            _consoleMenu.ServerSelectionButton.OnPressed += (_) =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };

            _consoleMenu.UnlockButton.OnPressed += (_) =>
            {
                if (_consoleMenu.TechnologySelected != null)
                {
                    SendMessage(new ConsoleUnlockTechnologyMessage(_consoleMenu.TechnologySelected.ID));
                }
            };

            _consoleMenu.OpenCentered();
        }

        public bool IsTechnologyUnlocked(TechnologyPrototype technology)
        {
            if (_technologyDatabase == null)
                return false;

            return _research.IsTechnologyUnlocked(_technologyDatabase.Owner, technology, _technologyDatabase);
        }

        public bool CanUnlockTechnology(TechnologyPrototype technology)
        {
            if (_technologyDatabase == null)
                return false;

            return _research.CanUnlockTechnology(_technologyDatabase.Owner, technology, _technologyDatabase);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ResearchConsoleBoundInterfaceState)state;
            Points = castState.Points;
            PointsPerSecond = castState.PointsPerSecond;
            // We update the user interface here.
            _consoleMenu?.PopulatePoints();
            _consoleMenu?.Populate();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _consoleMenu?.Dispose();
        }
    }
}
