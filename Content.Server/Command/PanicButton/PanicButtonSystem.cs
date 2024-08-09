using Content.Server.Administration.Logs;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Command.PanicButton
{
    public sealed partial class PanicButtonSystem : EntitySystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PanicButtonComponent, UseInHandEvent>(PanicButtonOnUseInHand);
        }

        private void PanicButtonOnUseInHand(Entity<PanicButtonComponent> panicButton, ref UseInHandEvent args)
        {
            var component = panicButton.Comp;
            string radioMessage;

            if (!_powerCell.HasActivatableCharge(panicButton.Owner, user: args.User))
            {
                args.Handled = false;
                return;
            }

            if (!_powerCell.TryGetBatteryFromSlot(panicButton.Owner, out var batteryUid, out var battery, null) &&
                !TryComp(panicButton.Owner, out battery))
            {
                args.Handled = false;
                return;
            }

            if (battery.CurrentCharge == 0)
            {
                _popup.PopupEntity(Loc.GetString("comp-panic-button-cell-dead-message"), args.User, panicButton.Owner);
                args.Handled = false;
                return;
            }

            if (batteryUid == null)
            {
                args.Handled = false;
                return;
            }

            _powerCell.SetDrawEnabled(panicButton.Owner, true);

            // Specify the location of sending the message if the SpecifyLocation is true
            if (component.SpecifyLocation)
            {
                var locationText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(panicButton.Owner));
                var messageAgr = Loc.GetString("comp-panic-button-location", ("location", locationText));
                radioMessage = $"{Loc.GetString(component.RadioMessage)} {messageAgr}";
            }
            else
            {
                radioMessage = Loc.GetString(component.RadioMessage);
            }

            // Sends a message to the radio channel
            _radioSystem.SendRadioMessage(panicButton.Owner, radioMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel),
                panicButton.Owner);

            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):actor} used {ToPrettyString(panicButton.Owner):panic button}");

            args.Handled = true;
        }
    }
}
