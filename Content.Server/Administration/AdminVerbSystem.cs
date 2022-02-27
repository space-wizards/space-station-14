using System.Collections.Generic;
using System.Threading;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Managers;
using Content.Server.Administration.UI;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Configurable;
using Content.Server.Disposal.Tube.Components;
using Content.Server.EUI;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Ghost.Roles;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration
{
    /// <summary>
    ///     System to provide various global admin/debug verbs
    /// </summary>
    public sealed class AdminVerbSystem : EntitySystem
    {
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IConsoleHost _console = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly GhostRoleSystem _ghostRoleSystem = default!;
        [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

        private readonly Dictionary<IPlayerSession, EditSolutionsEui> _openSolutionUis = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddAdminVerbs);
            SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddDebugVerbs);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<SolutionContainerManagerComponent, SolutionChangedEvent>(OnSolutionChanged);
        }

        private void AddAdminVerbs(GetVerbsEvent<Verb> args)
        {
            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;

            var player = actor.PlayerSession;

            if (_adminManager.IsAdmin(player))
            {
                if (TryComp(args.Target, out ActorComponent? targetActor))
                {
                    // AdminHelp
                    Verb verb = new();
                    verb.Text = Loc.GetString("ahelp-verb-get-data-text");
                    verb.Category = VerbCategory.Admin;
                    verb.IconTexture = "/Textures/Interface/gavel.svg.192dpi.png";
                    verb.Act = () =>
                        _console.RemoteExecuteCommand(player, $"openahelp \"{targetActor.PlayerSession.UserId}\"");
                    verb.Impact = LogImpact.Low;
                    args.Verbs.Add(verb);

                    // Freeze
                    var frozen = HasComp<AdminFrozenComponent>(args.Target);
                    args.Verbs.Add(new Verb
                    {
                        Priority = -1, // This is just so it doesn't change position in the menu between freeze/unfreeze.
                        Text = frozen
                            ? Loc.GetString("admin-verbs-unfreeze")
                            : Loc.GetString("admin-verbs-freeze"),
                        Category = VerbCategory.Admin,
                        IconTexture = "/Textures/Interface/VerbIcons/snow.svg.192dpi.png",
                        Act = () =>
                        {
                            if (frozen)
                                RemComp<AdminFrozenComponent>(args.Target);
                            else
                                EnsureComp<AdminFrozenComponent>(args.Target);
                        },
                        Impact = LogImpact.Medium,
                    });
                }

                // XenoArcheology
                if (TryComp<ArtifactComponent>(args.Target, out var artifact))
                {
                    // make artifact always active (by adding timer trigger)
                    args.Verbs.Add(new Verb()
                    {
                        Text = Loc.GetString("artifact-verb-make-always-active"),
                        Category = VerbCategory.Admin,
                        Act = () => EntityManager.AddComponent<ArtifactTimerTriggerComponent>(args.Target),
                        Disabled = EntityManager.HasComponent<ArtifactTimerTriggerComponent>(args.Target),
                        Impact = LogImpact.High
                    });

                    // force to activate artifact ignoring timeout
                    args.Verbs.Add(new Verb()
                    {
                        Text = Loc.GetString("artifact-verb-activate"),
                        Category = VerbCategory.Admin,
                        Act = () => _artifactSystem.ForceActivateArtifact(args.Target, component: artifact),
                        Impact = LogImpact.High
                    });
                }

                // TeleportTo
                args.Verbs.Add(new Verb
                {
                    Text = Loc.GetString("admin-verbs-teleport-to"),
                    Category = VerbCategory.Admin,
                    IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png",
                    Act = () => _console.ExecuteCommand(player, $"tpto {args.Target}"),
                    Impact = LogImpact.Low
                });

                // TeleportHere
                args.Verbs.Add(new Verb
                {
                    Text = Loc.GetString("admin-verbs-teleport-here"),
                    Category = VerbCategory.Admin,
                    IconTexture = "/Textures/Interface/VerbIcons/close.svg.192dpi.png",
                    Act = () => _console.ExecuteCommand(player, $"tpto {args.Target} {args.User}"),
                    Impact = LogImpact.Low
                });
            }

            // Artillery
            if (_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("explode-verb-get-data-text");
                verb.Category = VerbCategory.Admin;
                verb.Act = () =>
                {
                    var coords = Transform(args.Target).MapPosition;
                    Timer.Spawn(_gameTiming.TickPeriod, () => _explosionSystem.QueueExplosion(coords, "Default", 30, 4, 8), CancellationToken.None);
                    if (TryComp(args.Target, out SharedBodyComponent? body))
                    {
                        body.Gib();
                    }
                };
                verb.Impact = LogImpact.Extreme; // if you're just outright killing a person, I guess that deserves to be extreme?
                verb.ConfirmationPopup = true;
                args.Verbs.Add(verb);
            }
        }

        private void AddDebugVerbs(GetVerbsEvent<Verb> args)
        {
            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;

            var player = actor.PlayerSession;

            // Delete verb
            if (_groupController.CanCommand(player, "deleteentity"))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("delete-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png";
                verb.Act = () => EntityManager.DeleteEntity(args.Target);
                verb.Impact = LogImpact.Medium;
                verb.ConfirmationPopup = true;
                args.Verbs.Add(verb);
            }

            // Rejuvenate verb
            if (_groupController.CanCommand(player, "rejuvenate"))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("rejuvenate-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png";
                verb.Act = () => RejuvenateCommand.PerformRejuvenate(args.Target);
                verb.Impact = LogImpact.Medium;
                args.Verbs.Add(verb);
            }

            // Control mob verb
            if (_groupController.CanCommand(player, "controlmob") &&
                args.User != args.Target &&
                EntityManager.HasComponent<MindComponent>(args.User) &&
                EntityManager.TryGetComponent<MindComponent?>(args.Target, out var targetMind))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("control-mob-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                // TODO VERB ICON control mob icon
                verb.Act = () =>
                {
                    player.ContentData()?.Mind?.TransferTo(args.Target, ghostCheckOverride: true);
                };
                verb.Impact = LogImpact.High;
                verb.ConfirmationPopup = true;
                args.Verbs.Add(verb);
            }

            // Make Sentient verb
            if (_groupController.CanCommand(player, "makesentient") &&
                args.User != args.Target &&
                !EntityManager.HasComponent<MindComponent>(args.Target))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("make-sentient-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/sentient.svg.192dpi.png";
                verb.Act = () => MakeSentientCommand.MakeSentient(args.Target, EntityManager);
                verb.Impact = LogImpact.Medium;
                args.Verbs.Add(verb);
            }

            // Set clothing verb
            if (_groupController.CanCommand(player, "setoutfit") &&
                EntityManager.HasComponent<InventoryComponent>(args.Target))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("set-outfit-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png";
                verb.Act = () => _euiManager.OpenEui(new SetOutfitEui(args.Target), player);
                verb.Impact = LogImpact.Medium;
                args.Verbs.Add(verb);
            }

            // In range unoccluded verb
            if (_groupController.CanCommand(player, "inrangeunoccluded"))
            {
                Verb verb = new();
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
                EntityManager.TryGetComponent<IDisposalTubeComponent?>(args.Target, out var tube))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("tube-direction-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/information.svg.192dpi.png";
                verb.Act = () => tube.PopupDirections(args.User);
                args.Verbs.Add(verb);
            }

            // Make ghost role verb
            if (_groupController.CanCommand(player, "makeghostrole") &&
                !(EntityManager.GetComponentOrNull<MindComponent>(args.Target)?.HasMind ?? false))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("make-ghost-role-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                // TODO VERB ICON add ghost icon
                // Where is the national park service icon for haunted forests?
                verb.Act = () => _ghostRoleSystem.OpenMakeGhostRoleEui(player, args.Target);
                verb.Impact = LogImpact.Medium;
                args.Verbs.Add(verb);
            }

            // Configuration verb. Is this even used for anything!?
            if (_groupController.CanAdminMenu(player) &&
                EntityManager.TryGetComponent<ConfigurationComponent?>(args.Target, out var config))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("configure-verb-get-data-text");
                verb.IconTexture = "/Textures/Interface/VerbIcons/settings.svg.192dpi.png";
                verb.Category = VerbCategory.Debug;
                verb.Act = () => config.OpenUserInterface(actor);
                args.Verbs.Add(verb);
            }

            // Add verb to open Solution Editor
            if (_groupController.CanCommand(player, "addreagent") &&
                EntityManager.HasComponent<SolutionContainerManagerComponent>(args.Target))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("edit-solutions-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/spill.svg.192dpi.png";
                verb.Act = () => OpenEditSolutionsEui(player, args.Target);
                verb.Impact = LogImpact.Medium; // maybe high depending on WHAT reagents they add...
                args.Verbs.Add(verb);
            }
        }

        #region SolutionsEui
        private void OnSolutionChanged(EntityUid uid, SolutionContainerManagerComponent component, SolutionChangedEvent args)
        {
            foreach (var eui in _openSolutionUis.Values)
            {
                if (eui.Target == uid)
                    eui.StateDirty();
            }
        }

        public void OpenEditSolutionsEui(IPlayerSession session, EntityUid uid)
        {
            if (session.AttachedEntity == null)
                return;

            if (_openSolutionUis.ContainsKey(session))
                _openSolutionUis[session].Close();

            var eui = _openSolutionUis[session] = new EditSolutionsEui(uid);
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void OnEditSolutionsEuiClosed(IPlayerSession session)
        {
            _openSolutionUis.Remove(session, out var eui);
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            _openSolutionUis.Clear();
        }
        #endregion
    }
}
