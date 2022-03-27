using Content.Server.Body.Components;
using Content.Shared.Actions;

namespace Content.Server.Dragon
{
    public sealed class DragonSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, GetActionsEvent>(OnGetActions);
            SubscribeLocalEvent<DragonComponent, PerformEntityTargetActionEvent>(OnDevourAction);
        }

        private void OnGetActions(EntityUid uid, DragonComponent component, GetActionsEvent args)
        {
            args.Actions.Add(component.DevourAction);
        }

        private void OnDevourAction(EntityUid uid, DragonComponent component, PerformEntityTargetActionEvent args)
        {
            var target = args.Target;
            //Check if the target is valid
            //I honestly don't know much on how one detects valid eating targets, so right now I am using a body component to tell them apart
            //That way dragons can't vore guardians and other dragons. Yet.
            if (EntityManager.TryGetComponent(target, out BodyComponent body)
            {
                if (EntityManager.)
            }
        }

        
    }

}
