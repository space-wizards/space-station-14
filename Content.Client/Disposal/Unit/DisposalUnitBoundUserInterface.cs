using Content.Client.Power.EntitySystems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tagger;
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

        private void LineEdited(string newTag)
        {
            SendPredictedMessage(new DisposalUnitUiTaggerEditMessage(newTag));
        }

        protected override void Open()
        {
            base.Open();

            _disposalUnitWindow = this.CreateWindow<DisposalUnitWindow>();
            _disposalUnitWindow.OpenCenteredRight();

            _disposalUnitWindow.Eject.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Eject);
            _disposalUnitWindow.Engage.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Engage);
            _disposalUnitWindow.Power.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Power);

            _disposalUnitWindow.TagEdit.OnTextEntered += arg => LineEdited(arg.Text);
            _disposalUnitWindow.TagEdit.OnFocusExit += _ => RefreshTagEdit(); // More clarity for if you didn't change the tag

            Update();
        }

        public override void Update()
        {
            base.Update();

            if (EntMan.TryGetComponent(Owner, out DisposalUnitComponent? component))
            {
                Refresh((Owner, component));
            }

            RefreshTagEdit();
        }

        public void Refresh(Entity<DisposalUnitComponent> entity)
        {
            if (_disposalUnitWindow == null)
                return;

            var name = EntMan.GetComponent<MetaDataComponent>(entity.Owner).EntityName;
            _disposalUnitWindow.Title = Loc.GetString("ui-disposal-unit-title", ("name", name));

            if (!EntMan.TryGetComponent(entity.Owner, out DisposalUnitComponent? disposals))
                return;

            var disposalUnit = EntMan.System<DisposalUnitSystem>();
            var disposalState = disposalUnit.GetState(entity);
            var fullPressure = disposalUnit.EstimatedFullPressure((Owner, disposals));
            var pressurePerSecond = disposals.PressurePerSecond;

            _disposalUnitWindow.UnitState.Text = Loc.GetString($"disposal-unit-state-{disposalState}");
            _disposalUnitWindow.FullPressure = disposalUnit.EstimatedFullPressure(entity);
            _disposalUnitWindow.PressurePerSecond = entity.Comp.PressurePerSecond;
            _disposalUnitWindow.PressureBar.UpdatePressure(fullPressure, pressurePerSecond);
            _disposalUnitWindow.Power.Pressed = EntMan.System<PowerReceiverSystem>().IsPowered(Owner);
            _disposalUnitWindow.Engage.Pressed = entity.Comp.Engaged;
        }

        public void RefreshTagEdit()
        {
            if (_disposalUnitWindow == null)
                return;

            if (EntMan.TryGetComponent(Owner, out FlushTaggerComponent? component))
            {
                _disposalUnitWindow.TagBox.Visible = true;
                _disposalUnitWindow.TagEdit.Text = component.DisposalTag;
            }
            else
            {
                _disposalUnitWindow.TagBox.Visible = false;
            }
        }
    }
}
