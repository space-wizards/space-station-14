using Content.Client.Administration.Managers;
using Content.Client.Administration.UI.ManageSolutions;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;

namespace Content.Client.Verbs
{
    /// <summary>
    ///     Client-side admin verb system. These usually open some sort of UIs.
    /// </summary>
    class AdminVerbSystem : EntitySystem
    {
        [Dependency] private readonly IClientConGroupController _clientConGroupController = default!;
        [Dependency] private readonly IViewVariablesManager _viewVariablesManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;

        private ManageSolutionsWindow? _solutionsWindow;

        public override void Initialize()
        {
            SubscribeLocalEvent<GetOtherVerbsEvent>(AddAdminVerbs);
            SubscribeLocalEvent<SolutionContainerManagerComponent, SolutionChangedEvent>(UpdateSolutionGui);
        }

        private void AddAdminVerbs(GetOtherVerbsEvent args)
        {
            // View variables verbs
            if (_clientConGroupController.CanViewVar())
            {
                Verb verb = new();
                verb.Category = VerbCategory.Debug;
                verb.Text = "View Variables";
                verb.IconTexture = "/Textures/Interface/VerbIcons/vv.svg.192dpi.png";
                verb.Act = () => _viewVariablesManager.OpenVV(args.Target);
                args.Verbs.Add(verb);
            }


            // Add Solution Manager verb
            if (_adminManager.HasFlag(AdminFlags.Fun) &&
                args.Target.HasComponent<SolutionContainerManagerComponent>())
            {
                Verb verb = new();
                verb.Text = Loc.GetString("admin-solution-manager-verb-get-data-text");
                verb.Category = VerbCategory.Debug;
                verb.IconTexture = "/Textures/Interface/VerbIcons/spill.svg.192dpi.png";
                verb.Act = () =>
                {
                    _solutionsWindow?.Close();
                    _solutionsWindow?.Dispose();
                    _solutionsWindow = new ManageSolutionsWindow(args.Target.Uid);
                    _solutionsWindow.OpenCentered();
                };
                args.Verbs.Add(verb);
            }
        }

        private void UpdateSolutionGui(EntityUid uid, SolutionContainerManagerComponent component, SolutionChangedEvent args)
        {
            if (_solutionsWindow?.Target == uid)
                _solutionsWindow.UpdateReagents();
        }
    }
}
