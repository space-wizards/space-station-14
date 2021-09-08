using Content.Server.Administration.Commands;
using Content.Server.Administration.UI;
using Content.Server.Disposal.Tube.Components;
using Content.Server.EUI;
using Content.Server.Ghost.Roles;
using Content.Server.Inventory.Components;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification.Managers;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration
{
    /// <summary>
    ///     System to provide various global admin/debug verbs
    /// </summary>
    public class AdminVerbSystem : EntitySystem 
    {
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly GhostRoleSystem _ghostRoleSystem = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<GetOtherVerbsEvent>(AddDebugVerbs);
            // TODO QUESTION VERBS
            // if allowing an Assemble-Target and Assemble-Tool/USing
            // Maybe allow assemble on user?
        }

        // Odd collection of verbs.
        // Some require components to be present
        // And maybe should be moved to relevant systems
        // but also: all of them require IConGroupController.CanCommand(player, "....") checks
        // and also, all of them are debug-category verbs
        // Many of then need IServerConsoleHost

        // TODO QUESTION
        // Maybe these verbs should go into dedicated systems.
        // E.g., tube verb -> disposal tubes system.
        // and posses/take-control into mind system
        // But then where do you put stuff like make sentient
        // its defining feature is that it's target does NOT have a mind, so no targeted subscriptions
        private void AddDebugVerbs(GetOtherVerbsEvent args)
        {
            if (!args.User.TryGetComponent<ActorComponent>(out var actor))
            {
                return;
            }
            var player = actor.PlayerSession;

            // add delete verb?
            if (_groupController.CanCommand(player, "deleteentity"))
            {
                Verb verb = new Verb("debug:delete");
                verb.Text = Loc.GetString("pointing-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/delete.svg.192dpi.png";
                verb.Act = () => args.Target.Delete();
                args.Verbs.Add(verb);
            }

            // add rejuvenate verb?
            if (_groupController.CanCommand(player, "rejuvenate"))
            {
                Verb verb = new Verb("debug:rejuvenate");
                verb.Text = Loc.GetString("rejuvenate-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png";
                verb.Act = () => RejuvenateCommand.PerformRejuvenate(args.Target);
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
                verb.Category = VerbCategory.Debug;
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
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/sentient.svg.192dpi.png";
                verb.Act = () => MakeSentientCommand.MakeSentient(args.Target);
                args.Verbs.Add(verb);
            }

            // Set clothing verb
            if (_groupController.CanCommand(player, "setoutfit") &&
                args.Target.HasComponent<InventoryComponent>())
            {
                Verb verb = new Verb("debug:setoutfit");
                verb.Text = Loc.GetString("set-outfit-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png";
                verb.Act = () => _euiManager.OpenEui(new SetOutfitEui(args.Target), player);
                args.Verbs.Add(verb);
            }

            // In range unoccluded verb
            if (_groupController.CanCommand(player, "inrangeunoccluded"))
            {
                Verb verb = new Verb("debug:unoccluded");
                verb.Text = Loc.GetString("in-range-unoccluded-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
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
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/information.svg.192dpi.png";
                verb.Act = () => component.PopupDirections(args.User);
                args.Verbs.Add(verb);
            }

            // Make ghost role verb
            if (_groupController.CanCommand(player, "makeghostrole") &&
                !(args.Target.GetComponentOrNull<MindComponent>()?.HasMind ?? false) )
            {
                Verb verb = new Verb("debug:makeghostrole");
                verb.Text = Loc.GetString("make-ghost-role-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                // TODO VERB ICON add ghost icon
                // Where is the national park service icon for haunted forests?
                verb.Act = () => _ghostRoleSystem.OpenMakeGhostRoleEui(player, args.Target.Uid);
                args.Verbs.Add(verb);
            }
        }
    }
}
