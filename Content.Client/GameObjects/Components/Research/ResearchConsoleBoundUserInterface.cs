using Content.Client.Research;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Research
{
    public class ResearchConsoleBoundUserInterface : BoundUserInterface
    {
        public int Points { get; private set; } = 0;
        public int PointsPerSecond { get; private set; } = 0;
        private ResearchConsoleMenu _consoleMenu;
        private TechnologyDatabaseComponent TechnologyDatabase;


        public ResearchConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new SharedResearchConsoleComponent.ConsoleServerSyncMessage());
        }

        protected override void Open()
        {
            base.Open();

            if (!Owner.Owner.TryGetComponent(out TechnologyDatabase)) return;

            _consoleMenu = new ResearchConsoleMenu(this);

            _consoleMenu.OnClose += Close;

            _consoleMenu.ServerSyncButton.OnPressed += (args) =>
                {
                    SendMessage(new SharedResearchConsoleComponent.ConsoleServerSyncMessage());
                };

            _consoleMenu.ServerSelectionButton.OnPressed += (args) =>
            {
                SendMessage(new SharedResearchConsoleComponent.ConsoleServerSelectionMessage());
            };

            _consoleMenu.UnlockButton.OnPressed += (args) =>
            {
                SendMessage(new SharedResearchConsoleComponent.ConsoleUnlockTechnologyMessage(_consoleMenu.TechnologySelected.ID));
            };

            _consoleMenu.OpenCentered();

            TechnologyDatabase.OnDatabaseUpdated += _consoleMenu.Populate;
        }

        public bool IsTechnologyUnlocked(TechnologyPrototype technology)
        {
            return TechnologyDatabase.IsTechnologyUnlocked(technology);
        }

        public bool CanUnlockTechnology(TechnologyPrototype technology)
        {
            return TechnologyDatabase.CanUnlockTechnology(technology);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (SharedResearchConsoleComponent.ResearchConsoleBoundInterfaceState)state;
            Points = castState.Points;
            PointsPerSecond = castState.PointsPerSecond;
            // We update the user interface here.
            _consoleMenu?.PopulatePoints();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _consoleMenu?.Dispose();
        }
    }
}
