using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Paper;
using Robust.Shared.Random;
using Content.Shared.Fax.Components;
using Content.Server.StationEvents.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events
{
    public sealed class SpaceLawChangeRule : StationEventSystem<SpaceLawChangeRuleComponent>
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;
        private readonly RandomSelector _randomSelector = default!;

        private string? _randomMessage;

        public SpaceLawChangeRule()
        {
            var options = new List<string>
            {
                "station-event-space-law-change-essence-1",
                "station-event-space-law-change-essence-2",
                "station-event-space-law-change-essence-3",
                "station-event-space-law-change-essence-4",
                "station-event-space-law-change-essence-5",
                "station-event-space-law-change-essence-6",
                "station-event-space-law-change-essence-7",
                "station-event-space-law-change-essence-8",
                "station-event-space-law-change-essence-9",
                "station-event-space-law-change-essence-10",
                "station-event-space-law-change-essence-11"
                // Add other message options here if necessary
            };

            _randomSelector = new RandomSelector(options);
        }

        protected override void Started(EntityUid uid, SpaceLawChangeRuleComponent component,
            GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            int dispatchTime = 10;
            _randomMessage = Loc.GetString($"{_randomSelector.GetRandom(_robustRandom)}");
            var message = Loc.GetString("station-event-space-law-change-announcement",
            ("essence", _randomMessage),
            ("time", dispatchTime));

            _chat.DispatchGlobalAnnouncement(message, playSound: true, colorOverride: Color.Gold);

            SendSpaceLawChangeFax(message);

            // Start a timer to send a confirmation message
            Timer.Spawn(TimeSpan.FromMinutes(dispatchTime), SendConfirmationMessage);
        }

        /// <summary>
        ///     Sends a confirmation message that the change in Space Law has come into effect
        /// </summary>
        private void SendConfirmationMessage()
        {
            if (_randomMessage == null) return;

            var confirmationMessage = Loc.GetString("station-event-space-law-change-announcement-confirmation", ("essence", _randomMessage));
            _chat.DispatchGlobalAnnouncement(confirmationMessage, playSound: true, colorOverride: Color.Gold);
        }

        /// <summary>
        ///     Sending a fax announcing changes in Space Law
        /// </summary>
        private void SendSpaceLawChangeFax(string message)
        {
            var printout = new FaxPrintout(
                message,
                Loc.GetString("materials-paper"),
                null,
                null,
                "paper_stamp-centcom",
                new List<StampDisplayInfo>
                {
                    new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") }
                });

            var faxes = EntityManager.EntityQuery<FaxMachineComponent>();
            foreach (var fax in faxes)
            {
                _faxSystem.Receive(fax.Owner, printout, null, fax);
            }
        }
    }

    public class RandomSelector
    {
        private readonly List<string> _options;
        private readonly List<string> _exclusions;

        public RandomSelector(List<string> options)
        {
            _options = options;
            _exclusions = new List<string>();
        }

        /// <summary>
        ///     Randomly selects an option from the list. Selected options cannot be selected again until all unselected options are exhausted.
        /// </summary>
        public string GetRandom(IRobustRandom robustRandom)
        {
            if (_exclusions.Count >= _options.Count)
                _exclusions.Clear();

            var availableOptions = _options.Except(_exclusions).ToList();
            if (availableOptions.Count == 0)
            {
                _exclusions.Clear();
                availableOptions = _options.ToList();
            }

            var selectedOption = robustRandom.PickAndTake(availableOptions);
            _exclusions.Add(selectedOption);

            return selectedOption;
        }
    }
}
