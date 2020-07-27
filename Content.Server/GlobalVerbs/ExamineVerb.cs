using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class ExamineVerb : GlobalVerb
    {
        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            if (!user.TryGetComponent(out IActorComponent actor) ||
                actor.playerSession.AttachedEntity == null)
            {
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Examine");
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent(out IActorComponent actor) ||
                actor.playerSession.AttachedEntity == null)
            {
                return;
            }

            var examineSystem = EntitySystem.Get<ExamineSystem>();
            examineSystem.DoExamine(user, actor.playerSession.ConnectedClient, target.Uid);
        }
    }
}
