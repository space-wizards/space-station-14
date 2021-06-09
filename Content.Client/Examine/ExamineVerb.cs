using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Client.Examine
{
    [GlobalVerb]
    public class ExamineVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Examine");
            data.IconTexture = "/Textures/Interface/VerbIcons/examine.svg.192dpi.png";
        }

        public override void Activate(IEntity user, IEntity target)
        {
            EntitySystem.Get<ExamineSystem>().DoExamine(target);
        }
    }
}
