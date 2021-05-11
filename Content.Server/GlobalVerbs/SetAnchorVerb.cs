#nullable enable
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class SetAnchorVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Invisible;
            data.IconTexture = "/Textures/Interface/VerbIcons/anchor.svg.192dpi.png";

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (user.TryGetComponent<ActorComponent>(out var player))
            {
                if (!target.TryGetComponent(out PhysicsComponent? physics))
                {
                    return;
                }

                if (groupController.CanCommand(player.PlayerSession, "setanchor"))
                {
                    data.Text = physics.Anchored ? "Unanchor" : "Anchor";
                    data.Visibility = VerbVisibility.Visible;
                }
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (user.TryGetComponent<ActorComponent>(out var player))
            {
                var groupController = IoCManager.Resolve<IConGroupController>();
                if (!groupController.CanCommand(player.PlayerSession, "setanchor"))
                    return;

                if (target.TryGetComponent(out PhysicsComponent? physics))
                {
                    physics.Anchored = !physics.Anchored;
                }
            }
        }
    }
}
