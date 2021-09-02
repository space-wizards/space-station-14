using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Verbs
{

    // TODO QUESTION
    // This really shouldn't be it's own system.
    // Maybe add onto client-verb-system?
    // Similar for the DebugVerb System
    // Should that be added onto some sort of admin system? Or the verb system?
    // Also note that tehse are the only verbs that truly need to target component
    // if they were directly adddded by verb systems, events could be non-broadcast


    /// <summary>
    ///     System for a global verb that opens a view variables window for the entity in question.
    /// </summary>
    class ViewVariablesVerbSystem : EntitySystem
    {
        [Dependency] private readonly IClientConGroupController _clientConGroupController = default!;
        [Dependency] private readonly IViewVariablesManager _viewVariablesManager = default!;

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
                verb.Act = () => _viewVariablesManager.OpenVV(args.Target);
                args.Verbs.Add(verb);
            }
        }
    }
}
