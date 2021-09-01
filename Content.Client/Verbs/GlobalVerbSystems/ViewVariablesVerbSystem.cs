using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Verbs.GlobalVerbSystems
{
    /// <summary>
    ///     System for a global verb that opens a view variables window for the entity in question.
    /// </summary>
    class ViewVariablesVerbSystem : EntitySystem
    {
        [Dependency] IClientConGroupController _clientConGroupController = default!;
        [Dependency] IViewVariablesManager viewVariablesManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AssembleVerbsEvent>(AddViewVariablesVerb);
        }

        private void AddViewVariablesVerb(AssembleVerbsEvent args)
        {
            if (!args.Types.HasFlag(VerbTypes.Other))
                return;

            if (_clientConGroupController.CanViewVar())
            {
                Verb verb = new("ViewVariables");
                verb.Category = VerbCategories.Debug;
                verb.Text = "View Variables";
                verb.IconTexture = "/Textures/Interface/VerbIcons/vv.svg.192dpi.png";
                verb.Act = () => viewVariablesManager.OpenVV(args.Target);
                args.Verbs.Add(verb);
            }
        }
    }
}
