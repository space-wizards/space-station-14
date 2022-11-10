using Content.Shared.Disease.Components;
using Content.Shared.Disease;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Disease.UI
{
    [UsedImplicitly]
    public sealed class VaccineMachineBoundUserInterface : BoundUserInterface
    {
        private VaccineMachineMenu? _consoleMenu;
        public VaccineMachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _consoleMenu = new VaccineMachineMenu(this);

            _consoleMenu.OnClose += Close;

            _consoleMenu.OpenCentered();
        }

        public void CreateVaccineMessage(DiseasePrototype disease)
        {
            SendMessage(new CreateVaccineMessage(disease));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (VaccineMachineBoundInterfaceState)state;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _consoleMenu?.Dispose();
        }
    }
}
