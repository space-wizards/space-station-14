using Content.Shared.Disease.Components;
using Content.Shared.Disease;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Disease.UI
{
    [UsedImplicitly]
    public sealed class VaccineMachineBoundUserInterface : BoundUserInterface
    {
        private VaccineMachineMenu? _machineMenu;

        public EntityUid Machine;
        public VaccineMachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            Machine = owner.Owner;
        }

        protected override void Open()
        {
            base.Open();

            _machineMenu = new VaccineMachineMenu(this);

            _machineMenu.OnClose += Close;

            _machineMenu.OnServerSelectionButtonPressed += _ =>
            {
                SendMessage(new VaccinatorServerSelectionMessage());
            };

            _machineMenu.OpenCentered();
            _machineMenu?.PopulateBiomass(Machine);
        }

        public void CreateVaccineMessage(string disease, int amount)
        {
            SendMessage(new CreateVaccineMessage(disease, amount));
        }

        public void RequestSync()
        {
            SendMessage(new VaccinatorSyncRequestMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case VaccineMachineUpdateState msg:
                    _machineMenu?.UpdateLocked(msg.Locked);
                    _machineMenu?.PopulateDiseases(msg.Diseases);
                    _machineMenu?.PopulateBiomass(Machine);
                    _machineMenu?.UpdateCost(msg.BiomassCost);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _machineMenu?.Dispose();
        }
    }
}
