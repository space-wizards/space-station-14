using Content.Shared.Containers.ItemSlots;
using Content.Shared.Kitchen.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Kitchen.UI
{
    public sealed class ReagentGrinderBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private GrinderMenu? _menu;
        public ReagentGrinderBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();
            IoCManager.InjectDependencies(this);

            if (!_entityManager.TryGetComponent<ReagentGrinderComponent>(Owner.Owner, out var grinderComponent))
                return;

            _menu = new GrinderMenu();
            _menu.OpenCentered();
            _menu.OnClose += Close;

            _menu.OnGrindButtonPressed += _ =>
            {
                SendMessage(new ReagentGrinderGrindStartMessage());
            };

            _menu.OnJuiceButtonPressed += _ =>
            {
                SendMessage(new ReagentGrinderJuiceStartMessage());
            };

            _menu.OnEjectAllButtonPressed += _ =>
            {
                SendMessage(new ReagentGrinderEjectChamberAllMessage());
            };

            _menu.OnEjectBeakerButtonPressed += _ =>
            {
                SendMessage(new ItemSlotButtonPressedEvent(grinderComponent.BeakerSlotId));
            };

            _menu.OnItemListSelected += (_, uid) =>
            {
                SendMessage(new ReagentGrinderEjectChamberContentMessage(uid));
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is ReagentGrinderInterfaceState cState)
                _menu?.UpdateState(cState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);
            _menu?.HandleMessage(message);
        }
    }
}
