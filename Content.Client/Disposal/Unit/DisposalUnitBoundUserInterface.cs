using Content.Client.Power.EntitySystems;
using Content.Client.UserInterface.Controls;
using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Unit
{
    [UsedImplicitly]
    public sealed class DisposalUnitBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
    {
        [ViewVariables]
        private DisposalUnitWindow? _disposalUnitWindow;

        protected override void Open()
        {
            base.Open();
            _disposalUnitWindow = this.CreateWindow<DisposalUnitWindow>();
            _disposalUnitWindow.OpenCenteredRight();
            _disposalUnitWindow.SetInfoFromEntity(EntMan, Owner);

            _disposalUnitWindow.OnTogglePower += ButtonPressed;
            _disposalUnitWindow.OnEject += ButtonPressed;
            _disposalUnitWindow.OnEngage += ButtonPressed;

            _disposalUnitWindow.OnChangeRouting += OpenRoutingWindow;

            Update();
        }

        public override void Update()
        {
            base.Update();

            if (_disposalUnitWindow == null)
                return;

            if (!EntMan.TryGetComponent<DisposalUnitComponent>(Owner, out var component))
                return;

            var disposalSystem = EntMan.System<DisposalUnitSystem>();
            _disposalUnitWindow.Populate(
                    EntMan.System<PowerReceiverSystem>().IsPowered(Owner),
                    component.Engaged,
                    disposalSystem.GetState((Owner, component)),
                    disposalSystem.EstimatedFullPressure((Owner, component)),
                    component.PressurePerSecond);

            if (EntMan.TryGetComponent<DisposalTaggerComponent>(Owner, out var tagger))
                _disposalUnitWindow.PopulateRouting(tagger.Editable);
        }

        private void ButtonPressed(DisposalUnitUiButton button)
        {
            SendPredictedMessage(new DisposalUnitUiButtonPressedMessage(button));
        }

        private void OpenRoutingWindow()
        {
            SendPredictedMessage(new DisposalTaggerOpenUiMessage());
        }
    }
}
