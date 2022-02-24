using Content.Shared.PlayingCard;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.PlayingCard.UI;
    public class PlayingCardHandBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private PlayingCardHandMenu? _menu;

        public PlayingCardHandBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new CardListSyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();

            if(State != null)
                UpdateState(State);

            _menu = new PlayingCardHandMenu(this) {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};
            Logger.Debug("attempting to get playing card comp4");

            // _menu.Populate(PlayingCardHand.CardList);
            Logger.Debug("attempting to get playing card comp5");

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_menu == null || state is not PlayingCardHandBoundUserInterfaceState cast)
                return;

            // _minTemp = cast.MinTemperature;
            // _maxTemp = cast.MaxTemperature;

            // _window.SetTemperature(cast.Temperature);
            // _window.SetActive(cast.Enabled);
            // _window.Title = cast.Mode switch
            // {
            //     ThermoMachineMode.Freezer => Loc.GetString("comp-gas-thermomachine-ui-title-freezer"),
            //     ThermoMachineMode.Heater => Loc.GetString("comp-gas-thermomachine-ui-title-heater"),
            //     _ => string.Empty
            // };
        }

        public void Eject(int id)
        {
            SendMessage(new PickSingleCardMessage(id));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case CardListMessage msg:
                    _menu?.Populate(msg.Cards);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
