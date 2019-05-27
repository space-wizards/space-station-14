using Content.Client.Research;
using Content.Shared.GameObjects.Components.Research;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Log;

namespace Content.Client.GameObjects.Components.Research
{
    public class ResearchConsoleBoundUserInterface : BoundUserInterface
    {
        private int _points = 0;
        private int _pointsPerSecond = 0;
        private ResearchConsoleMenu _consoleMenu;
        public TechnologyDatabaseComponent TechnologyDatabase;


        public ResearchConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            if (!Owner.Owner.TryGetComponent(out TechnologyDatabase)) return;

            _consoleMenu = new ResearchConsoleMenu() { Owner = this };

            _consoleMenu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (SharedResearchConsoleComponent.ResearchConsoleBoundInterfaceState)state;
            _points = castState.Points;
            _pointsPerSecond = castState.PointsPerSecond;
        }
    }
}
