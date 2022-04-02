using Content.Server.Body.Components;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Components;

namespace Content.Server.Dragon
{
    public sealed class DragonSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, GetActionsEvent>(OnGetActions);
            SubscribeLocalEvent<DragonComponent, DevourActionEvent>(OnDevourAction);
        }

        private void OnGetActions(EntityUid uid, DragonComponent component, GetActionsEvent args)
        {
            args.Actions.Add(component.DevourAction);
        }

        private void OnDevourAction(EntityUid uid, DragonComponent component, PerformEntityTargetActionEvent args)
        {
            var target = args.Target;

            //Check if the target is valid. The effects should be possible to accomplish on either a wall or a body.

            //NOTE: I honestly don't know much on how one detects valid eating targets, so right now I am using a body component to tell them apart
            //That way dragons can't devour guardians and other dragons. Yet.

            if (EntityManager.TryGetComponent(target, out BodyComponent body))
            {
                //Humanoid devours allow dragon to get eggs, corpses included
                if (EntityManager.TryGetComponent(target, out HumanoidAppearanceComponent humanoid))
                {
                    component.EggsLeft++;
                    EntityManager.QueueDeleteEntity(target);
                }
                else return;
            }
            else return;
        }

        
    }

}
