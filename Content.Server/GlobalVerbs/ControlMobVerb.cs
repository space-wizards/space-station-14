using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Players;
using Content.Shared.GameObjects;
using Robust.Server.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class ControlMobVerb : GlobalVerb
    {
        public override string GetText(IEntity user, IEntity target) => "Control Mob";
        public override string GetCategory(IEntity user, IEntity target) => "Debug";

        public override bool RequireInteractionRange => false;

        public override VerbVisibility GetVisibility(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();
            if (user == target) return VerbVisibility.Invisible;

            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (!user.HasComponent<MindComponent>() || !target.HasComponent<MindComponent>())
                    return VerbVisibility.Invisible;

                if (groupController.CanCommand(player.playerSession, "controlmob"))
                    return VerbVisibility.Visible;
            }

            return VerbVisibility.Invisible;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var userMind = user.GetComponent<IActorComponent>().playerSession.ContentData().Mind;
            var targetMind = target.GetComponent<MindComponent>();
            var oldEntity = userMind.CurrentEntity;

            targetMind.Mind?.TransferTo(null);
            userMind.TransferTo(target);

            if(oldEntity.HasComponent<GhostComponent>())
                oldEntity.Delete();
        }
    }
}
