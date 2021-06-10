using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.ViewVariables;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.ViewVariables
{
    /// <summary>
    /// Global verb that opens a view variables window for the entity in question.
    /// </summary>
    [GlobalVerb]
    class ViewVariablesVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            var groupController = IoCManager.Resolve<IClientConGroupController>();
            if (!groupController.CanViewVar())
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            data.Text = "View Variables";
            data.CategoryData = VerbCategories.Debug;
            data.IconTexture = "/Textures/Interface/VerbIcons/vv.svg.192dpi.png";
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var vvm = IoCManager.Resolve<IViewVariablesManager>();
            vvm.OpenVV(target);
        }
    }
}
