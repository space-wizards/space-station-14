using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class TransformableContainerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TransformableContainerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<TransformableContainerComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnMapInit(EntityUid uid, TransformableContainerComponent component, MapInitEvent args)
        {
            var meta = MetaData(uid);
            component.InitialName = meta.EntityName;
            component.InitialDescription = meta.EntityDescription;
        }

        private void OnSolutionChange(EntityUid owner, TransformableContainerComponent component,
            SolutionChangedEvent args)
        {
            if (!_solutionsSystem.TryGetFitsInDispenser(owner, out var solution))
                return;
            //Transform container into initial state when emptied
            if (component.CurrentReagent != null && solution.Contents.Count == 0)
            {
                CancelTransformation(component);
            }

            //the biggest reagent in the solution decides the appearance
            var reagentId = solution.GetPrimaryReagentId();

            //If biggest reagent didn't changed - don't change anything at all
            if (component.CurrentReagent != null && component.CurrentReagent.ID == reagentId)
            {
                return;
            }

            //Only reagents with spritePath property can change appearance of transformable containers!
            if (!string.IsNullOrWhiteSpace(reagentId)
                && _prototypeManager.TryIndex(reagentId, out ReagentPrototype? proto))
            {
                var metadata = MetaData(owner);
                string val = Loc.GetString("transformable-container-component-glass", ("name", proto.LocalizedName));
                metadata.EntityName = val;
                metadata.EntityDescription = proto.LocalizedDescription;
                component.CurrentReagent = proto;
                component.Transformed = true;
            }
        }

        private void CancelTransformation(TransformableContainerComponent component)
        {
            component.CurrentReagent = null;
            component.Transformed = false;

            var metadata = MetaData(component.Owner);

            metadata.EntityName = component.InitialName;
            metadata.EntityDescription = component.InitialDescription;
        }
    }
}
