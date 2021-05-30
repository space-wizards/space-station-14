#nullable enable
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class MakeGhostRoleVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            var groupController = IoCManager.Resolve<IConGroupController>();

            if (target.TryGetComponent(out MindComponent? mind) &&
                mind.HasMind)
            {
                return;
            }

            if (!user.TryGetComponent(out ActorComponent? actor) ||
                !groupController.CanCommand(actor.PlayerSession, "makeghostrole"))
            {
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Make Ghost Role");
            data.CategoryData = VerbCategories.Debug;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();

            if (target.TryGetComponent(out MindComponent? mind) &&
                mind.HasMind)
            {
                return;
            }

            if (!user.TryGetComponent(out ActorComponent? actor) ||
                !groupController.CanCommand(actor.PlayerSession, "makeghostrole"))
            {
                return;
            }

            var ghostRoleSystem = EntitySystem.Get<GhostRoleSystem>();
            ghostRoleSystem.OpenMakeGhostRoleEui(actor.PlayerSession, target.Uid);
        }
    }
}
