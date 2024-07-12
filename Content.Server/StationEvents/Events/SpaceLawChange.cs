using System.Collections.Generic;
using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Paper;
using Content.Shared.Dataset;
using Robust.Shared.Random;
using Content.Shared.Fax.Components;
using Content.Server.StationEvents.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events
{
    public sealed class SpaceLawChangeRule : StationEventSystem<SpaceLawChangeRuleComponent>
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const int DispatchTime = 10; // Wait time to send confirmation

        protected override void Started(EntityUid uid, SpaceLawChangeRuleComponent component,
            GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            // Loading a prototype dataset
            if (!_prototypeManager.TryIndex<LocalizedDatasetPrototype>("spaceLawChangeLaws", out var dataset))
            {
                Logger.Error("Could not find dataset prototype 'spaceLawChangeLaws'");
                return;
            }

            // Ensure SequenceLaws is initialized only once
            if (component.SequenceLaws.Count == 0)
            {
                component.SequenceLaws.AddRange(dataset.Values);
            }

            // Select a random law from the first half of SequenceLaws
            List<string> listOldLaws = component.SequenceLaws.GetRange(0, component.SequenceLaws.Count / 2);

            var randomLaw = _robustRandom.Pick(listOldLaws);
            var rearrangement = component.SequenceLaws.IndexOf(randomLaw);

            // Move the selected law to the end of SequenceLaws
            component.SequenceLaws.RemoveAt(rearrangement);
            component.SequenceLaws.Add(randomLaw);

            component.RandomMessage = Loc.GetString(randomLaw);
            var message = Loc.GetString("station-event-space-law-change-announcement",
                ("essence", component.RandomMessage),
                ("time", DispatchTime));

            // Send a global announcement
            _chat.DispatchGlobalAnnouncement(message, playSound: true, colorOverride: Color.Gold);
            SendSpaceLawChangeFax(message);

            // Start a timer to send a confirmation message after DispatchTime minutes
            Timer.Spawn(TimeSpan.FromMinutes(DispatchTime), () => SendConfirmationMessage(uid));
        }

        /// <summary>
        ///     Sends a confirmation message that the change in Space Law has come into effect
        /// </summary>
        private void SendConfirmationMessage(EntityUid uid)
        {
            if (!_entityManager.TryGetComponent(uid, out SpaceLawChangeRuleComponent? component) || component.RandomMessage == null)
            {
                Logger.Error($"Failed to send confirmation message for Space Law change event for entity {uid}: Component or RandomMessage is null.");
                return;
            }

            var confirmationMessage = Loc.GetString("station-event-space-law-change-announcement-confirmation", ("essence", component.RandomMessage));
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

            var faxes = _entityManager.EntityQuery<FaxMachineComponent>();
            foreach (var fax in faxes)
            {
                _faxSystem.Receive(fax.Owner, printout, null, fax);
            }
        }
    }
}
