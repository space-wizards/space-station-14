using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using static Content.Shared.Chemistry.Components.SharedChemMasterComponent;

namespace Content.Client.Fax.UI
{
    [UsedImplicitly]
    public sealed class FaxMachineBoundUserInterface : BoundUserInterface
    {
        private FaxMachineWindow? _window;

        public FaxMachineBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();

            //Setup window layout/elements
            _window = new FaxMachineWindow
            {
                Title = Loc.GetString("fax-bound-user-interface-title"),
            };

            _window.OpenCentered();
            _window.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ChemMasterBoundUserInterfaceState) state;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
