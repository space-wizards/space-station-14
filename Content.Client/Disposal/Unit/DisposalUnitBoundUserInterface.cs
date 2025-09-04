using Content.Client.Disposal.Mailing;
using Content.Client.Power.EntitySystems;
using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Unit
{
    /// <summary>
    /// Initializes a <see cref="MailingUnitWindow"/> or a <see cref="_disposalUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalUnitBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private DisposalUnitWindow? _disposalUnitWindow;

        public DisposalUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        private void ButtonPressed(DisposalUnitComponent.UiButton button)
        {
            SendPredictedMessage(new DisposalUnitComponent.UiButtonPressedMessage(button));
            // If we get client-side power stuff then we can predict the button presses but for now we won't as it stuffs
            // the pressure lerp up.
        }

        protected override void Open()
        {
            base.Open();

            _disposalUnitWindow = this.CreateWindow<DisposalUnitWindow>();

            _disposalUnitWindow.OpenCenteredRight();

            _disposalUnitWindow.Eject.OnPressed += _ => ButtonPressed(DisposalUnitComponent.UiButton.Eject);
            _disposalUnitWindow.Engage.OnPressed += _ => ButtonPressed(DisposalUnitComponent.UiButton.Engage);
            _disposalUnitWindow.Power.OnPressed += _ => ButtonPressed(DisposalUnitComponent.UiButton.Power);

            if (EntMan.TryGetComponent(Owner, out DisposalUnitComponent? component))
            {
                Refresh((Owner, component));
            }
        }

        public void Refresh(Entity<DisposalUnitComponent> entity)
        {
            if (_disposalUnitWindow == null)
                return;

            var disposalSystem = EntMan.System<DisposalUnitSystem>();

            _disposalUnitWindow.Title = EntMan.GetComponent<MetaDataComponent>(entity.Owner).EntityName;

            var state = disposalSystem.GetState(entity.Owner, entity.Comp);

            _disposalUnitWindow.UnitState.Text = Loc.GetString($"disposal-unit-state-{state}");
            _disposalUnitWindow.Power.Pressed = EntMan.System<PowerReceiverSystem>().IsPowered(Owner);
            _disposalUnitWindow.Engage.Pressed = entity.Comp.Engaged;
            _disposalUnitWindow.FullPressure = disposalSystem.EstimatedFullPressure(entity.Owner, entity.Comp);
        }
    }
}
