using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Medical;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Content.Shared.GameObjects.Components;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class AcceptCloningBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly ILocalizationManager _localization;

        public AcceptCloningBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private AcceptCloningWindow _window;

        protected override void Open()
        {
            base.Open();

            _window = new AcceptCloningWindow(_localization);
            _window.OnClose += Close;
            _window.DenyButton.OnPressed += _ => _window.Close();
            _window.ConfirmButton.OnPressed += _ =>
            {
                SendMessage(
                    new SharedAcceptCloningComponent.UiButtonPressedMessage(
                        SharedAcceptCloningComponent.UiButton.Accept));
                _window.Close();
            };
            _window.OpenCentered();
        }

    }
}
