using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;

namespace Content.Client.Chemistry.UI
{
    [UsedImplicitly]
    public sealed class MedipenRefillerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MedipenRefillerWindow? _window;

        public MedipenRefillerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new MedipenRefillerWindow
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
            };

            _window.OpenCentered();
            _window.OnClose += Close;
            _window.OnTransferButtonPressed += args => SendMessage(new MedipenRefillerTransferReagentMessage(args.Id, args.Value, args.IsBuffer));
            _window.InputEjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(SharedMedipenRefiller.InputSlotName));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case MedipenRefillerUpdateState msg:
                    if (_window != null)
                    {
                        _window.MedipenRecipes = msg.Recipes;
                        _window!.InputContainerData = msg.InputContainerData;
                        _window!.BufferData = msg.BufferData;
                    }
                    _window?.UpdateRecipes();
                    _window?.UpdateContainerInfo();
                    break;
            }
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
