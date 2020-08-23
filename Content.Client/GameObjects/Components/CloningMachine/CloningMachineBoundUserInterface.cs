using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Medical;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningMachineComponent;

namespace Content.Client.GameObjects.Components.CloningMachine
{
    [UsedImplicitly]
    public class CloningMachineBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly ILocalizationManager _localization;

        public CloningMachineBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private CloningMachineWindow _window;

        protected override void Open()
        {
            base.Open();
            var foo = new List<EntityUid>();
            for (int i =1; i <= 20; i++)
            {
                foo.Add(new EntityUid(i));
            }

            _window = new CloningMachineWindow(foo, _localization);
            _window.OnClose += Close;
            _window.CloneButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone,_window.SelectedScan));
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            //TODO:Update with new entity UIDs
        }
    }

    public class Enumberable
    {
    }
}
