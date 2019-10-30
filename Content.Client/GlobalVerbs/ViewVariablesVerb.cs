using Content.Shared.GameObjects;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GlobalVerbs
{
    /// <summary>
    /// Global verb that opens a view variables window for the entity in question.
    /// </summary>
    [GlobalVerb]
    class ViewVariablesVerb : GlobalVerb
    {
        public override string GetText(IEntity user, IEntity target) => "View variables";
        public override bool RequireInteractionRange => false;

        public override VerbVisibility GetVisibility(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IClientConGroupController>();
            if (groupController.CanViewVar())
                return VerbVisibility.Visible;
            return VerbVisibility.Invisible;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var vvm = IoCManager.Resolve<IViewVariablesManager>();
            vvm.OpenVV(target);
        }
    }
}
