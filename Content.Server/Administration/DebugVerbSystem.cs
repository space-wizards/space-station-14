using Content.Server.Administration.Commands;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Inventory.Components;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification.Managers;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration
{
    /// <summary>
    ///     System to provide various global debug verbs
    /// </summary>
    public class DebugVerbSystem : EntitySystem 
    {
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IServerConsoleHost _consoleHost = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AssembleVerbsEvent>(AddDebugVerbs);
            // TODO QUESTION VERBS
            // if allowing an Assemble-Target and Assemble-Tool/USing
            // Maybe allow assemble on user?
        }


        // TODO QUESTION
        // MAAAAybe these verbs should go into dedicated systens.
        // E.g., tube verb -> disposal tubes system.
        // and posses/take-control into mind system
        // But then where do you put stuff like make sentient
        // its defining feature is that it's target does NOT have a mind, so no targeted subscriptions
        private void AddDebugVerbs(AssembleVerbsEvent args)
        {
            if (!args.Types.HasFlag(VerbTypes.Other))
                return;

            if (!args.User.TryGetComponent<ActorComponent>(out var actor))
            {
                return;
            }
            var player = actor.PlayerSession;

            // add delete verb?
            if (_groupController.CanCommand(player, "deleteentity"))
            {
                Verb verb = new Verb("debug:delete");
                verb.Text = Loc.GetString("delete-verb-get-data-text");
                verb.Category = VerbCategories.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/delete.svg.192dpi.png";
                verb.Act = () => args.Target.Delete();
                args.Verbs.Add(verb);
            }

            // add rejuvenate verb?
            if (_groupController.CanCommand(player, "rejuvenate"))
            {
                Verb verb = new Verb("debug:rejuvenate");
                verb.Text = Loc.GetString("rejuvenate-verb-get-data-text");
                verb.Category = VerbCategories.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png";
                verb.Act = () => Rejuvenate.PerformRejuvenate(args.Target);
                args.Verbs.Add(verb);
            }

            // Control mob verb
            if (_groupController.CanCommand(player, "controlmob") &&
                args.User != args.Target &&
                args.User.HasComponent<MindComponent>() &&
                args.Target.TryGetComponent<MindComponent>(out var targetMind))
            {
                Verb verb = new Verb("debug:control");
                verb.Text = Loc.GetString("control-mob-verb-get-data-text");
                verb.Category = VerbCategories.Debug;
                // TODO VERB ICON control mob icon
                verb.Act = () =>
                {
                    targetMind.Mind?.TransferTo(null);
                    player.ContentData()?.Mind?.TransferTo(args.Target, ghostCheckOverride: true);
                };
                args.Verbs.Add(verb);
            }

            // Make Sentient verb
            if (_groupController.CanCommand(player, "makesentient") &&
                args.User != args.Target &&
                !args.Target.HasComponent<MindComponent>())
            {
                Verb verb = new Verb("debug:sentient");
                verb.Text = Loc.GetString("make-sentient-verb-get-data-text");
                verb.Category = VerbCategories.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/sentient.svg.192dpi.png";
                verb.Act = () =>
                {
                    var cmd = new MakeSentientCommand();
                    var uidStr = args.Target.Uid.ToString();
                    cmd.Execute(new ConsoleShell(_consoleHost, player), $"{cmd.Command} {uidStr}",
                        new[] { uidStr });
                };
                args.Verbs.Add(verb);
            }

            // Set clothing verb
            if (_groupController.CanCommand(player, "setoutfit") &&
                args.Target.HasComponent<InventoryComponent>())
            {
                Verb verb = new Verb("debug:outfit");
                verb.Text = Loc.GetString("set-outfit-verb-get-data-text");
                verb.Category = VerbCategories.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png";
                verb.Act = () =>
                {
                    var cmd = new SetOutfitCommand();
                    var uidStr = args.Target.Uid.ToString();
                    cmd.Execute(new ConsoleShell(_consoleHost, player), $"{cmd.Command} {uidStr}",
                        new[] { uidStr });
                };
                args.Verbs.Add(verb);
            }

            // In range unoccluded verb
            if (_groupController.CanCommand(player, "inrangeunoccluded"))
            {
                Verb verb = new Verb("debug:unoccluded");
                verb.Text = Loc.GetString("in-range-unoccluded-verb-get-data-text");
                verb.Category = VerbCategories.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/information.svg.192dpi.png";
                verb.Act = () =>
                {
                    var message = args.User.InRangeUnOccluded(args.Target)
                    ? Loc.GetString("in-range-unoccluded-verb-on-activate-not-occluded")
                    : Loc.GetString("in-range-unoccluded-verb-on-activate-occluded");
                    args.Target.PopupMessage(args.User, message);
                };
                args.Verbs.Add(verb);
            }

            // Get Disposal tube direction verb
            if (_groupController.CanCommand(player, "tubeconnections") &&
                args.Target.TryGetComponent<IDisposalTubeComponent>(out var component))
            {
                Verb verb = new Verb("debug:tubeconnections");
                verb.Text = Loc.GetString("tube-direction-verb-get-data-text");
                verb.Category = VerbCategories.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/information.svg.192dpi.png";
                verb.Act = () =>
                {
                    component.PopupDirections(args.User);
                };
                args.Verbs.Add(verb);
            }
        }
    }
}
