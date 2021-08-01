using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class TransformableContainerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TransformableContainerComponent, SolutionChangeEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, TransformableContainerComponent component,
            SolutionChangeEvent args)
        {
            var solution = args.Owner.GetComponent<SolutionContainerComponent>();
            //Transform container into initial state when emptied
            if (component.CurrentReagent != null && solution.ReagentList.Count == 0)
            {
                CancelTransformation(component);
            }

            //the biggest reagent in the solution decides the appearance
            var reagentId = solution.Solution.GetPrimaryReagentId();

            //If biggest reagent didn't changed - don't change anything at all
            if (component.CurrentReagent != null && component.CurrentReagent.ID == reagentId)
            {
                return;
            }

            //Only reagents with spritePath property can change appearance of transformable containers!
            if (!string.IsNullOrWhiteSpace(reagentId)
                && _prototypeManager.TryIndex(reagentId, out ReagentPrototype? proto)
                && !string.IsNullOrWhiteSpace(proto.SpriteReplacementPath))
            {
                var spriteSpec =
                    new SpriteSpecifier.Rsi(
                        new ResourcePath("Objects/Consumable/Drinks/" + proto.SpriteReplacementPath), "icon");

                if (args.Owner.TryGetComponent(out SpriteComponent? sprite))
                {
                    sprite?.LayerSetSprite(0, spriteSpec);
                }

                args.Owner.Name = proto.Name + " glass";
                args.Owner.Description = proto.Description;
                component.CurrentReagent = proto;
                component.Transformed = true;
            }
        }

        private void CancelTransformation(TransformableContainerComponent component)
        {
            component.CurrentReagent = null;
            component.Transformed = false;

            if (component.Owner.TryGetComponent(out SpriteComponent? sprite) &&
                component.InitialSprite != null)
            {
                sprite.LayerSetSprite(0, component.InitialSprite);
            }

            component.Owner.Name = component.InitialName;
            component.Owner.Description = component.InitialDescription;
        }
    }
}
