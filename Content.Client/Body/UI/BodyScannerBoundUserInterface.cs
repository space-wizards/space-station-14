using System;
using Content.Shared.Body.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.Body.UI
{
    [UsedImplicitly]
    public sealed class BodyScannerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private BodyScannerDisplay? _display;

        public BodyScannerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();
            _display = new BodyScannerDisplay(this);
            _display.OnClose += Close;
            _display.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not BodyScannerUIState scannerState)
            {
                return;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.EntityExists(scannerState.Uid))
            {
                throw new ArgumentException($"Received an invalid entity with id {scannerState.Uid} for body scanner with id {Owner.Owner} at {entMan.GetComponent<TransformComponent>(Owner.Owner).MapPosition}");
            }

            _display?.UpdateDisplay(scannerState.Uid);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _display?.Dispose();
            }
        }
    }
}
