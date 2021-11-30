using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Verbs
{
    /// <summary>
    ///     Client-side admin verb system. These usually open some sort of UIs.
    /// </summary>
    class AdminVerbSystem : EntitySystem
    {
        [Dependency] private readonly IClientConGroupController _clientConGroupController = default!;
        [Dependency] private readonly IViewVariablesManager _viewVariablesManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<GetOtherVerbsEvent>(AddAdminVerbs);
        }

        private void AddAdminVerbs(GetOtherVerbsEvent args)
        {
            // Currently this is only the ViewVariables verb, but more admin-UI related verbs can be added here.

            // View variables verbs
            if (_clientConGroupController.CanViewVar())
            {
                Verb verb = new();
                verb.Category = VerbCategory.Debug;
                verb.Text = "View Variables";
                verb.IconTexture = "/Textures/Interface/VerbIcons/vv.svg.192dpi.png";
                verb.Act = () => _viewVariablesManager.OpenVV(args.Target);
                verb.ClientExclusive = true; // opening VV window is client-side. Don't ask server to run this verb.
                args.Verbs.Add(verb);
            }
        }
    }
}
