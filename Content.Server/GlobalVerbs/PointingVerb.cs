using Content.Server.GameObjects.Components.Pointing;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    /// <summary>
    ///     Global verb that points at an entity.
    /// </summary>
    [GlobalVerb]
    public class PointingVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            if (!user.HasComponent<IActorComponent>())
            {
                return;
            }

            if (!EntitySystem.Get<PointingSystem>().InRange(user, target.Transform.Coordinates))
            {
                return;
            }

            if (target.HasComponent<PointingArrowComponent>())
            {
                return;
            }

            data.Visibility = VerbVisibility.Visible;

            data.Text = Loc.GetString("Point at");
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            EntitySystem.Get<PointingSystem>().TryPoint(actor.playerSession, target.Transform.Coordinates, target.Uid);
        }
    }
}
