using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Medical;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningPodComponent;

namespace Content.Client.GameObjects.Components.CloningPod
{
    [UsedImplicitly]
    public class CloningPodBoundUserInterface : BoundUserInterface
    {
        public CloningPodBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private CloningPodWindow _window;

        protected override void Open()
        {
            base.Open();


            _window = new CloningPodWindow(new Dictionary<int, string>());
            _window.OnClose += Close;
            _window.CloneButton.OnPressed += _ =>
            {
                if (_window.SelectedScan != null)
                {
                    SendMessage(new CloningPodUiButtonPressedMessage(UiButton.Clone, (int) _window.SelectedScan));
                }
            };
            _window.EjectButton.OnPressed += _ =>
            {
                SendMessage(new CloningPodUiButtonPressedMessage(UiButton.Eject, null));
            };
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window.Populate((CloningPodBoundUserInterfaceState) state);
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
