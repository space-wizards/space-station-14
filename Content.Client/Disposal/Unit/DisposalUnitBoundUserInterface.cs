using Content.Client.Power.EntitySystems;
using Content.Client.UserInterface.Controls;
using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Unit
{
    [UsedImplicitly]
    public sealed class DisposalUnitBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DisposalUnitWindow? _disposalUnitWindow;

        public DisposalUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        private void ButtonPressed(DisposalUnitUiButton button)
        {
            SendPredictedMessage(new DisposalUnitUiButtonPressedMessage(button));
        }

        protected override void Open()
        {
            base.Open();

            _disposalUnitWindow = this.CreateWindow<DisposalUnitWindow>();
            _disposalUnitWindow.OpenCenteredRight();

            _disposalUnitWindow.SetInfoFromEntity(EntMan, Owner);

            _disposalUnitWindow.Eject.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Eject);
            _disposalUnitWindow.Engage.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Engage);
            _disposalUnitWindow.Power.OnPressed += _ =>
            {
                ButtonPressed(DisposalUnitUiButton.Power);
                ToggleStateText(_disposalUnitWindow, _disposalUnitWindow.Power.Pressed);
            };

            _disposalUnitWindow.Routing.OnPressed += _ => SendPredictedMessage(new DisposalTaggerOpenUiMessage());

            Update();
        }

        public override void Update()
        {
            base.Update();

            if (EntMan.TryGetComponent(Owner, out DisposalUnitComponent? component))
            {
                Refresh((Owner, component));
            }
        }

        public void Refresh(Entity<DisposalUnitComponent> entity)
        {
            if (_disposalUnitWindow == null)
                return;

            var disposalUnit = EntMan.System<DisposalUnitSystem>();

            var powered = EntMan.System<PowerReceiverSystem>().IsPowered(Owner);
            var disposalState = disposalUnit.GetState(entity);
            var fullPressure = disposalUnit.EstimatedFullPressure(entity);
            var pressurePerSecond = entity.Comp.PressurePerSecond;

            ToggleStateText(_disposalUnitWindow, powered);
            _disposalUnitWindow.UnitState.Text = Loc.GetString($"disposal-unit-state-{disposalState}");
            _disposalUnitWindow.FullPressure = disposalUnit.EstimatedFullPressure(entity);
            _disposalUnitWindow.PressurePerSecond = entity.Comp.PressurePerSecond;
            _disposalUnitWindow.PressureBar.UpdatePressure(fullPressure, pressurePerSecond);
            _disposalUnitWindow.Power.Pressed = powered;
            _disposalUnitWindow.Engage.Pressed = entity.Comp.Engaged;

            // Hide the button if there's nothing for it to do
            _disposalUnitWindow.Routing.Visible =
                EntMan.TryGetComponent<DisposalTaggerComponent>(Owner, out var tagger) && tagger.Editable;
        }

        /// <summary>
        /// This trick is used to hide power being unpredicted so the UI feels responsive.
        /// </summary>
        private void ToggleStateText(DisposalUnitWindow window, bool powered)
        {
            window.UnitState.Visible = powered;
            window.UnitStateUnpowered.Visible = !powered;
        }
    }
}
